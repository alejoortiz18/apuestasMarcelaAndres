using NewRich.Domain.Enums;

namespace NewRich.Domain.Entities;

public class CodigoPreventaOffline
{
    public Guid CodigoId { get; set; }
    public string ConsecutivoUnico { get; set; } = string.Empty;
    public Guid UsuarioId { get; set; }
    public Guid DispositivoId { get; set; }
    public byte[] PayloadCifrado { get; set; } = Array.Empty<byte>();
    public EstadoCodigoOffline EstadoDelCodigo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaDescarga { get; set; }
    public DateTime? FechaVentaOffline { get; set; }
    public DateTime? FechaRegistro { get; set; }
    public Guid? AdminQueRegistro { get; set; }
    public Guid? VentaId { get; set; }
    public Usuario? Usuario { get; set; }
    public Dispositivo? Dispositivo { get; set; }
    public Usuario? AdminQueRegistroNavigation { get; set; }
    public Venta? Venta { get; set; }
}