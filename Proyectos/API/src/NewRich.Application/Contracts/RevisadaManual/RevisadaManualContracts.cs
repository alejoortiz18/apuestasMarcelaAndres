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

public sealed class CubrirDesdeResumenRevisadaManualRequest
{
    public List<string> Claves { get; set; } = [];
    public decimal ComisionPorcentaje { get; set; }
    public decimal DescuentoPorcentaje { get; set; }
}

public sealed class RecubrirRevisadaManualRequest
{
    public List<string> Claves { get; set; } = [];
    public decimal ComisionPorcentaje { get; set; }
    public decimal DescuentoPorcentaje { get; set; }
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
    public DateTime? FechaOperacion { get; set; }
    public List<LineaRevisadaManualResponse> Lineas { get; set; } = [];
}

/// <summary>Fila del histórico de Cubierto: lo cubierto de una lotería y número en un mismo día.</summary>
public sealed class FilaCubiertoHistoricoResponse
{
    public DateTime Fecha { get; set; }
    public string Loteria { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public int Registros { get; set; }
    public decimal Subtotal { get; set; }
    public List<decimal> ComisionPorcentajes { get; set; } = [];
    public decimal ComisionValor { get; set; }
    public decimal TotalConComision { get; set; }
    public List<decimal> DescuentoPorcentajes { get; set; } = [];
    public decimal DescuentoValor { get; set; }
    public decimal TotalPagar { get; set; }
    public List<GrupoRevisadaManualResponse> Cubrimientos { get; set; } = [];
}

public sealed class CubiertoHistoricoResponse
{
    public int TotalFilas { get; set; }
    public int Registros { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ComisionValor { get; set; }
    public decimal TotalConComision { get; set; }
    public decimal DescuentoValor { get; set; }
    public decimal TotalPagar { get; set; }
    public bool HayMas { get; set; }
    public List<FilaCubiertoHistoricoResponse> Filas { get; set; } = [];
}

/// <summary>Fila del histórico de Resumen: apuestas de una lotería y número en un mismo día, pendientes o cubiertas.</summary>
public sealed class FilaResumenHistoricoResponse
{
    public DateTime Fecha { get; set; }
    public string Loteria { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int Registros { get; set; }
    public decimal Subtotal { get; set; }
    public List<LineaRevisadaManualResponse> Lineas { get; set; } = [];
}

public sealed class ResumenHistoricoResponse
{
    public int TotalFilas { get; set; }
    public int Registros { get; set; }
    public decimal Subtotal { get; set; }
    public decimal SubtotalPendiente { get; set; }
    public decimal SubtotalCubierto { get; set; }
    public bool HayMas { get; set; }
    public List<FilaResumenHistoricoResponse> Filas { get; set; } = [];
}

public sealed class LineaRevisadaManualResponse
{
    public string Clave { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime Fecha { get; set; }
}
