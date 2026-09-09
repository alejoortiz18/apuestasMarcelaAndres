namespace NewRich.Domain.Entities;

public class Configuracion
{
    public Guid ConfiguracionId { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public DateTime FechaActualizacion { get; set; }
}