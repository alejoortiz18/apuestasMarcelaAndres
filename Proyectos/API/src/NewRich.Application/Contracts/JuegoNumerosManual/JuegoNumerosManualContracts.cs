namespace NewRich.Application.Contracts.JuegoNumerosManual;

/// <summary>
/// Jugada de «Juego números». Solo se registra con conexión; la fecha y hora la pone el servidor.
/// El mismo VentaId identifica los reintentos para no registrar la jugada dos veces.
/// </summary>
public sealed class RegistrarJuegoNumerosManualRequest
{
    public Guid VentaId { get; set; }
    public decimal ValorJugado { get; set; }
    public List<string> Numeros { get; set; } = [];
    public List<Guid> LoteriaIds { get; set; } = [];
}

public sealed class JuegoNumerosManualResponse
{
    public Guid VentaId { get; set; }
    public Guid BoletoId { get; set; }
    public string CodigoPublico { get; set; } = string.Empty;
    public string CodigoImpreso { get; set; } = string.Empty;
    public string Qr { get; set; } = string.Empty;
    public DateTime FechaVenta { get; set; }
    public decimal ValorJugado { get; set; }
    public int CantidadNumeros { get; set; }
    public int CantidadLoterias { get; set; }
    public int CantidadApuestas { get; set; }
    public decimal ValorPorApuesta { get; set; }
    public int ApuestasConSobrante { get; set; }
    public List<string> Numeros { get; set; } = [];
    public List<string> Loterias { get; set; } = [];
    public List<ApuestaJuegoNumerosManualResponse> Apuestas { get; set; } = [];
}

public sealed class ApuestaJuegoNumerosManualResponse
{
    public string Numero { get; set; } = string.Empty;
    public Guid LoteriaId { get; set; }
    public string Loteria { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}
