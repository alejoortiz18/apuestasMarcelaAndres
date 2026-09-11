using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Chat;
using NewRich.Application.Contracts.Chat;
using NewRich.Application.Contracts.Premios;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class PremioService : IPremioService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;
    private readonly INotificacionService _notificaciones;
    private readonly IQrCryptoService _qr;
    private readonly IChatFileStorage _files;
    private readonly IValidacionBoletoService _validacion;

    public PremioService(
        INewRichDbContext db,
        IClock clock,
        INotificacionService notificaciones,
        IQrCryptoService qr,
        IChatFileStorage files,
        IValidacionBoletoService validacion)
    {
        _db = db;
        _clock = clock;
        _notificaciones = notificaciones;
        _qr = qr;
        _files = files;
        _validacion = validacion;
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
        var bruto = (request.TicketCode ?? string.Empty).Trim();
        if (bruto.Length == 0)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TicketRequerido);
        }

        if (bruto.Length > TicketCodeLimits.MaxInputLength)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TicketDemasiadoLargo);
        }

        var solicitante = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == solicitanteId, cancellationToken);
        if (solicitante is null)
        {
            return Result<CasoGanadorResponse>.Fail(UsuarioMessages.UsuarioNoEncontrado, 404);
        }

        var boletos = _db.Boletos
            .Include(b => b.Venta)
            .ThenInclude(v => v!.Dispositivo)
            .Include(b => b.Venta)
            .ThenInclude(v => v!.Usuario);

        var consulta = await _validacion.ConsultarPorCodigoAsync(bruto, cancellationToken);
        if (!consulta.IsSuccess || consulta.Data?.BoletoId is not Guid boletoId)
        {
            return Result<CasoGanadorResponse>.Fail(consulta.Message, consulta.StatusCode);
        }

        var boleto = await BoletoPorCodigo.BuscarAsync(boletos, _qr, bruto, cancellationToken)
            ?? await boletos.FirstOrDefaultAsync(b => b.BoletoId == boletoId, cancellationToken);
        if (boleto is null)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TicketNoEncontrado, 404);
        }

        var codigo = boleto.CodigoPublico;

        if (boleto.EstadoBoleto == EstadoBoleto.PremioEntregado || boleto.EstadoDelPremio == EstadoDelPremio.PremioEntregado)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TicketPremioEntregado);
        }

        var listoParaCobrar = consulta.Data.ResultadoVisual == BoletoMessages.BoletoGanador
            || boleto.EstadoBoleto == EstadoBoleto.Ganador;
        if (!listoParaCobrar)
        {
            return Result<CasoGanadorResponse>.Fail(
                string.IsNullOrWhiteSpace(consulta.Data.Mensaje)
                    ? PremioMessages.BoletoNoEsGanadorParaCaso
                    : consulta.Data.Mensaje);
        }

        if (solicitante.Rol == RolUsuario.Vendedor && boleto.Venta?.UsuarioId != solicitanteId)
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.TicketNoEsDelVendedor, 403);
        }

        if (await _db.CasosGanadores.AnyAsync(c => c.BoletoId == boleto.BoletoId, cancellationToken))
        {
            return Result<CasoGanadorResponse>.Fail(PremioMessages.CasoYaExiste);
        }

        var fotoObligatoria = false;
        var foto = await ResolverFotoAsync(request, fotoObligatoria, cancellationToken);
        if (!foto.IsSuccess)
        {
            return Result<CasoGanadorResponse>.Fail(foto.Message);
        }

        var vendedorId = boleto.Venta?.UsuarioId ?? solicitanteId;
        var caso = new CasoGanador
        {
            CasoId = Guid.NewGuid(),
            BoletoId = boleto.BoletoId,
            TicketCode = codigo,
            Estado = EstadoCasoGanador.Reportado,
            FechaReporte = _clock.UtcNow,
            VendedorQueReporto = vendedorId,
            FotoTicketRuta = foto.Data?.Ruta,
            FotoTicketNombre = foto.Data?.Nombre
        };
        _db.CasosGanadores.Add(caso);
        boleto.CasoGanadorId = caso.CasoId;
        boleto.EstadoDelPremio = EstadoDelPremio.Vigente;

        var admins = await _db.Usuarios
            .Where(u => u.Rol == RolUsuario.Administrador && u.Estado == EstadoUsuario.Activo && !u.EstadoBloqueado)
            .Select(u => u.UsuarioId)
            .ToListAsync(cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        await _notificaciones.CrearParaAsync(admins, "CasoGanador", $"Se reportó el ticket {codigo} como ganador.", cancellationToken);
        var creado = await Query().FirstAsync(c => c.CasoId == caso.CasoId, cancellationToken);
        return Result<CasoGanadorResponse>.Created(Map(creado), SuccessMessages.CasoGanadorReportado);
    }

    public async Task<Result<DescargaAdjuntoResponse>> ObtenerFotoAsync(Guid casoId, CancellationToken cancellationToken)
    {
        var caso = await _db.CasosGanadores.FirstOrDefaultAsync(c => c.CasoId == casoId, cancellationToken);
        if (caso is null)
        {
            return Result<DescargaAdjuntoResponse>.Fail(PremioMessages.CasoNoEncontrado, 404);
        }

        if (string.IsNullOrWhiteSpace(caso.FotoTicketRuta))
        {
            return Result<DescargaAdjuntoResponse>.Fail(PremioMessages.FotoQrRequerida, 404);
        }

        var stream = await _files.OpenReadAsync(caso.FotoTicketRuta, cancellationToken);
        if (stream is null)
        {
            return Result<DescargaAdjuntoResponse>.Fail(PremioMessages.FotoQrRequerida, 404);
        }

        return Result<DescargaAdjuntoResponse>.Ok(new DescargaAdjuntoResponse
        {
            NombreArchivo = caso.FotoTicketNombre ?? "ticket.jpg",
            Contenido = stream
        }, SuccessMessages.OperacionExitosa);
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

        await _db.SaveChangesAsync(cancellationToken);
        await _notificaciones.CrearParaAsync(
            [observador.UsuarioId],
            "CasoAsignado",
            $"Se te asignó el caso del ticket {caso.TicketCode.Trim()}.",
            cancellationToken);
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
            ValorTotalGanado = entrega?.ValorTotalGanado,
            TieneFoto = !string.IsNullOrWhiteSpace(caso.FotoTicketRuta),
            NombreFoto = caso.FotoTicketNombre
        };
    }

    private async Task<Result<FotoGuardada?>> ResolverFotoAsync(
        ReportarCasoGanadorRequest request,
        bool obligatoria,
        CancellationToken cancellationToken)
    {
        var hay = !string.IsNullOrWhiteSpace(request.ContenidoBase64) || !string.IsNullOrWhiteSpace(request.NombreArchivo);
        if (!hay)
        {
            return obligatoria
                ? Result<FotoGuardada?>.Fail(PremioMessages.FotoQrRequerida)
                : Result<FotoGuardada?>.Ok(null, SuccessMessages.OperacionExitosa);
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(request.ContenidoBase64 ?? string.Empty);
        }
        catch (FormatException)
        {
            return Result<FotoGuardada?>.Fail(PremioMessages.FotoQrInvalida);
        }

        var nombre = ChatAdjunto.NombreSeguro(request.NombreArchivo);
        if (!ChatAdjunto.EsImagen(nombre))
        {
            return Result<FotoGuardada?>.Fail(PremioMessages.FotoQrInvalida);
        }

        var validacion = ChatAdjunto.Validar(nombre, bytes);
        if (!validacion.IsSuccess)
        {
            return Result<FotoGuardada?>.Fail(PremioMessages.FotoQrInvalida);
        }

        await using var stream = new MemoryStream(bytes);
        var ruta = await _files.SaveAsync(stream, nombre, cancellationToken);
        return Result<FotoGuardada?>.Ok(new FotoGuardada(ruta, nombre), SuccessMessages.OperacionExitosa);
    }

    private sealed record FotoGuardada(string Ruta, string Nombre);

    private static string EstadoVisible(CasoGanador caso)
    {
        if (caso.Boleto?.EstadoBoleto == EstadoBoleto.PremioEntregado || caso.Estado == EstadoCasoGanador.Registrado)
        {
            return "Premio entregado";
        }

        return caso.Estado == EstadoCasoGanador.EnProceso ? "En proceso" : caso.Estado.ToString();
    }
}
