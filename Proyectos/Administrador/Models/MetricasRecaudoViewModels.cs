using NewRich.Application.Contracts.Recaudo;

namespace NewRich.Admin.Models;

public sealed class MetricasRecaudoViewModel
{
    public DateOnly Desde { get; init; }
    public DateOnly Hasta { get; init; }
    public bool PeriodoRecortado { get; init; }
    public Guid? RecaudadorId { get; init; }
    public string? Grupo { get; init; }
    public DateTime Actualizado { get; init; }
    public string? Aviso { get; init; }
    public decimal TotalVendido { get; init; }
    public decimal TotalPorRecaudar { get; init; }
    public decimal TotalRecaudado { get; init; }
    public decimal TotalPendiente { get; init; }
    public decimal DeudaAnterior { get; init; }
    public decimal PendienteDelDia { get; init; }
    public int PorcentajeRecaudo { get; init; }
    public int VendedoresAlDia { get; init; }
    public int VendedoresPorCobrar { get; init; }
    public int VendedoresEnDeuda { get; init; }
    public int GruposConPendiente { get; init; }
    public int Vendedores => VendedoresAlDia + VendedoresPorCobrar + VendedoresEnDeuda;
    public GraficoLineaRecaudo Grafico { get; init; } = GraficoLineaRecaudo.De([]);
    public IReadOnlyList<BarraRecaudo> Recaudadores { get; init; } = [];
    public IReadOnlyList<BarraRecaudo> Grupos { get; init; } = [];
    public IReadOnlyList<SaldoVendedorRecaudoResponse> MayoresSaldos { get; init; } = [];
    public IReadOnlyList<OpcionRecaudoResponse> OpcionesRecaudadores { get; init; } = [];
    public IReadOnlyList<string> OpcionesGrupos { get; init; } = [];
    public LecturasRecaudo Lecturas { get; init; } = LecturasRecaudo.De([]);
    public IReadOnlyList<RangoRapidoRecaudo> Rangos { get; init; } = [];
    public bool HayFiltros => RecaudadorId is not null || Grupo is not null;

    /// <summary>Parte de un total en porcentaje (0 a 100), para anchos de barras.</summary>
    public static double Parte(decimal valor, decimal total) =>
        total <= 0m ? 0 : (double)Math.Round(Math.Clamp(valor, 0m, total) * 100m / total, 1);
}

/// <summary>Una barra horizontal del tablero: lo cobrado en verde y lo pendiente en rojo, medidos contra la barra más larga.</summary>
public sealed record BarraRecaudo(
    Guid? Id,
    string Nombre,
    string Detalle,
    int Vendedores,
    decimal PorRecaudar,
    decimal Recaudado,
    decimal Pendiente,
    int Porcentaje,
    double AnchoRecaudado,
    double AnchoPendiente)
{
    public static IReadOnlyList<BarraRecaudo> De(IEnumerable<RecaudoAgrupadoResponse> filas)
    {
        var lista = filas.ToList();
        var mayor = lista.Count == 0 ? 0m : lista.Max(f => f.TotalRecaudado + f.TotalPendiente);
        return lista
            .OrderByDescending(f => f.TotalPendiente)
            .ThenByDescending(f => f.TotalRecaudado)
            .ThenBy(f => f.Nombre)
            .Select(f => new BarraRecaudo(
                f.Id,
                f.Nombre,
                f.Detalle,
                f.Vendedores,
                f.TotalPorRecaudar,
                f.TotalRecaudado,
                f.TotalPendiente,
                f.PorcentajeRecaudo,
                MetricasRecaudoViewModel.Parte(f.TotalRecaudado, mayor),
                MetricasRecaudoViewModel.Parte(f.TotalPendiente, mayor)))
            .ToList();
    }
}

/// <summary>Datos destacados del periodo para decidir rápido.</summary>
public sealed record LecturasRecaudo(PuntoLineaRecaudo? MejorDia, PuntoLineaRecaudo? DiaMasPendiente, decimal PromedioCobrado, int DiasSinCobro)
{
    public static LecturasRecaudo De(IReadOnlyList<PuntoLineaRecaudo> puntos) => new(
        puntos.Where(p => p.Cobrado > 0m).OrderByDescending(p => p.Cobrado).ThenBy(p => p.Fecha).FirstOrDefault(),
        puntos.Where(p => p.Pendiente > 0m).OrderByDescending(p => p.Pendiente).ThenBy(p => p.Fecha).FirstOrDefault(),
        puntos.Count == 0 ? 0m : Math.Round(puntos.Sum(p => p.Cobrado) / puntos.Count, 0, MidpointRounding.AwayFromZero),
        puntos.Count(p => p.Debia > 0m && p.Cobrado <= 0m));
}

public sealed record RangoRapidoRecaudo(int Dias, string Etiqueta, DateOnly Desde, DateOnly Hasta, bool Activo);
