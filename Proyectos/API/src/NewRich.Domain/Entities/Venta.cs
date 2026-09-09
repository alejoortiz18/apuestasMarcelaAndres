using NewRich.Domain.Enums;

namespace NewRich.Domain.Entities;

public class Venta
{
    public Guid VentaId { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid? DispositivoId { get; set; }
    public DateTime FechaVenta { get; set; }
    public decimal Total { get; set; }
    public TipoApuesta TipoApuesta { get; set; }
    public string? EstadoSincronizacion { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTime? FechaSincronizacion { get; set; }
    public Usuario? Usuario { get; set; }
    public Dispositivo? Dispositivo { get; set; }
    public ICollection<Boleto> Boletos { get; set; } = new List<Boleto>();
}