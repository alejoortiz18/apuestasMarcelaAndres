using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.VentasRegistradasWeb;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class VentaRegistradaWebService : IVentaRegistradaWebService
{
    private const int FilasMaximas = 100;
    private readonly INewRichDbContext _db;
    private readonly IPasswordHasher _hasher;

    public VentaRegistradaWebService(INewRichDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<Result<VentaRegistradaWebResponse>> RegistrarAsync(
        Guid vendedorId,
        RegistrarVentaWebRequest request,
        DateOnly hoy,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<VentaRegistradaWebResponse>.Fail(VentaRegistradaWebMessages.ContrasenaRequerida);
        }

        var vendedor = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.UsuarioId == vendedorId, cancellationToken);
        if (vendedor is null)
        {
            return Result<VentaRegistradaWebResponse>.Fail(AuthMessages.SesionInvalida, 401);
        }

        if (!_hasher.Verify(request.Password, vendedor.PasswordHash, vendedor.PasswordSalt))
        {
            return Result<VentaRegistradaWebResponse>.Fail(VentaRegistradaWebMessages.ContrasenaIncorrecta);
        }

        if (request.FechaVenta != hoy)
        {
            return Result<VentaRegistradaWebResponse>.Fail(VentaRegistradaWebMessages.SoloHoy);
        }

        if (request.Valor <= 0 || decimal.Truncate(request.Valor) != request.Valor)
        {
            return Result<VentaRegistradaWebResponse>.Fail(VentaRegistradaWebMessages.ValorInvalido);
        }

        var venta = new VentaRegistradaWeb
        {
            VentaRegistradaWebId = Guid.NewGuid(),
            VendedorId = vendedorId,
            FechaVenta = request.FechaVenta,
            Valor = request.Valor,
            FechaRegistro = DateTime.UtcNow
        };
        _db.VentasRegistradasWeb.Add(venta);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<VentaRegistradaWebResponse>.Created(Mapa(venta), VentaRegistradaWebMessages.Registrada);
    }

    public async Task<Result<VentasRegistradasWebResponse>> ListarPropiasAsync(
        Guid vendedorId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, FilasMaximas);
        var consulta = _db.VentasRegistradasWeb.AsNoTracking().Where(v => v.VendedorId == vendedorId);
        var total = await consulta.CountAsync(cancellationToken);
        var items = await consulta
            .OrderByDescending(v => v.FechaVenta)
            .ThenByDescending(v => v.FechaRegistro)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Result<VentasRegistradasWebResponse>.Ok(
            new VentasRegistradasWebResponse(total, items.Select(Mapa).ToList()),
            SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<TotalVentasRegistradasWebResponse>> TotalAsync(
        DateOnly desde,
        DateOnly hasta,
        Guid? grupoId,
        Guid? vendedorId,
        CancellationToken cancellationToken)
    {
        var consulta = _db.VentasRegistradasWeb.AsNoTracking()
            .Where(v => v.FechaVenta >= desde && v.FechaVenta <= hasta);
        // Los juegos se guardan en hora de Colombia.
        var inicio = desde.ToDateTime(TimeOnly.MinValue);
        var fin = hasta.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var individuales = _db.RegistrosJuegoManual.AsNoTracking()
            .Where(r => r.FechaRegistro >= inicio && r.FechaRegistro < fin);
        var combos = _db.RegistrosJuegoNumerosWebManual.AsNoTracking()
            .Where(r => r.FechaRegistro >= inicio && r.FechaRegistro < fin);
        if (grupoId.HasValue)
        {
            var miembros = _db.Usuarios
                .Where(u => u.UsuarioGrupos.Any(g => g.GrupoId == grupoId.Value))
                .Select(u => u.UsuarioId);
            consulta = consulta.Where(v => miembros.Contains(v.VendedorId));
            individuales = individuales.Where(r => miembros.Contains(r.UsuarioId));
            combos = combos.Where(r => miembros.Contains(r.UsuarioId));
        }

        if (vendedorId.HasValue)
        {
            consulta = consulta.Where(v => v.VendedorId == vendedorId.Value);
            individuales = individuales.Where(r => r.UsuarioId == vendedorId.Value);
            combos = combos.Where(r => r.UsuarioId == vendedorId.Value);
        }

        var valores = await consulta.Select(v => v.Valor).ToListAsync(cancellationToken);
        var valoresIndividuales = await individuales
            .Select(r => r.Filas.Sum(f => (decimal?)f.Valor) ?? 0m)
            .ToListAsync(cancellationToken);
        var valoresCombos = await combos.Select(r => r.ValorJugado).ToListAsync(cancellationToken);
        return Result<TotalVentasRegistradasWebResponse>.Ok(
            new TotalVentasRegistradasWebResponse(
                valores.Sum(),
                valores.Count,
                valoresIndividuales.Sum() + valoresCombos.Sum(),
                valoresIndividuales.Count + valoresCombos.Count),
            SuccessMessages.OperacionExitosa);
    }

    private static VentaRegistradaWebResponse Mapa(VentaRegistradaWeb venta) =>
        new(venta.VentaRegistradaWebId, venta.FechaVenta, venta.Valor, venta.FechaRegistro);
}
