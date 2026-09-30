namespace NewRich.Domain.Entities;

public class PorcentajeGrupoRecaudo
{
    public Guid GrupoId { get; set; }
    public int Porcentaje { get; set; }
    public DateTime FechaModificacion { get; set; }
}
