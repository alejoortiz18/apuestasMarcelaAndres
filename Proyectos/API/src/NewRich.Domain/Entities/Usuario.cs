using NewRich.Domain.Enums;

namespace NewRich.Domain.Entities;

/// <summary>
/// Tabla dbo.Usuarios. El nombre de acceso se mapea a la columna "Usuario".
/// </summary>
public class Usuario
{
    public Guid UsuarioId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Columna dbo.Usuarios.Usuario (UQ_Usuarios_Usuario).</summary>
    public string NombreUsuario { get; set; } = string.Empty;

    public string? Alias { get; set; }
    public string? Documento { get; set; }
    public string? Celular { get; set; }
    public string? Email { get; set; }

    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;

    public RolUsuario Rol { get; set; } = RolUsuario.Vendedor;
    public EstadoUsuario Estado { get; set; } = EstadoUsuario.Activo;

    /// <summary>true = contraseña temporal, obliga a cambiarla al ingresar (RS-006).</summary>
    public bool EstadoValidado { get; set; } = true;

    /// <summary>true = bloqueado tras 3 intentos fallidos consecutivos (RS-009).</summary>
    public bool EstadoBloqueado { get; set; }

    public int IntentosFallidos { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaUltimoAcceso { get; set; }

    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
    public ICollection<UsuarioGrupo> UsuarioGrupos { get; set; } = new List<UsuarioGrupo>();
    public ICollection<DispositivoUsuario> DispositivosUsuarios { get; set; } = new List<DispositivoUsuario>();
    public ICollection<Sesion> Sesiones { get; set; } = new List<Sesion>();
    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
}