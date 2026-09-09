namespace NewRich.Domain.Entities;

public class Sincronizacion
{
    public Guid SincronizacionId { get; set; }
    public Guid DispositivoId { get; set; }
    public DateTime FechaSincronizacion { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Resultado { get; set; } = string.Empty;
    public Dispositivo? Dispositivo { get; set; }
}