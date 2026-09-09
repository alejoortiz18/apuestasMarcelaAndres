namespace NewRich.Application.Contracts.Resultados;

public sealed class RegistrarResultadoRequest
{
    public Guid LoteriaId { get; set; }
    public DateOnly FechaJuego { get; set; }
    public string Numero { get; set; } = string.Empty;
}

public sealed class ResultadoResponse
{
    public Guid NumeroGanadorId { get; set; }
    public Guid LoteriaId { get; set; }
    public string Loteria { get; set; } = string.Empty;
    public DateOnly FechaJuego { get; set; }
    public string Numero { get; set; } = string.Empty;
}
