namespace NewRich.Domain.Entities;

public class NumeroGanador
{
    public Guid NumeroGanadorId { get; set; }
    public Guid LoteriaId { get; set; }
    public DateTime FechaJuego { get; set; }
    public string Numero { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public Loteria? Loteria { get; set; }
}