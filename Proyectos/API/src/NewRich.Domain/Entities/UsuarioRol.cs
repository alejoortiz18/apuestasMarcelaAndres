namespace NewRich.Domain.Entities;

/// <summary>
/// Tabla dbo.UsuariosRoles. Relación N:N entre usuarios y roles.
/// Coexiste con la columna Usuarios.Rol, que es la que gobierna la autorización del API.
/// </summary>
public class UsuarioRol
{
    public Guid UsuarioId { get; set; }
    public int RolId { get; set; }

    public Usuario? Usuario { get; set; }
    public Rol? Rol { get; set; }
}