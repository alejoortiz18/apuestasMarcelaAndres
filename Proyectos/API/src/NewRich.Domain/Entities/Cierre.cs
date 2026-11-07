namespace NewRich.Domain.Entities;

public class Cierre
{
    public Guid CierreId { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int Dias { get; set; }
    public DateTime FechaEjecucion { get; set; }
    public Guid UsuarioId { get; set; }
    public decimal TotalVentas { get; set; }
    public int CantidadVentas { get; set; }
    public decimal TotalRecaudos { get; set; }
    public int CantidadRecaudos { get; set; }
    public decimal TotalPremios { get; set; }
    public int CantidadPremios { get; set; }
    public decimal TotalGastos { get; set; }
    public int CantidadGastos { get; set; }
    public decimal TotalCubierto { get; set; }
    public int CantidadCubierto { get; set; }
    public decimal TotalComisiones { get; set; }
    public int CantidadComisiones { get; set; }
    public decimal TotalSalarios { get; set; }
    public int CantidadSalarios { get; set; }
    public decimal TotalGanancia { get; set; }
    public Usuario? Usuario { get; set; }
}
