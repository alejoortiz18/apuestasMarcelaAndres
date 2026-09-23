namespace NewRich.Domain.Entities;

public class NumeroRestringido
{
    public Guid NumeroRestringidoId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}
