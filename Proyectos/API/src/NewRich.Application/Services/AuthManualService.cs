using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Auth;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public interface IAuthManualService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}

public sealed class AuthManualService : IAuthManualService
{
    private readonly INewRichDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IClock _clock;

    public AuthManualService(INewRichDbContext db, IPasswordHasher hasher, IJwtTokenService jwt, IClock clock)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
        _clock = clock;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<LoginResponse>.Fail(AuthMessages.CredencialesInvalidas, 401);
        }

        var nombreIngreso = request.Usuario.Trim().ToLowerInvariant();
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario.ToLower() == nombreIngreso, cancellationToken);
        if (usuario is null)
        {
            return Result<LoginResponse>.Fail(AuthMessages.CredencialesInvalidas, 401);
        }

        if (usuario.Rol != RolUsuario.Vendedor)
        {
            return Result<LoginResponse>.Fail(AuthMessages.CredencialesInvalidas, 403);
        }

        if (usuario.Estado == EstadoUsuario.Inactivo)
        {
            return Result<LoginResponse>.Fail(AuthMessages.UsuarioInactivo, 403);
        }

        if (usuario.EstadoBloqueado)
        {
            return Result<LoginResponse>.Fail(AuthMessages.UsuarioBloqueado, 403);
        }

        if (!_hasher.Verify(request.Password, usuario.PasswordHash, usuario.PasswordSalt))
        {
            usuario.IntentosFallidos += 1;
            _db.IntentosFallidos.Add(new IntentosFallidos
            {
                UsuarioId = usuario.UsuarioId,
                FechaIntento = _clock.LocalNow,
                Exitoso = false
            });
            if (usuario.IntentosFallidos >= 3)
            {
                usuario.EstadoBloqueado = true;
                await _db.SaveChangesAsync(cancellationToken);
                return Result<LoginResponse>.Fail(AuthMessages.UsuarioBloqueadoAhora, 403);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return Result<LoginResponse>.Fail(AuthMessages.CredencialesInvalidas, 401);
        }

        usuario.IntentosFallidos = 0;
        usuario.FechaUltimoAcceso = _clock.LocalNow;
        _db.IntentosFallidos.Add(new IntentosFallidos
        {
            UsuarioId = usuario.UsuarioId,
            FechaIntento = _clock.LocalNow,
            Exitoso = true
        });
        var expiracion = _clock.UtcNow.AddHours(8);
        var sesion = new Sesion
        {
            SesionId = Guid.NewGuid(),
            UsuarioId = usuario.UsuarioId,
            DispositivoId = null,
            Token = Guid.NewGuid().ToString("N"),
            FechaInicio = _clock.UtcNow,
            FechaExpiracion = expiracion,
            Activa = true
        };
        _db.Sesiones.Add(sesion);
        await _db.SaveChangesAsync(cancellationToken);
        var token = _jwt.CreateToken(new JwtUser(
            usuario.UsuarioId,
            usuario.NombreUsuario,
            usuario.Rol,
            sesion.SesionId,
            null,
            usuario.EstadoValidado), expiracion);
        return Result<LoginResponse>.Ok(new LoginResponse
        {
            Token = token,
            UsuarioId = usuario.UsuarioId,
            NombreUsuario = usuario.NombreUsuario,
            NombreCompleto = usuario.NombreCompleto,
            Rol = usuario.Rol,
            DebeCambiarPassword = usuario.EstadoValidado,
            DispositivoId = null,
            FechaExpiracion = expiracion
        }, usuario.EstadoValidado ? AuthMessages.DebeCambiarPassword : SuccessMessages.OperacionExitosa);
    }

}
