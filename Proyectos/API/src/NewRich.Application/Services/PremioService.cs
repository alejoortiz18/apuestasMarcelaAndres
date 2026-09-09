using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Premios;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class PremioService : IPremioService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;

    public PremioService(INewRichDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<CasoGanadorResponse>>> ListarAsync(CancellationToken cancellationToken)
    {
        var casos = await Query().OrderByDescending(c => c.FechaReporte).ToListAsync(cancellationToken);
        return Result<IReadOnlyList<CasoGanadorResponse>>.Ok(casos.Select(Map).ToList(), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<CasoGanadorResponse>> ObtenerAsync(Guid casoId, CancellationToken cancellationToken)
    {
        var caso = await Query().FirstOrDefaultAsync(c => c.CasoId == casoId, cancellationToken);
        return caso is null
            ? Result<CasoGanadorResponse>.Fail(PremioMessages.CasoNoEncontrado, 404)
            : Result<CasoGanadorResponse>.Ok(Map(caso), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<CasoGanadorResponse>> ReportarAsync(Guid solicitanteId, ReportarCasoGanadorRequest request, CancellationToken cancellationToken)
    {
        var codigo = NormalizarTicket(request.TicketCode);
        if (codigo.Length == 0)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TicketRequerido);
        }

        var solicitante = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == solicitanteId, cancellationToken);
        if (solicitante is null)
        {
            return Result<CasoGanadorResponse>.Fail(UsuarioMessages.UsuarioNoEncontrado, 404);
        }

        var boleto = await _db.Boletos
            .Include(b => b.Venta)
            .ThenInclude(v => v!.Dispositivo)
            .Include(b => b.Venta)
            .ThenInclude(v => v!.Usuario)
            .FirstOrDefaultAsync(b => b.CodigoPublico == codigo, cancellationToken);
        if (boleto is null)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TicketNoEncontrado, 404);
        }

        if (boleto.EstadoBoleto == EstadoBoleto.PremioEntregado || boleto.EstadoDelPremio == EstadoDelPremio.PremioEntregado)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TicketPremioEntregado);
        }

        if (boleto.EstadoBoleto != EstadoBoleto.Ganador)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.BoletoNoEsGanadorParaCaso);
        }

        if (solicitante.Rol == RolUsuario.Vendedor && boleto.Venta?.UsuarioId != solicitanteId)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TicketNoEsDelVendedor, 403);
        }

        if (await _db.CasosGanadores.AnyAsync(c => c.BoletoId == boleto.BoletoId, cancellationToken))
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.CasoYaExiste);
        }

        var vendedorId = boleto.Venta?.UsuarioId ?? solicitanteId;
        var caso = new CasoGanador
        {
            CasoId = Guid.NewGuid(),
            BoletoId = boleto.BoletoId,
            TicketCode = codigo,
            Estado = EstadoCasoGanador.Reportado,
            FechaReporte = _clock.UtcNow,
            VendedorQueReporto = vendedorId
        };
        _db.CasosGanadores.Add(caso);
        boleto.CasoGanadorId = caso.CasoId;
        boleto.EstadoDelPremio = EstadoDelPremio.Vigente;

        var admins = await _db.Usuarios
            .Where(u => u.Rol == RolUsuario.Administrador && u.Estado == EstadoUsuario.Activo && !u.EstadoBloqueado)
            .Select(u => u.UsuarioId)
            .ToListAsync(cancellationToken);
        foreach (var adminId in admins)
        {
            _db.Notificaciones.Add(new Notificacion
            {
                NotificacionId = Guid.NewGuid(),
                UsuarioId = adminId,
                Tipo = "CasoGanador",
                Mensaje = $"Se reportó el ticket {codigo} como ganador.",
                FechaCreacion = _clock.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        var creado = await Query().FirstAsync(c => c.CasoId == caso.CasoId, cancellationToken);
        return Result<CasoGanadorResponse>.Created(Map(creado), SuccessMessages.CasoGanadorReportado);
    }

    public async Task<Result<CasoGanadorResponse>> ValidarAsync(Guid casoId, Guid adminId, CancellationToken cancellationToken)
    {
        var caso = await Query().FirstOrDefaultAsync(c => c.CasoId == casoId, cancellationToken);
        if (caso is null)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.CasoNoEncontrado, 404);
        }

        if (caso.Estado != EstadoCasoGanador.Reportado)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TransicionInvalida);
        }

        caso.Estado = EstadoCasoGanador.Validado;
        caso.FechaValidacionAdmin = _clock.UtcNow;
        caso.AdminQueValido = adminId;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<CasoGanadorResponse>.Ok(Map(caso), SuccessMessages.CasoGanadorValidado);
    }

    public async Task<Result<CasoGanadorResponse>> RechazarAsync(Guid casoId, Guid adminId, CancellationToken cancellationToken)
    {
        var caso = await Query().FirstOrDefaultAsync(c => c.CasoId == casoId, cancellationToken);
        if (caso is null)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.CasoNoEncontrado, 404);
        }

        if (caso.Estado != EstadoCasoGanador.Reportado)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TransicionInvalida);
        }

        caso.Estado = EstadoCasoGanador.Rechazado;
        caso.FechaValidacionAdmin = _clock.UtcNow;
        caso.AdminQueValido = adminId;
        if (caso.Boleto is not null)
        {
            caso.Boleto.EstadoDelPremio = EstadoDelPremio.Rechazado;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<CasoGanadorResponse>.Ok(Map(caso), SuccessMessages.CasoGanadorRechazado);
    }

    public async Task<Result<CasoGanadorResponse>> AsignarAsync(Guid casoId, Guid adminId, AsignarObservadorRequest request, CancellationToken cancellationToken)
    {
        var caso = await Query().FirstOrDefaultAsync(c => c.CasoId == casoId, cancellationToken);
        if (caso is null)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.CasoNoEncontrado, 404);
        }

        if (caso.Estado is EstadoCasoGanador.Registrado or EstadoCasoGanador.Rechazado)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TransicionInvalida);
        }

        if (caso.Estado is not (EstadoCasoGanador.Validado or EstadoCasoGanador.Asignado or EstadoCasoGanador.EnProceso))
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TransicionInvalida);
        }

        var observador = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == request.ObservadorId, cancellationToken);
        if (observador is null
            || observador.Rol != RolUsuario.Observador
            || observador.Estado != EstadoUsuario.Activo
            || observador.EstadoBloqueado)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.ObservadorInvalido);
        }

        caso.ObservadorAsignado = observador.UsuarioId;
        caso.ObservadorAsignadoNavigation = observador;
        caso.AdminQueAsigno = adminId;
        caso.FechaAsignacion = _clock.UtcNow;
        if (caso.Estado == EstadoCasoGanador.Validado)
        {
            caso.Estado = EstadoCasoGanador.Asignado;
        }

        _db.Notificaciones.Add(new Notificacion
        {
            NotificacionId = Guid.NewGuid(),
            UsuarioId = observador.UsuarioId,
            Tipo = "CasoAsignado",
            Mensaje = $"Se te asignó el caso del ticket {caso.TicketCode.Trim()}.",
            FechaCreacion = _clock.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return Result<CasoGanadorResponse>.Ok(Map(caso), SuccessMessages.CasoGanadorAsignado);
    }

    private IQueryable<CasoGanador> Query() =>
        _db.CasosGanadores
            .Include(c => c.Boleto)
            .ThenInclude(b => b!.Venta)
            .ThenInclude(v => v!.Dispositivo)
            .Include(c => c.Boleto)
            .ThenInclude(b => b!.Venta)
            .ThenInclude(v => v!.Usuario)
            .Include(c => c.VendedorQueReportoNavigation)
            .Include(c => c.ObservadorAsignadoNavigation)
            .Include(c => c.EntregaGanador);

    private static CasoGanadorResponse Map(CasoGanador caso)
    {
        var entrega = caso.EntregaGanador;
        return new CasoGanadorResponse
        {
            CasoId = caso.CasoId,
            BoletoId = caso.BoletoId,
            Ticket = caso.TicketCode.Trim(),
            Vendedor = caso.VendedorQueReportoNavigation?.NombreCompleto
                ?? caso.Boleto?.Venta?.Usuario?.NombreCompleto
                ?? string.Empty,
            Pda = caso.Boleto?.Venta?.Dispositivo?.CodigoDispositivo ?? "No aplica",
            Estado = EstadoVisible(caso),
            Observador = string.IsNullOrWhiteSpace(caso.ObservadorAsignadoNavigation?.NombreCompleto)
                ? "Pendiente"
                : caso.ObservadorAsignadoNavigation!.NombreCompleto,
            FechaReporte = caso.FechaReporte,
            FechaValidacion = caso.FechaValidacionAdmin,
            FechaAsignacion = caso.FechaAsignacion,
            FechaRegistro = caso.FechaRegistro,
            NombreGanador = entrega?.NombreGanador,
            ApellidoGanador = entrega?.ApellidoGanador,
            NumeroContacto = entrega?.NumeroContacto,
            LugarGano = entrega?.LugarGano,
            ValorTotalGanado = entrega?.ValorTotalGanado
        };
    }

    private static string EstadoVisible(CasoGanador caso)
    {
        if (caso.Boleto?.EstadoBoleto == EstadoBoleto.PremioEntregado || caso.Estado == EstadoCasoGanador.Registrado)
        {
            return "Premio entregado";
        }

        return caso.Estado == EstadoCasoGanador.EnProceso ? "En proceso" : caso.Estado.ToString();
    }

    private static string NormalizarTicket(string? ticket)
    {
        var codigo = (ticket ?? string.Empty).Trim();
        if (codigo.Length == 0 || codigo.Length > 7)
        {
            return codigo.Length > 7 ? codigo[..7] : codigo;
        }

        return codigo.PadLeft(7, '0');
    }
}
