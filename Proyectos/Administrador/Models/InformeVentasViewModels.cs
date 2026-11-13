namespace NewRich.Admin.Models;

public sealed class FiltrosPeriodoInforme
{
    public string Periodo { get; set; } = string.Empty;
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public List<Guid>? Grupos { get; set; }
    public List<Guid>? Vendedores { get; set; }

    /// <summary>Solo para Egresos: premios, gastos y cubierto. Null es todos.</summary>
    public List<string>? TiposEgreso { get; set; }
}

public sealed class PeriodoInformeViewModel
{
    public string Clave { get; init; } = string.Empty;
    public bool Configurada { get; init; }
    public FiltrosPeriodoInforme Filtros { get; init; } = new();
    public IReadOnlyList<OpcionInforme> Periodos { get; init; } = [];
    public IReadOnlyList<OpcionInforme> Grupos { get; init; } = [];
    public IReadOnlyList<OpcionInforme> Vendedores { get; init; } = [];
    public IReadOnlyList<DetalleFiltroInforme> Detalle { get; init; } = [];
    public string ResumenCorto { get; init; } = string.Empty;
    public DateOnly Hoy { get; init; }
}

public sealed class ModalPeriodoInforme
{
    public PeriodoInformeViewModel Config { get; init; } = new();
    public string Prefijo { get; init; } = string.Empty;
    public string Titulo { get; init; } = string.Empty;
    public string Ayuda { get; init; } = string.Empty;
    public string NotaGrupos { get; init; } = string.Empty;
}

public abstract class InformePorPeriodo
{
    public DateTime Corte { get; init; }
    public DateOnly Desde { get; init; }
    public DateOnly Hasta { get; init; }
    public string Alcance { get; init; } = string.Empty;
}

public sealed class InformeVentasViewModel : InformePorPeriodo
{
    public ResumenFinancieroInforme Resumen { get; init; } = new();
    public IngresosInforme Ingresos { get; init; } = new();
}

public sealed class InformeEgresosViewModel : InformePorPeriodo
{
    public ResumenFinancieroInforme Resumen { get; init; } = new();
    public EgresosInforme Egresos { get; init; } = new();
}

public sealed class InformeRecaudoViewModel : InformePorPeriodo
{
    public RecaudoInforme? Recaudo { get; init; }
}

public sealed class InformeGeneralViewModel : InformePorPeriodo
{
    public decimal TotalPorRecibir { get; init; }
    public decimal Recibido { get; init; }
    public bool EnDeuda { get; init; }
    public bool Recortado { get; init; }
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
    public decimal TotalCubiertoPda { get; init; }
    public int JugadasCubiertasPda { get; init; }
    public decimal TotalEgresos => TotalPremios + TotalGastos + TotalCubierto + TotalCubiertoPda;
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
    public DateTime FechaVenta { get; init; }
    public string Vendedor { get; init; } = string.Empty;
    public string Grupo { get; init; } = string.Empty;
    public decimal Total { get; init; }
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
    public EstadoOrden OrdenRanking { get; init; } = new("posicion", "asc");
}

public sealed class SegmentoValorInforme
{
    public string Texto { get; init; } = string.Empty;
    public decimal Valor { get; init; }
    public int Cantidad { get; init; }
    public decimal Porcentaje { get; init; }
    public decimal Ancho { get; init; }
}

/// <summary>Un gasto activo, sin agrupar: de qué fecha, quién lo registró, de qué tipo y por cuánto.</summary>
public sealed class GastoPersonaInforme
{
    public DateOnly Fecha { get; init; }
    public string Persona { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public decimal Total { get; init; }
}

/// <summary>Un número cubierto en Revisada manual: cuándo, de qué lotería, quién lo cubrió y cuánto se pagó.</summary>
public sealed class CubiertoInforme
{
    public DateOnly Fecha { get; init; }
    public string Loteria { get; init; } = string.Empty;
    public string Numero { get; init; } = string.Empty;
    public string CubiertoPor { get; init; } = string.Empty;
    public decimal Subtotal { get; init; }
    public decimal SubtotalConComision { get; init; }
    public decimal? DescuentoPorcentaje { get; init; }
    public decimal Descuento { get; init; }
    public decimal Pagado { get; init; }
}

public sealed class PremioWebInforme
{
    public DateOnly Fecha { get; init; }
    public string Vendedor { get; init; } = string.Empty;
    public string Loteria { get; init; } = string.Empty;
    public string Numero { get; init; } = string.Empty;
    public decimal ValorApostado { get; init; }
    public decimal ValorPremio { get; init; }
    public string Entrega { get; init; } = string.Empty;
}

public sealed class EgresosInforme
{
    public IReadOnlyList<SegmentoValorInforme> Conceptos { get; init; } = [];
    public IReadOnlyList<SegmentoValorInforme> GastosPorTipo { get; init; } = [];
    public PagedViewModel<PremioWebInforme> PremiosWeb { get; init; } = new();
    public string OrdenPremios { get; init; } = "fecha";
    public string DireccionPremios { get; init; } = "desc";
    public string OrdenGastos { get; init; } = "total";
    public string DireccionGastos { get; init; } = "desc";
    public PagedViewModel<GastoPersonaInforme> GastosPorPersona { get; init; } = new();
    public decimal AnuladosValor { get; init; }
    public int AnuladosCantidad { get; init; }
    public IReadOnlyList<SegmentoValorInforme> PremiosPorLoteria { get; init; } = [];
    public IReadOnlyList<SegmentoValorInforme> CubiertoPorLoteria { get; init; } = [];
    public PagedViewModel<CubiertoInforme> Cubiertos { get; init; } = new();
    public EstadoOrden OrdenCubiertos { get; init; } = new("fecha", "desc");
    public PagedViewModel<CubiertoInforme> CubiertosPda { get; init; } = new();
    public EstadoOrden OrdenCubiertosPda { get; init; } = new("fecha", "desc");
    public bool IncluyePremios { get; init; } = true;
    public bool IncluyeGastos { get; init; } = true;
    public bool IncluyeCubierto { get; init; } = true;
    public bool IncluyeCubiertoPda { get; init; } = true;
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

public sealed class RecaudadorDeudaInforme
{
    public string Nombre { get; init; } = string.Empty;
    public string Grupos { get; init; } = string.Empty;
    public decimal Recibir { get; init; }
    public decimal Entregado { get; init; }
    public decimal Debe { get; init; }
}

public sealed class RecaudoInforme
{
    public decimal PorRecaudar { get; init; }
    public decimal Recaudado { get; init; }
    public decimal Pendiente { get; init; }
    public decimal DeudaAnterior { get; init; }
    public decimal PendienteDelDia { get; init; }
    public decimal DebeEntregar { get; init; }
    public decimal ValorEntregado { get; init; }
    public bool EnDeudaAdministracion { get; init; }
    public int Avance { get; init; }
    public int VendedoresAlDia { get; init; }
    public int VendedoresPorCobrar { get; init; }
    public int VendedoresEnDeuda { get; init; }
    public GraficoLineaRecaudo Grafico { get; init; } = GraficoLineaRecaudo.De([]);
    public IReadOnlyList<RecaudadorInforme> Recaudadores { get; init; } = [];
    public PagedViewModel<AtrasoInforme> Atrasos { get; init; } = new();
    public EstadoOrden OrdenAtrasos { get; init; } = new("pendiente", "desc");
    public PagedViewModel<RecaudadorDeudaInforme> RecaudadoresDeuda { get; init; } = new();
    public bool Recortado { get; init; }
}
