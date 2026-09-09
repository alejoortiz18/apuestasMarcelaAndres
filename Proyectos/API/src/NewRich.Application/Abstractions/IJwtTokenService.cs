using NewRich.Domain.Enums;

namespace NewRich.Application.Abstractions;

public sealed record JwtUser(
    Guid UsuarioId,
    string NombreUsuario,
    RolUsuario Rol,
    Guid SesionId,
    Guid? DispositivoId,
    bool DebeCambiarPassword);

public interface IJwtTokenService
{
    string CreateToken(JwtUser user, DateTime expiresAt);
}
