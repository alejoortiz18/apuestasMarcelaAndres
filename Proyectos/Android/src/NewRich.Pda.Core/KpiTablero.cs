using NewRich.Application.Contracts.Kpi;

namespace NewRich.Pda.Core;

public sealed record KpiPuntoDia(DateOnly Fecha, decimal Total, int Ventas, int Alto);

public sealed record KpiBarraVendedor(string Nombre, string Grupo, int Ventas, decimal Total, int Ancho);

public sealed record KpiTablero(
    string Lectura,
    string VariacionTexto,
    bool VariacionPositiva,
    decimal Ingresos,
    int Ventas,
    decimal TicketPromedio,
    string Pico,
    IReadOnlyList<KpiPuntoDia> Serie,
    IReadOnlyList<KpiBarraVendedor> Ranking)
{
    public static KpiTablero De(KpiResponse kpi, DateOnly desde, DateOnly hasta, bool filtroVendedor)
    {
        if (hasta < desde)
        {
            (desde, hasta) = (hasta, desde);
        }

        var porFecha = kpi.VentasPorDiaDetalle.ToDictionary(x => x.Fecha, x => x);
        var serie = new List<KpiPuntoDia>();
        for (var dia = desde; dia <= hasta; dia = dia.AddDays(1))
        {
            porFecha.TryGetValue(dia, out var fila);
            serie.Add(new KpiPuntoDia(dia, fila?.Total ?? 0, fila?.Ventas ?? 0, 0));
        }

        var maximo = serie.Count == 0 ? 0 : serie.Max(x => x.Total);
        serie = serie.Select(x => x with
        {
            Alto = maximo == 0 ? 0 : (int)Math.Round(x.Total * 100 / maximo, 0)
        }).ToList();

        var maxVendedor = kpi.IngresosPorVendedor.Count == 0 ? 0 : kpi.IngresosPorVendedor.Max(x => x.Total);
        var ranking = kpi.IngresosPorVendedor
            .OrderByDescending(x => x.Total)
            .Select(x => new KpiBarraVendedor(
                x.Vendedor,
                x.Grupo,
                x.Ventas,
                x.Total,
                maxVendedor == 0 ? 0 : (int)Math.Round(x.Total * 100 / maxVendedor, 0)))
            .ToList();

        var pico = serie.Where(x => x.Total > 0).OrderByDescending(x => x.Total).FirstOrDefault();
        var picoTexto = pico is null ? "—" : $"{pico.Fecha.Day:00}/{pico.Fecha.Month:00}/{pico.Fecha.Year}";
        var variacion = kpi.VariacionIngresos ?? 0;
        return new KpiTablero(
            FraseLectura(kpi.Ingresos, kpi.VariacionIngresos, filtroVendedor),
            kpi.VariacionIngresos.HasValue ? PdaTexts.KpiVariacion(kpi.VariacionIngresos.Value) : "—",
            variacion >= 0,
            kpi.Ingresos,
            kpi.VentasConfirmadas,
            kpi.TicketPromedio,
            picoTexto,
            serie,
            ranking);
    }

    private static string FraseLectura(decimal ingresos, decimal? variacion, bool filtroVendedor)
    {
        if (ingresos == 0)
        {
            return PdaTexts.KpiSinMovimientos;
        }

        if (!variacion.HasValue || variacion.Value == 0)
        {
            return filtroVendedor ? PdaTexts.KpiLecturaVendedorEstables() : PdaTexts.KpiLecturaEstables();
        }

        var porcentaje = PdaTexts.FormatoPorcentaje(variacion.Value);
        if (variacion.Value > 0)
        {
            return filtroVendedor
                ? PdaTexts.KpiLecturaVendedorSubieron(porcentaje)
                : PdaTexts.KpiLecturaSubieron(porcentaje);
        }

        return filtroVendedor
            ? PdaTexts.KpiLecturaVendedorBajaron(porcentaje)
            : PdaTexts.KpiLecturaBajaron(porcentaje);
    }
}
