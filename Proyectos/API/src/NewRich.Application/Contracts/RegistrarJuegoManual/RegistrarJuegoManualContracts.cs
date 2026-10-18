namespace NewRich.Application.Contracts.RegistrarJuegoManual;

public sealed class RegistrarJuegoManualRequest
{
    public List<FilaRegistrarJuegoManualRequest> Filas { get; set; } = [];
}

public sealed class FilaRegistrarJuegoManualRequest
{
    public Guid LoteriaId { get; set; }
    public decimal Valor { get; set; }
    public string Numero { get; set; } = string.Empty;
}

public sealed class RegistroJuegoManualResponse
{
    public Guid RegistroId { get; set; }
    public string CodigoPublico { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public IReadOnlyList<FilaRegistroJuegoManualResponse> Filas { get; set; } = [];
}

public sealed class FilaRegistroJuegoManualResponse
{
    public string Numero { get; set; } = string.Empty;
    public string Loteria { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}

public sealed class FilaHistoricoJuegoManualResponse
{
    public DateTime FechaRegistro { get; set; }
    public string CodigoPublico { get; set; } = string.Empty;
    public string Loteria { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}
