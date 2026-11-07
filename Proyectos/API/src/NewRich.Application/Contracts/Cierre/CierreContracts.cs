namespace NewRich.Application.Contracts.Cierre;

public static class ClavesCierre
{
    public const string Ventas = "ventas";
    public const string Recaudos = "recaudos";
    public const string Premios = "premios";
    public const string Gastos = "gastos";
    public const string GastosTipo = "gastos-tipo";
    public const string Cubierto = "cubierto";
    public const string Comisiones = "comisiones";
    public const string Salarios = "salarios";
    public const string Otros = "otros";

    public static readonly IReadOnlyList<string> Todas =
    [
        Ventas, Recaudos, Premios, Gastos, GastosTipo, Cubierto, Comisiones, Salarios, Otros
    ];
}

public sealed class CierreCategoriaResponse
{
    public string Clave { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal Valor { get; set; }
}

public sealed class CierreGastoTipoResponse
{
    public string Tipo { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal Valor { get; set; }
}

public sealed class CierreMovimientoResponse
{
    public DateTime Fecha { get; set; }
    public bool ConHora { get; set; }
    public string Detalle { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}

public sealed class CierreGrupoComparacionResponse
{
    public string Detalle { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal Valor { get; set; }
}

public sealed class CierrePreviaResponse
{
    public bool HayCierreAnterior { get; set; }
    public DateTime? FechaUltimoCierre { get; set; }
    public DateTime FechaCierre { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public DateTime AperturaSiguiente { get; set; }
    public int Dias { get; set; }
    public decimal TotalGanancia { get; set; }
    public IReadOnlyList<CierreCategoriaResponse> Categorias { get; set; } = [];
    public IReadOnlyList<CierreGastoTipoResponse> GastosPorTipo { get; set; } = [];
}

public sealed class ConfirmarCierreRequest
{
    public string Password { get; set; } = string.Empty;
}

public sealed class CierreResumenResponse
{
    public Guid CierreId { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public DateTime AperturaSiguiente { get; set; }
    public int Dias { get; set; }
    public DateTime FechaEjecucion { get; set; }
    public string Administrador { get; set; } = string.Empty;
    public decimal TotalVentas { get; set; }
    public decimal TotalRecaudos { get; set; }
    public decimal TotalPremios { get; set; }
    public decimal TotalGastos { get; set; }
    public decimal TotalCubierto { get; set; }
    public decimal TotalComisiones { get; set; }
    public decimal TotalSalarios { get; set; }
    public decimal TotalGanancia { get; set; }
    public decimal? VariacionGanancia { get; set; }
    public decimal? VariacionGastos { get; set; }
}

public sealed class CierreDetalleResponse
{
    public CierreResumenResponse Cierre { get; set; } = new();
    public IReadOnlyList<CierreCategoriaResponse> Categorias { get; set; } = [];
    public IReadOnlyList<CierreGastoTipoResponse> GastosPorTipo { get; set; } = [];
    public string Grupo { get; set; } = ClavesCierre.Ventas;
    public int Pagina { get; set; }
    public int Filas { get; set; }
    public int TotalFilas { get; set; }
    public IReadOnlyList<CierreMovimientoResponse> Movimientos { get; set; } = [];
    public IReadOnlyList<CierreGrupoComparacionResponse> Comparacion { get; set; } = [];
}
