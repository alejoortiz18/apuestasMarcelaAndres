namespace NewRich.Application.Contracts.RevisadaManual;

public sealed class MarcarPorCubrirRevisadaManualRequest
{
    public List<string> Claves { get; set; } = [];
}

public sealed class CubrirRevisadaManualRequest
{
    public Guid OperacionId { get; set; }
    public decimal ComisionPorcentaje { get; set; }
}

public sealed class DescuentoRevisadaManualRequest
{
    public Guid OperacionId { get; set; }
    public decimal DescuentoPorcentaje { get; set; }
}

public sealed class VistaRevisadaManualResponse
{
    public int Jugadas { get; set; }
    public int Loterias { get; set; }
    public decimal Total { get; set; }
    public List<GrupoRevisadaManualResponse> Grupos { get; set; } = [];
}

public sealed class GrupoRevisadaManualResponse
{
    public Guid? OperacionId { get; set; }
    public string Loteria { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal? ComisionPorcentaje { get; set; }
    public decimal? ComisionValor { get; set; }
    public decimal? TotalConComision { get; set; }
    public decimal? DescuentoPorcentaje { get; set; }
    public decimal? DescuentoValor { get; set; }
    public decimal? TotalPagar { get; set; }
    public List<LineaRevisadaManualResponse> Lineas { get; set; } = [];
}

public sealed class LineaRevisadaManualResponse
{
    public string Clave { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime Fecha { get; set; }
}
