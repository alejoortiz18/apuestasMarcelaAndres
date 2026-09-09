namespace NewRich.Domain.Entities;

/// <summary>Tabla dbo.Roles. Catálogo de roles asignables.</summary>
public class Rol
{
    public int RolId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
}