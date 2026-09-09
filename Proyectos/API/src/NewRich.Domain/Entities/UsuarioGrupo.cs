namespace NewRich.Domain.Entities;

public class UsuarioGrupo
{
    public Guid UsuarioId { get; set; }
    public Guid GrupoId { get; set; }
    public Usuario? Usuario { get; set; }
    public Grupo? Grupo { get; set; }
}