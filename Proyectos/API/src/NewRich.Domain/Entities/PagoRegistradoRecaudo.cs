namespace NewRich.Domain.Entities;

public class PagoRegistradoRecaudo
{
    public Guid PagoId { get; set; }
    public Guid ObligacionId { get; set; }
    public Guid RecaudadorId { get; set; }
    public Guid VendedorId { get; set; }
    public decimal Valor { get; set; }
    public decimal SaldoResultante { get; set; }
    public string ClaveIdempotencia { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
}
