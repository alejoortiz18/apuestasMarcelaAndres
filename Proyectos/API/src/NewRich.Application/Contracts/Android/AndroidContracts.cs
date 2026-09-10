using NewRich.Application.Contracts.Auth;
using NewRich.Domain.Enums;

namespace NewRich.Application.Contracts.Android;

public sealed class LoginAndroidResponse
{
    public string Token { get; set; } = string.Empty;
    public Guid UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; }
    public bool DebeCambiarPassword { get; set; }
    public Guid? DispositivoId { get; set; }
    public DateTime FechaExpiracion { get; set; }
    public string GrupoNombre { get; set; } = string.Empty;
    public string CodigoDispositivo { get; set; } = string.Empty;
}

public sealed class CodigoOfflineAndroidResponse
{
    public Guid CodigoId { get; set; }
    public string Consecutivo { get; set; } = string.Empty;
    public string PayloadBase64 { get; set; } = string.Empty;
}
