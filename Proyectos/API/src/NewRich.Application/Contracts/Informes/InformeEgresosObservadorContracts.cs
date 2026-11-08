namespace NewRich.Application.Contracts.Informes;

public sealed class BarraInformeResponse
{
    public string Texto { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int Cantidad { get; set; }
    public decimal Porcentaje { get; set; }
    public decimal Ancho { get; set; }
}

public sealed class PremioInformeFilaResponse
{
    public DateOnly Fecha { get; set; }
    public string Vendedor { get; set; } = string.Empty;
    public string Loteria { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public decimal ValorApostado { get; set; }
    public decimal ValorPremio { get; set; }
}

public sealed class CubiertoInformeFilaResponse
{
    public DateOnly Fecha { get; set; }
    public string Loteria { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string CubiertoPor { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal DescuentoPorcentaje { get; set; }
    public decimal Descuento { get; set; }
    public decimal Pagado { get; set; }
}

public sealed class InformeEgresosObservadorResponse
{
    public List<PremioInformeFilaResponse> Premios { get; set; } = [];
    public List<BarraInformeResponse> PremiosPorLoteria { get; set; } = [];
    public List<BarraInformeResponse> GastosPorTipo { get; set; } = [];
    public List<BarraInformeResponse> CubiertoPorLoteria { get; set; } = [];
    public List<CubiertoInformeFilaResponse> Cubiertos { get; set; } = [];
}
