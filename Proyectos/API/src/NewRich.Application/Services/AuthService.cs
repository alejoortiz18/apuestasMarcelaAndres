using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Auth;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class AuthService : IAuthService
{
    private static readonly Regex PasswordFuerte = new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", RegexOptions.Compiled);

    private readonly INewRichDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IClock _clock;

    public AuthService(INewRichDbContext db, IPasswordHasher hasher, IJwtTokenService jwt, IClock clock)
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

        var usuario = await _db.Usuarios
            .Include(u => u.DispositivosUsuarios)
            .ThenInclude(d => d.Dispositivo)
            .FirstOrDefaultAsync(u => u.NombreUsuario == request.Usuario, cancellationToken);

        if (usuario is null)
        {
            return Result<LoginResponse>.Fail(AuthMessages.CredencialesInvalidas, 401);
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
                FechaIntento = _clock.UtcNow,
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

        Guid? dispositivoId = null;
        if (usuario.Rol is RolUsuario.Vendedor or RolUsuario.Observador)
        {
            if (string.IsNullOrWhiteSpace(request.CodigoDispositivo))
            {
                return Result<LoginResponse>.Fail(AuthMessages.DispositivoNoRegistrado, 403);
            }

            var dispositivo = await _db.Dispositivos
                .FirstOrDefaultAsync(d => d.CodigoDispositivo == request.CodigoDispositivo, cancellationToken);

            if (dispositivo is null)
            {
                return Result<LoginResponse>.Fail(AuthMessages.DispositivoNoRegistrado, 403);
            }

            if (dispositivo.Estado != EstadoGeneral.Activo)
            {
                return Result<LoginResponse>.Fail(AuthMessages.DispositivoInactivo, 403);
            }

            var tipoEsperado = usuario.Rol == RolUsuario.Vendedor ? TipoDispositivo.Vendedor : TipoDispositivo.Observador;
            if (dispositivo.Tipo != tipoEsperado)
            {
                return Result<LoginResponse>.Fail(AuthMessages.DispositivoTipoNoCorresponde, 403);
            }

            var asociado = usuario.DispositivosUsuarios.Any(x => x.DispositivoId == dispositivo.DispositivoId && x.Activo);
            if (!asociado)
            {
                return Result<LoginResponse>.Fail(AuthMessages.DispositivoNoAsociado, 403);
            }

            dispositivoId = dispositivo.DispositivoId;

            if (usuario.Rol == RolUsuario.Vendedor && await EstaFueraDeHorario(cancellationToken))
            {
                return Result<LoginResponse>.Fail(AuthMessages.FueraDeHorarioOperacion, 403);
            }
        }

        usuario.IntentosFallidos = 0;
        usuario.FechaUltimoAcceso = _clock.UtcNow;
        _db.IntentosFallidos.Add(new IntentosFallidos
        {
            UsuarioId = usuario.UsuarioId,
            FechaIntento = _clock.UtcNow,
            Exitoso = true
        });

        var expiracion = _clock.UtcNow.AddHours(8);
        var sesion = new Sesion
        {
            SesionId = Guid.NewGuid(),
            UsuarioId = usuario.UsuarioId,
            DispositivoId = dispositivoId,
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
            dispositivoId,
            usuario.EstadoValidado), expiracion);

        return Result<LoginResponse>.Ok(new LoginResponse
        {
            Token = token,
            UsuarioId = usuario.UsuarioId,
            NombreUsuario = usuario.NombreUsuario,
            NombreCompleto = usuario.NombreCompleto,
            Rol = usuario.Rol,
            DebeCambiarPassword = usuario.EstadoValidado,
            DispositivoId = dispositivoId,
            FechaExpiracion = expiracion
        }, usuario.EstadoValidado ? AuthMessages.DebeCambiarPassword : SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<LoginResponse>> CambiarPasswordAsync(Guid usuarioId, Guid sesionId, CambiarPasswordRequest request, CancellationToken cancellationToken)
    {
        if (request.PasswordNuevo != request.PasswordConfirmacion)
        {
            return Result<LoginResponse>.Fail(AuthMessages.PasswordConfirmacionNoCoincide);
        }

        if (!PasswordFuerte.IsMatch(request.PasswordNuevo))
        {
            return Result<LoginResponse>.Fail(ValidationMessages.PasswordDebilFormato);
        }

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, cancellationToken);
        if (usuario is null)
        {
            return Result<LoginResponse>.Fail(UsuarioMessages.UsuarioNoEncontrado, 404);
        }

        if (!_hasher.Verify(request.PasswordActual, usuario.PasswordHash, usuario.PasswordSalt))
        {
            return Result<LoginResponse>.Fail(AuthMessages.PasswordActualIncorrecto);
        }

        if (_hasher.Verify(request.PasswordNuevo, usuario.PasswordHash, usuario.PasswordSalt))
        {
            return Result<LoginResponse>.Fail(AuthMessages.PasswordNuevoIgualActual);
        }

        var hashed = _hasher.Hash(request.PasswordNuevo);
        usuario.PasswordHash = hashed.Hash;
        usuario.PasswordSalt = hashed.Salt;
        usuario.EstadoValidado = false;

        var sesion = await _db.Sesiones.FirstOrDefaultAsync(s => s.SesionId == sesionId && s.UsuarioId == usuarioId, cancellationToken);
        if (sesion is null || !sesion.Activa)
        {
            return Result<LoginResponse>.Fail(AuthMessages.SesionInvalida, 401);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var token = _jwt.CreateToken(new JwtUser(
            usuario.UsuarioId,
            usuario.NombreUsuario,
            usuario.Rol,
            sesion.SesionId,
            sesion.DispositivoId,
            false), sesion.FechaExpiracion);

        return Result<LoginResponse>.Ok(new LoginResponse
        {
            Token = token,
            UsuarioId = usuario.UsuarioId,
            NombreUsuario = usuario.NombreUsuario,
            NombreCompleto = usuario.NombreCompleto,
            Rol = usuario.Rol,
            DebeCambiarPassword = false,
            DispositivoId = sesion.DispositivoId,
            FechaExpiracion = sesion.FechaExpiracion
        }, SuccessMessages.PasswordCambiado);
    }

    public async Task<Result> LogoutAsync(Guid sesionId, CancellationToken cancellationToken)
    {
        var sesion = await _db.Sesiones.FirstOrDefaultAsync(s => s.SesionId == sesionId, cancellationToken);
        if (sesion is null)
        {
            return Result.Fail(AuthMessages.SesionInvalida, 401);
        }

        sesion.Activa = false;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.SesionCerrada);
    }

    private async Task<bool> EstaFueraDeHorario(CancellationToken cancellationToken)
    {
        var hora = await _db.Configuraciones.FirstOrDefaultAsync(c => c.Clave == "HoraCierre", cancellationToken);
        if (hora is null || !TimeSpan.TryParse(hora.Valor, out var cierre))
        {
            return false;
        }

        return _clock.LocalNow.TimeOfDay > cierre;
    }
}
