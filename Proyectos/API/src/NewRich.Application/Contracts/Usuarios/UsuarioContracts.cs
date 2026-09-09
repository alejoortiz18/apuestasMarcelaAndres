using NewRich.Domain.Enums;

namespace NewRich.Application.Contracts.Usuarios;

public sealed class CrearUsuarioRequest
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public string? Documento { get; set; }
    public string? Celular { get; set; }
    public string? Email { get; set; }
    public RolUsuario Rol { get; set; }
    public Guid? DispositivoId { get; set; }
}

public sealed class ActualizarUsuarioRequest
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public string? Documento { get; set; }
    public string? Celular { get; set; }
    public string? Email { get; set; }
    public EstadoUsuario Estado { get; set; }
    public Guid? DispositivoId { get; set; }
}

public sealed class UsuarioResponse
{
    public Guid UsuarioId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public string? Documento { get; set; }
    public string? Celular { get; set; }
    public string? Email { get; set; }
    public RolUsuario Rol { get; set; }
    public EstadoUsuario Estado { get; set; }
    public bool DebeCambiarPassword { get; set; }
    public bool EstadoBloqueado { get; set; }
    public Guid? DispositivoId { get; set; }
    public string? CodigoDispositivo { get; set; }
    public Guid? GrupoId { get; set; }
    public string? GrupoNombre { get; set; }
}

public sealed class RestablecerPasswordResponse
{
    public Guid UsuarioId { get; set; }
    public string PasswordTemporal { get; set; } = string.Empty;
}
