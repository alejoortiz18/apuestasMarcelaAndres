namespace NewRich.Domain.Entities;

public class ObligacionRecaudo
{
    public Guid ObligacionId { get; set; }
    public Guid VendedorId { get; set; }
    public Guid RecaudadorId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal TotalVendido { get; set; }
    public int Porcentaje { get; set; }
    public decimal ValorGenerado { get; set; }
    public decimal SaldoAnterior { get; set; }
    public DateTime FechaGeneracion { get; set; }

    /// <summary>Vacío en obligaciones anteriores al porcentaje por usuario: ahí Porcentaje era el del grupo.</summary>
    public int? PorcentajeRecaudador { get; set; }
    public decimal? ComisionRecaudador { get; set; }
    public decimal? EntregarAdministracion { get; set; }
}
