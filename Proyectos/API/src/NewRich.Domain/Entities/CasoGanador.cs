using NewRich.Domain.Enums;

namespace NewRich.Domain.Entities;

public class CasoGanador
{
    public Guid CasoId { get; set; }
    public Guid BoletoId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public EstadoCasoGanador Estado { get; set; }
    public DateTime FechaReporte { get; set; }
    public DateTime? FechaValidacionAdmin { get; set; }
    public DateTime? FechaAsignacion { get; set; }
    public DateTime? FechaRegistro { get; set; }
    public Guid VendedorQueReporto { get; set; }
    public Guid? AdminQueValido { get; set; }
    public Guid? AdminQueAsigno { get; set; }
    public Guid? ObservadorAsignado { get; set; }
    public string? FotoTicketRuta { get; set; }
    public string? FotoTicketNombre { get; set; }
    public Boleto? Boleto { get; set; }
    public Usuario? VendedorQueReportoNavigation { get; set; }
    public Usuario? AdminQueValidoNavigation { get; set; }
    public Usuario? AdminQueAsignoNavigation { get; set; }
    public Usuario? ObservadorAsignadoNavigation { get; set; }
    public EntregaGanador? EntregaGanador { get; set; }
}