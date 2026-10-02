namespace NewRich.Domain.Entities;

/// <summary>Tabla dbo.Jornadas. Categoría de lotería configurable por el administrador.</summary>
public class Jornada
{
    public Guid JornadaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }

    public ICollection<Loteria> Loterias { get; set; } = new List<Loteria>();
}
