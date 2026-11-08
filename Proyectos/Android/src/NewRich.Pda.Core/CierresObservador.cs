using NewRich.Application.Contracts.Cierre;

namespace NewRich.Pda.Core;

/// <summary>Los cierres que el observador puede ver, en solo lectura, según las fechas de su filtro.</summary>
public static class CierresObservador
{
    public static IReadOnlyList<CierreResumenResponse> EnRango(
        IEnumerable<CierreResumenResponse> cierres,
        DateOnly desde,
        DateOnly hasta)
    {
        if (hasta < desde)
        {
            return [];
        }

        var inicio = desde.ToDateTime(TimeOnly.MinValue);
        var fin = hasta.ToDateTime(TimeOnly.MaxValue);
        return cierres
            .Where(c => (c.FechaInicio ?? DateTime.MinValue) <= fin && c.FechaFin >= inicio)
            .OrderByDescending(c => c.FechaFin)
            .ToList();
    }
}
