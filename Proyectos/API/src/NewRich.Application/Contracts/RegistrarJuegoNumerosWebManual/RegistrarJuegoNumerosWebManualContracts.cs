namespace NewRich.Application.Contracts.RegistrarJuegoNumerosWebManual;

public sealed class RegistrarJuegoNumerosWebManualRequest
{
    public Guid RegistroId { get; set; }
    public decimal ValorJugado { get; set; }
    public List<string> Numeros { get; set; } = [];
    public List<Guid> LoteriaIds { get; set; } = [];
}

public sealed class RegistroJuegoNumerosWebManualResponse
{
    public Guid RegistroId { get; set; }
    public string CodigoPublico { get; set; } = string.Empty;
    public string Qr { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public decimal ValorJugado { get; set; }
    public int CantidadNumeros { get; set; }
    public int CantidadLoterias { get; set; }
    public int CantidadApuestas { get; set; }
    public decimal ValorPorApuesta { get; set; }
    public int ApuestasConSobrante { get; set; }
    public List<string> Numeros { get; set; } = [];
    public List<string> Loterias { get; set; } = [];
    public List<ApuestaJuegoNumerosWebManualResponse> Apuestas { get; set; } = [];
}

public sealed class ApuestaJuegoNumerosWebManualResponse
{
    public string Loteria { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}
