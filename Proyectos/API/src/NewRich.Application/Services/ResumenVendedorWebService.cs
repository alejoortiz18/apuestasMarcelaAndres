using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.ResumenVendedorWeb;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public interface IResumenVendedorWebService
{
    Task<Result<ResumenVendedorWebResponse>> ObtenerAsync(Guid vendedorId, CancellationToken cancellationToken);
}

public sealed class ResumenVendedorWebService : IResumenVendedorWebService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;

    public ResumenVendedorWebService(INewRichDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<ResumenVendedorWebResponse>> ObtenerAsync(Guid vendedorId, CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(_clock.LocalNow);
        // Los juegos y los resultados se guardan en hora de Colombia; las ventas y los premios web, en UTC.
        var inicioLocal = hoy.ToDateTime(TimeOnly.MinValue);
        var finLocal = inicioLocal.AddDays(1);
        var inicioUtc = ZonaHorariaColombia.InicioUtcDelDia(hoy);
        var finUtc = inicioUtc.AddDays(1);

        var registros = await _db.RegistrosJuegoManual.AsNoTracking()
            .Include(r => r.Filas)
            .Where(r => r.UsuarioId == vendedorId && r.FechaRegistro >= inicioLocal && r.FechaRegistro < finLocal)
            .ToListAsync(cancellationToken);
        var combos = await _db.RegistrosJuegoNumerosWebManual.AsNoTracking()
            .Include(r => r.Apuestas)
            .Where(r => r.UsuarioId == vendedorId && r.FechaRegistro >= inicioLocal && r.FechaRegistro < finLocal)
            .ToListAsync(cancellationToken);
        var individuales = registros
            .SelectMany(r => r.Filas.Select(f => new Apuesta(r.RegistroId, r.CodigoPublico, r.FechaRegistro, f.LoteriaId, f.Numero, f.Valor, TiposRegistroJuego.Individual)))
            .ToList();
        var apuestas = individuales
            .Concat(combos.SelectMany(r => r.Apuestas.Select(a => new Apuesta(r.RegistroId, r.CodigoPublico, r.FechaRegistro, a.LoteriaId, a.Numero, a.Valor, TiposRegistroJuego.Combo))))
            .ToList();

        var ventas = await _db.VentasRegistradasWeb.AsNoTracking()
            .Where(v => v.VendedorId == vendedorId && v.FechaVenta == hoy)
            .Select(v => v.Valor)
            .ToListAsync(cancellationToken);
        var premios = await _db.PremiosEntregadosWeb.AsNoTracking()
            .Where(p => p.VendedorId == vendedorId && p.FechaRegistro >= inicioUtc && p.FechaRegistro < finUtc)
            .Select(p => p.ValorPremio)
            .ToListAsync(cancellationToken);
        var gastos = await _db.Gastos.AsNoTracking()
            .Where(g => g.UsuarioRegistroId == vendedorId && g.FechaGasto == hoy && g.Estado == EstadoGasto.Activo)
            .Select(g => g.Valor)
            .ToListAsync(cancellationToken);

        return Result<ResumenVendedorWebResponse>.Ok(new ResumenVendedorWebResponse
        {
            Fecha = hoy,
            JuegosRegistrados = registros.Count + combos.Count,
            ApuestasRegistradas = apuestas.Count,
            ValorJuegos = individuales.Sum(a => a.Valor) + combos.Sum(c => c.ValorJugado),
            VentasRegistradas = ventas.Count,
            ValorVentaDelDia = ventas.Sum(),
            PremiosEntregados = premios.Count,
            ValorPremiosEntregados = premios.Sum(),
            Gastos = gastos.Count,
            ValorGastos = gastos.Sum(),
            Ganadores = await GanadoresAsync(apuestas, inicioLocal, finLocal, cancellationToken)
        }, SuccessMessages.OperacionExitosa);
    }

    private async Task<List<NumeroGanadorVendedorResponse>> GanadoresAsync(
        IReadOnlyList<Apuesta> apuestas,
        DateTime inicio,
        DateTime fin,
        CancellationToken cancellationToken)
    {
        if (apuestas.Count == 0)
        {
            return [];
        }

        var loterias = apuestas.Select(a => a.LoteriaId).Distinct().ToList();
        var resultados = await _db.NumerosGanadores.AsNoTracking()
            .Where(n => loterias.Contains(n.LoteriaId) && n.FechaJuego >= inicio && n.FechaJuego < fin)
            .Select(n => new { n.LoteriaId, n.Numero })
            .ToListAsync(cancellationToken);
        var nombres = await _db.Loterias.AsNoTracking()
            .Where(l => loterias.Contains(l.LoteriaId))
            .ToDictionaryAsync(l => l.LoteriaId, l => l.Nombre, cancellationToken);

        return apuestas
            .Where(a => resultados.Any(r => r.LoteriaId == a.LoteriaId && r.Numero.Trim() == a.Numero.Trim()))
            .OrderByDescending(a => a.FechaRegistro)
            .Select(a => new NumeroGanadorVendedorResponse
            {
                Loteria = nombres.GetValueOrDefault(a.LoteriaId) ?? string.Empty,
                Numero = a.Numero.Trim(),
                Valor = a.Valor,
                CodigoPublico = a.CodigoPublico.Trim(),
                Tipo = a.Tipo,
                FechaRegistro = a.FechaRegistro
            })
            .ToList();
    }

    private sealed record Apuesta(
        Guid RegistroId,
        string CodigoPublico,
        DateTime FechaRegistro,
        Guid LoteriaId,
        string Numero,
        decimal Valor,
        string Tipo);
}
