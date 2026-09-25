using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Domain.Services;

namespace NewRich.Application.Services;

/// <summary>Acumulado diario (Colombia) de Lotería + Número sumando directo y combinado.</summary>
public static class AcumuladoTopeConsulta
{
    public static async Task<IReadOnlyList<ValidacionTope.Acumulado>> CargarAsync(
        INewRichDbContext db,
        IClock clock,
        IReadOnlyCollection<(Guid LoteriaId, string Numero)> claves,
        CancellationToken cancellationToken)
    {
        if (claves.Count == 0)
        {
            return [];
        }

        var loteriaIds = claves.Select(c => c.LoteriaId).Distinct().ToList();
        var numeros = claves.Select(c => c.Numero.Trim()).Distinct().ToList();
        var (inicioUtc, finUtc) = RangoDiaLocalUtc(clock.LocalNow.Date);

        var filas = await db.JuegoLoterias
            .AsNoTracking()
            .Where(jl => loteriaIds.Contains(jl.LoteriaId)
                && jl.Juego != null
                && numeros.Contains(jl.Juego.Numero)
                && jl.Juego.Boleto != null
                && jl.Juego.Boleto.Venta != null
                && jl.Juego.Boleto.Venta.FechaVenta >= inicioUtc
                && jl.Juego.Boleto.Venta.FechaVenta < finUtc)
            .Select(jl => new { jl.LoteriaId, Numero = jl.Juego!.Numero, jl.Juego.Valor })
            .ToListAsync(cancellationToken);

        var pedidas = claves
            .Select(c => (c.LoteriaId, Numero: c.Numero.Trim()))
            .ToHashSet();

        return filas
            .Where(f => pedidas.Contains((f.LoteriaId, f.Numero.Trim())))
            .GroupBy(f => (f.LoteriaId, Numero: f.Numero.Trim()))
            .Select(g => new ValidacionTope.Acumulado(g.Key.LoteriaId, g.Key.Numero, g.Sum(x => x.Valor)))
            .ToList();
    }

    public static (DateTime InicioUtc, DateTime FinUtc) RangoDiaLocalUtc(DateTime diaLocal)
    {
        var local = DateTime.SpecifyKind(diaLocal.Date, DateTimeKind.Unspecified);
        var inicioUtc = TimeZoneInfo.ConvertTimeToUtc(local, ZonaHorariaColombia.Actual);
        return (inicioUtc, inicioUtc.AddDays(1));
    }

    public static IReadOnlyList<ValidacionTope.Aporte> AportesDe(
        IEnumerable<(string Numero, decimal Valor, IReadOnlyList<(Guid LoteriaId, string Nombre)> Loterias)> lineas) =>
        lineas
            .SelectMany(l => l.Loterias.Select(lot =>
                new ValidacionTope.Aporte(lot.LoteriaId, lot.Nombre, l.Numero.Trim(), l.Valor)))
            .ToList();
}
