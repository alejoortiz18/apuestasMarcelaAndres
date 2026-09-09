namespace NewRich.Domain.Entities;

public class ConfiguracionTipoApuesta
{
    public Guid ConfiguracionTipoApuestaId { get; set; }
    public string TipoApuesta { get; set; } = string.Empty;
    public int Maximo { get; set; }
    public DateTime FechaActualizacion { get; set; }
}