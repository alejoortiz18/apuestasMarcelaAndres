using NewRich.Domain.Enums;

namespace NewRich.Application.Contracts.Auth;

public sealed class LoginRequest
{
    public string Usuario { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? CodigoDispositivo { get; set; }
    public PruebaLlaveAdministradorRequest? PruebaLlave { get; set; }
}

public sealed class PruebaLlaveAdministradorRequest
{
    public string Codigo { get; set; } = string.Empty;
    public string HuellaDispositivo { get; set; } = string.Empty;
    public string Firma { get; set; } = string.Empty;
    public long Unix { get; set; }
}

public sealed class CambiarPasswordRequest
{
    public string PasswordActual { get; set; } = string.Empty;
    public string PasswordNuevo { get; set; } = string.Empty;
    public string PasswordConfirmacion { get; set; } = string.Empty;
}

public sealed class ConfirmarAccionRequest
{
    public string Password { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public int Usos { get; set; } = 1;
}

public sealed class ConfirmarAccionResponse
{
    public string Token { get; set; } = string.Empty;
}

public sealed class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public Guid UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; }
    public bool DebeCambiarPassword { get; set; }
    public Guid? DispositivoId { get; set; }
    public DateTime FechaExpiracion { get; set; }
}
