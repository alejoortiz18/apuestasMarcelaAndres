namespace NewRich.Domain.Entities;

public class ClaveValidacionBoleto
{
    public Guid ClaveId { get; set; }
    public Guid BoletoId { get; set; }
    public string ClaveHash { get; set; } = string.Empty;
    public int Version { get; set; }
    public Guid IdentificadorClave { get; set; }
    public DateTime FechaCreacion { get; set; }
    public Boleto? Boleto { get; set; }
}