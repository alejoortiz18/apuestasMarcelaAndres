namespace NewRich.Domain.Entities;

public class AsignacionVendedorRecaudo
{
    public Guid AsignacionId { get; set; }
    public Guid RecaudadorId { get; set; }
    public Guid VendedorId { get; set; }
    public int Porcentaje { get; set; }
    public string Estado { get; set; } = "Activa";
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaModificacion { get; set; }
}
