namespace NewRich.Domain.Entities;

public class AsignacionGrupoRecaudo
{
    public Guid AsignacionId { get; set; }
    public Guid RecaudadorId { get; set; }
    public Guid GrupoId { get; set; }
    public int Porcentaje { get; set; }
    public string Estado { get; set; } = "Activa";
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaModificacion { get; set; }
}
