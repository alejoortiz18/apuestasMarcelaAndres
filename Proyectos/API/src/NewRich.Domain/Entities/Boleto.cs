using NewRich.Domain.Enums;

namespace NewRich.Domain.Entities;

public class Boleto
{
    public Guid BoletoId { get; set; }
    public Guid VentaId { get; set; }
    public string CodigoPublico { get; set; } = string.Empty;
    public string ClaveValidacionHash { get; set; } = string.Empty;
    public EstadoBoleto EstadoBoleto { get; set; }
    public EstadoDelPremio? EstadoDelPremio { get; set; }
    public DateTime? FechaEntregaPremio { get; set; }
    public Guid? CasoGanadorId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int VigenciaDias { get; set; }
    public Venta? Venta { get; set; }
    public ICollection<Juego> Juegos { get; set; } = new List<Juego>();
    public CasoGanador? CasoGanador { get; set; }
}
