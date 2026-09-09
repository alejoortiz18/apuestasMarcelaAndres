using NewRich.Domain.Enums;

namespace NewRich.Domain.Entities;

/// <summary>Tabla dbo.Dispositivos. PDA de vendedor u observador.</summary>
public class Dispositivo
{
    public Guid DispositivoId { get; set; }
    public string CodigoDispositivo { get; set; } = string.Empty;
    public TipoDispositivo Tipo { get; set; }
    public EstadoGeneral Estado { get; set; } = EstadoGeneral.Activo;
    public string? Modelo { get; set; }
    public string? NumeroSerie { get; set; }

    /// <summary>Tope de códigos de preventa offline que admite el equipo (RS-088).</summary>
    public int CapacidadCodigosOffline { get; set; }

    public DateTime FechaRegistro { get; set; }

    public ICollection<DispositivoUsuario> DispositivosUsuarios { get; set; } = new List<DispositivoUsuario>();
    public ICollection<Sincronizacion> Sincronizaciones { get; set; } = new List<Sincronizacion>();
}