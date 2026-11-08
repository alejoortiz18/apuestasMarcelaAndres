using NewRich.Application.Contracts.Informes;
using NewRich.Application.Contracts.Kpi;
using NewRich.Application.Contracts.PremiosEntregadosWeb;
using NewRich.Application.Contracts.RevisadaManual;
using NewRich.Application.Contracts.Utilidades;

namespace NewRich.Application.Informes;

/// <summary>
/// Arma el informe de egresos con las mismas sumas que el administrador:
/// premios del observador y del vendedor web por lotería, gastos por tipo y números cubiertos.
/// </summary>
public static class InformeEgresosLectura
{
    public static InformeEgresosObservadorResponse Armar(
        IReadOnlyList<KpiPremioLoteriaResponse> premiosObservador,
        IReadOnlyList<PremioEntregadoWebResponse> premiosWeb,
        IReadOnlyList<GastoPorTipoResponse> gastos,
        IReadOnlyList<GrupoRevisadaManualResponse> cubiertos,
        DateOnly fechaSiFalta)
    {
        var porLoteria = premiosWeb
            .GroupBy(p => p.Loteria)
            .Select(g => (Texto: g.Key, Valor: g.Sum(p => p.ValorPremio), Cantidad: g.Count()))
            .Concat(premiosObservador.Select(p => (Texto: p.Loteria, p.Valor, p.Cantidad)))
            .GroupBy(p => p.Texto)
            .Select(g => (Texto: g.Key, Valor: g.Sum(p => p.Valor), Cantidad: g.Sum(p => p.Cantidad)))
            .ToList();
        var filasCubiertas = cubiertos
            .Select(c => new CubiertoInformeFilaResponse
            {
                Fecha = c.FechaOperacion is { } fecha ? DateOnly.FromDateTime(fecha) : fechaSiFalta,
                Loteria = c.Loteria,
                Numero = c.Numero,
                CubiertoPor = string.IsNullOrWhiteSpace(c.CubiertoPor) ? "-" : c.CubiertoPor,
                Subtotal = c.Subtotal,
                DescuentoPorcentaje = c.DescuentoPorcentaje ?? 0m,
                Descuento = c.DescuentoValor ?? 0m,
                Pagado = c.TotalPagar ?? 0m
            })
            .OrderByDescending(c => c.Fecha)
            .ThenBy(c => c.Loteria, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(c => c.Numero, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        var porCubierto = filasCubiertas
            .GroupBy(c => c.Loteria)
            .Select(g => (Texto: g.Key, Valor: g.Sum(c => c.Subtotal), Cantidad: g.Count()))
            .ToList();

        return new InformeEgresosObservadorResponse
        {
            Premios = premiosWeb.Select(p => new PremioInformeFilaResponse
            {
                Fecha = p.FechaPremio,
                Vendedor = p.Vendedor,
                Loteria = p.Loteria,
                Numero = p.Numero,
                ValorApostado = p.ValorApostado,
                ValorPremio = p.ValorPremio
            }).ToList(),
            PremiosPorLoteria = Barras(porLoteria),
            GastosPorTipo = Barras(gastos.Select(t => (Texto: t.Tipo, t.Valor, t.Cantidad)).ToList()),
            CubiertoPorLoteria = Barras(porCubierto),
            Cubiertos = filasCubiertas
        };
    }

    public static List<BarraInformeResponse> Barras(IReadOnlyList<(string Texto, decimal Valor, int Cantidad)> partes)
    {
        var total = partes.Sum(p => p.Valor);
        var mayor = partes.Count == 0 ? 0m : partes.Max(p => p.Valor);
        return partes
            .OrderByDescending(p => p.Valor)
            .ThenBy(p => p.Texto, StringComparer.CurrentCultureIgnoreCase)
            .Select(p => new BarraInformeResponse
            {
                Texto = p.Texto,
                Valor = p.Valor,
                Cantidad = p.Cantidad,
                Porcentaje = Porcentaje(p.Valor, total),
                Ancho = Porcentaje(p.Valor, mayor)
            })
            .ToList();
    }

    private static decimal Porcentaje(decimal parte, decimal total) =>
        total <= 0 ? 0 : Math.Round(parte * 100m / total, 1, MidpointRounding.AwayFromZero);
}
