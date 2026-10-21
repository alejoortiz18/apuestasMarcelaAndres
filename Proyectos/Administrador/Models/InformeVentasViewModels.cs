namespace NewRich.Admin.Models;

public sealed class FiltrosVentasInforme
{
    public string Periodo { get; set; } = string.Empty;
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public List<Guid>? Grupos { get; set; }
    public List<string>? Secciones { get; set; }
}

public sealed class VentasInformeViewModel
{
    public bool Configurada { get; init; }
    public FiltrosVentasInforme Filtros { get; init; } = new();
    public IReadOnlyList<OpcionInforme> Periodos { get; init; } = [];
    public IReadOnlyList<OpcionInforme> Grupos { get; init; } = [];
    public IReadOnlyList<OpcionInforme> Secciones { get; init; } = [];
    public IReadOnlyList<DetalleFiltroInforme> Detalle { get; init; } = [];
    public string ResumenCorto { get; init; } = string.Empty;
    public DateOnly Hoy { get; init; }
}

public sealed class InformeVentasViewModel
{
    public DateTime Corte { get; init; }
    public DateOnly Desde { get; init; }
    public DateOnly Hasta { get; init; }
    public string Alcance { get; init; } = string.Empty;
    public bool VerIngresos { get; init; }
    public bool VerEgresos { get; init; }
    public bool VerRecaudo { get; init; }
    public ResumenFinancieroInforme Resumen { get; init; } = new();
    public IngresosInforme Ingresos { get; init; } = new();
    public EgresosInforme Egresos { get; init; } = new();
    public RecaudoInforme? Recaudo { get; init; }
}

public sealed class ResumenFinancieroInforme
{
    public decimal IngresosPda { get; init; }
    public decimal IngresosWeb { get; init; }
    public decimal TotalIngresos => IngresosPda + IngresosWeb;
    public int VentasConfirmadas { get; init; }
    public decimal PremiosObservador { get; init; }
    public decimal PremiosWeb { get; init; }
    public decimal TotalPremios => PremiosObservador + PremiosWeb;
    public int EntregasPremio { get; init; }
    public decimal TotalGastos { get; init; }
    public int CantidadGastos { get; init; }
    public decimal TotalCubierto { get; init; }
    public int JugadasCubiertas { get; init; }
    public decimal TotalEgresos => TotalPremios + TotalGastos + TotalCubierto;
    public decimal Ganancias => TotalIngresos - TotalEgresos;
    public decimal? Margen => TotalIngresos == 0 ? null : Math.Round(Ganancias * 100m / TotalIngresos, 1, MidpointRounding.AwayFromZero);
    public decimal? PesoEgresos => TotalIngresos == 0 ? null : Math.Round(TotalEgresos * 100m / TotalIngresos, 1, MidpointRounding.AwayFromZero);
}

public sealed class IngresoDiaInforme
{
    public DateOnly Fecha { get; init; }
    public decimal Total { get; init; }
    public int Ventas { get; init; }
    public decimal Alto { get; init; }
}

public sealed class VendedorIngresoInforme
{
    public int Posicion { get; init; }
    public string Vendedor { get; init; } = string.Empty;
    public string Grupo { get; init; } = string.Empty;
    public int Ventas { get; init; }
    public decimal Total { get; init; }
    public decimal Participacion { get; init; }
}

public sealed class IngresosInforme
{
    public IReadOnlyList<IngresoDiaInforme> Dias { get; init; } = [];
    public decimal PromedioDiario { get; init; }
    public decimal TicketPromedio { get; init; }
    public IngresoDiaInforme? MejorDia { get; init; }
    public int DiasConVentas { get; init; }
    public IReadOnlyList<SegmentoValorInforme> PorGrupo { get; init; } = [];
    public PagedViewModel<VendedorIngresoInforme> Vendedores { get; init; } = new();
}

public sealed class SegmentoValorInforme
{
    public string Texto { get; init; } = string.Empty;
    public decimal Valor { get; init; }
    public int Cantidad { get; init; }
    public decimal Porcentaje { get; init; }
    public decimal Ancho { get; init; }
}

public sealed class GastoPersonaInforme
{
    public string Persona { get; init; } = string.Empty;
    public int Cantidad { get; init; }
    public decimal Total { get; init; }
    public decimal Participacion { get; init; }
    public string TipoPrincipal { get; init; } = string.Empty;
}

public sealed class EgresosInforme
{
    public IReadOnlyList<SegmentoValorInforme> Conceptos { get; init; } = [];
    public IReadOnlyList<SegmentoValorInforme> GastosPorTipo { get; init; } = [];
    public PagedViewModel<GastoPersonaInforme> GastosPorPersona { get; init; } = new();
    public decimal AnuladosValor { get; init; }
    public int AnuladosCantidad { get; init; }
}

public sealed class RecaudadorInforme
{
    public string Nombre { get; init; } = string.Empty;
    public string Detalle { get; init; } = string.Empty;
    public int Vendedores { get; init; }
    public decimal PorRecaudar { get; init; }
    public decimal Recaudado { get; init; }
    public decimal Pendiente { get; init; }
    public int Avance { get; init; }
    public decimal AnchoRecaudado { get; init; }
    public decimal AnchoPendiente { get; init; }
}

public sealed class AtrasoInforme
{
    public string Vendedor { get; init; } = string.Empty;
    public string Grupo { get; init; } = string.Empty;
    public string Recaudador { get; init; } = string.Empty;
    public decimal DeudaAnterior { get; init; }
    public decimal PendienteDelDia { get; init; }
    public decimal TotalPendiente { get; init; }
}

public sealed class RecaudoInforme
{
    public decimal PorRecaudar { get; init; }
    public decimal Recaudado { get; init; }
    public decimal Pendiente { get; init; }
    public decimal DeudaAnterior { get; init; }
    public decimal PendienteDelDia { get; init; }
    public int Avance { get; init; }
    public int VendedoresAlDia { get; init; }
    public int VendedoresPorCobrar { get; init; }
    public int VendedoresEnDeuda { get; init; }
    public GraficoLineaRecaudo Grafico { get; init; } = GraficoLineaRecaudo.De([]);
    public IReadOnlyList<RecaudadorInforme> Recaudadores { get; init; } = [];
    public IReadOnlyList<AtrasoInforme> Atrasos { get; init; } = [];
    public bool Recortado { get; init; }
}
