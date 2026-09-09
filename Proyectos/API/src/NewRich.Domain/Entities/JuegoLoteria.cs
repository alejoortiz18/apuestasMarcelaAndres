namespace NewRich.Domain.Entities;

public class JuegoLoteria
{
    public Guid JuegoId { get; set; }
    public Guid LoteriaId { get; set; }
    public Juego? Juego { get; set; }
    public Loteria? Loteria { get; set; }
}