namespace NewRich.Domain.Entities;

public class TirillaCobroRecaudo
{
    public Guid TirillaId { get; set; }
    public Guid PagoId { get; set; }
    public int Consecutivo { get; set; }
    public string RecaudadorNombre { get; set; } = string.Empty;
    public string VendedorNombre { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public decimal ValorRecibido { get; set; }
    public decimal SaldoRestante { get; set; }
}
