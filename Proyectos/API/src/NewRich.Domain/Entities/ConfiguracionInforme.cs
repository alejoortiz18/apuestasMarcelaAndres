namespace NewRich.Domain.Entities;

public class ConfiguracionInforme
{
    public Guid ConfiguracionId { get; set; }
    public Guid UsuarioId { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Filtros { get; set; } = string.Empty;
    public DateTime FechaActualizacionUtc { get; set; }
}
