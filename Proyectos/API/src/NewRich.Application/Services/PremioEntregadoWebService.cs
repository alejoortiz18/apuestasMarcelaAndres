using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.PremiosEntregadosWeb;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class PremioEntregadoWebService : IPremioEntregadoWebService
{
    private const int FilasMaximas = 100;
    private const int DigitosMaximosNumero = 4;
    private const decimal ValorApostadoMaximo = 999_000m;
    private readonly INewRichDbContext _db;
    private readonly IPasswordHasher _hasher;

    public PremioEntregadoWebService(INewRichDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<Result<PremioEntregadoWebResponse>> RegistrarAsync(
        Guid vendedorId,
        RegistrarPremioWebRequest request,
        DateOnly hoy,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<PremioEntregadoWebResponse>.Fail(PremioEntregadoWebMessages.ContrasenaRequerida);
        }

        var vendedor = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.UsuarioId == vendedorId, cancellationToken);
        if (vendedor is null)
        {
            return Result<PremioEntregadoWebResponse>.Fail(AuthMessages.SesionInvalida, 401);
        }

        if (!_hasher.Verify(request.Password, vendedor.PasswordHash, vendedor.PasswordSalt))
        {
            return Result<PremioEntregadoWebResponse>.Fail(PremioEntregadoWebMessages.ContrasenaIncorrecta);
        }

        var numero = request.Numero?.Trim() ?? string.Empty;
        if (numero.Length is 0 or > DigitosMaximosNumero || !numero.All(char.IsAsciiDigit))
        {
            return Result<PremioEntregadoWebResponse>.Fail(PremioEntregadoWebMessages.NumeroInvalido);
        }

        if (!EsPesoEntero(request.ValorApostado) || request.ValorApostado > ValorApostadoMaximo)
        {
            return Result<PremioEntregadoWebResponse>.Fail(PremioEntregadoWebMessages.ValorApostadoInvalido);
        }

        if (!EsPesoEntero(request.ValorPremio))
        {
            return Result<PremioEntregadoWebResponse>.Fail(PremioEntregadoWebMessages.ValorPremioInvalido);
        }

        if (request.FechaPremio > hoy)
        {
            return Result<PremioEntregadoWebResponse>.Fail(PremioEntregadoWebMessages.FechaFutura);
        }

        var loteria = await _db.Loterias.AsNoTracking()
            .FirstOrDefaultAsync(l => l.LoteriaId == request.LoteriaId && l.Estado == EstadoGeneral.Activo, cancellationToken);
        if (loteria is null)
        {
            return Result<PremioEntregadoWebResponse>.Fail(PremioEntregadoWebMessages.LoteriaNoDisponible);
        }

        var premio = new PremioEntregadoWeb
        {
            PremioEntregadoWebId = Guid.NewGuid(),
            VendedorId = vendedorId,
            LoteriaId = loteria.LoteriaId,
            Numero = numero,
            ValorApostado = request.ValorApostado,
            ValorPremio = request.ValorPremio,
            FechaPremio = request.FechaPremio,
            FechaRegistro = DateTime.UtcNow
        };
        _db.PremiosEntregadosWeb.Add(premio);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<PremioEntregadoWebResponse>.Created(
            new PremioEntregadoWebResponse(premio.PremioEntregadoWebId, premio.FechaPremio, loteria.Nombre, premio.Numero,
                premio.ValorApostado, premio.ValorPremio, premio.FechaRegistro, vendedorId, vendedor.NombreCompleto),
            PremioEntregadoWebMessages.Registrado);
    }

    public async Task<Result<PremiosEntregadosWebResponse>> ListarPropiosAsync(
        Guid vendedorId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, FilasMaximas);
        var propios = _db.PremiosEntregadosWeb.AsNoTracking().Where(p => p.VendedorId == vendedorId);
        var total = await propios.CountAsync(cancellationToken);
        var items = await Proyectar(propios
                .OrderByDescending(p => p.FechaRegistro)
                .Skip((page - 1) * pageSize)
                .Take(pageSize))
            .ToListAsync(cancellationToken);

        return Result<PremiosEntregadosWebResponse>.Ok(new PremiosEntregadosWebResponse(total, items), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<TotalPremiosEntregadosWebResponse>> TotalAsync(
        DateOnly desde,
        DateOnly hasta,
        Guid? grupoId,
        Guid? vendedorId,
        CancellationToken cancellationToken)
    {
        var valores = await Filtrar(desde, hasta, grupoId, vendedorId).Select(p => p.ValorPremio).ToListAsync(cancellationToken);
        return Result<TotalPremiosEntregadosWebResponse>.Ok(
            new TotalPremiosEntregadosWebResponse(valores.Sum(), valores.Count),
            SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<IReadOnlyList<PremioEntregadoWebResponse>>> ListadoAsync(
        DateOnly desde,
        DateOnly hasta,
        Guid? grupoId,
        CancellationToken cancellationToken)
    {
        var items = await Proyectar(Filtrar(desde, hasta, grupoId, null).OrderByDescending(p => p.FechaPremio).ThenByDescending(p => p.FechaRegistro))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyList<PremioEntregadoWebResponse>>.Ok(items, SuccessMessages.OperacionExitosa);
    }

    private IQueryable<PremioEntregadoWeb> Filtrar(DateOnly desde, DateOnly hasta, Guid? grupoId, Guid? vendedorId)
    {
        var consulta = _db.PremiosEntregadosWeb.AsNoTracking()
            .Where(p => p.FechaPremio >= desde && p.FechaPremio <= hasta);
        if (grupoId.HasValue)
        {
            var miembros = _db.Usuarios
                .Where(u => u.UsuarioGrupos.Any(g => g.GrupoId == grupoId.Value))
                .Select(u => u.UsuarioId);
            consulta = consulta.Where(p => miembros.Contains(p.VendedorId));
        }

        if (vendedorId.HasValue)
        {
            consulta = consulta.Where(p => p.VendedorId == vendedorId.Value);
        }

        return consulta;
    }

    private IQueryable<PremioEntregadoWebResponse> Proyectar(IQueryable<PremioEntregadoWeb> premios) =>
        from premio in premios
        join loteria in _db.Loterias on premio.LoteriaId equals loteria.LoteriaId
        join vendedor in _db.Usuarios on premio.VendedorId equals vendedor.UsuarioId
        select new PremioEntregadoWebResponse(
            premio.PremioEntregadoWebId,
            premio.FechaPremio,
            loteria.Nombre,
            premio.Numero,
            premio.ValorApostado,
            premio.ValorPremio,
            premio.FechaRegistro,
            premio.VendedorId,
            vendedor.NombreCompleto);

    private static bool EsPesoEntero(decimal valor) => valor > 0 && decimal.Truncate(valor) == valor;
}
