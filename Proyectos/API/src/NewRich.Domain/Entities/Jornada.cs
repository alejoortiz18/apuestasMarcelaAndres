namespace NewRich.Domain.Entities;

/// <summary>Tabla dbo.Jornadas. Agrupa las loterías por hora de cierre: Mañana, Tarde y Noche.</summary>
public class Jornada
{
    public Guid JornadaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }

    public ICollection<Loteria> Loterias { get; set; } = new List<Loteria>();
}
