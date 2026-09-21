using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Auth;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class AuthService : IAuthService
{
    private static readonly Regex PasswordFuerte = new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", RegexOptions.Compiled);

    private readonly INewRichDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IClock _clock;
    private readonly IConfirmacionAccionStore _confirmaciones;

    public AuthService(INewRichDbContext db, IPasswordHasher hasher, IJwtTokenService jwt, IClock clock, IConfirmacionAccionStore confirmaciones)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
        _clock = clock;
        _confirmaciones = confirmaciones;
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

        if (usuario.Rol == RolUsuario.Administrador)
        {
            var llave = await ValidarLlaveAdministradorAsync(usuario, request.PruebaLlave, cancellationToken);
            if (!llave.IsSuccess)
            {
                return Result<LoginResponse>.Fail(llave.Message, llave.StatusCode);
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

    public async Task<Result<ConfirmarAccionResponse>> ConfirmarAccionAdministrativaAsync(Guid usuarioId, ConfirmarAccionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Accion))
        {
            return Result<ConfirmarAccionResponse>.Fail(AuthMessages.ConfirmacionAccionRequerida, 400);
        }

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, cancellationToken);
        if (usuario is null)
        {
            return Result<ConfirmarAccionResponse>.Fail(AuthMessages.SesionInvalida, 401);
        }

        if (!RolConsola.EsEquipoAdministrativo(usuario.Rol))
        {
            return Result<ConfirmarAccionResponse>.Fail(AuthMessages.SoloAdministrador, 403);
        }

        if (!_hasher.Verify(request.Password, usuario.PasswordHash, usuario.PasswordSalt))
        {
            return Result<ConfirmarAccionResponse>.Fail(AuthMessages.ContrasenaAccionIncorrecta, 403);
        }

        var token = _confirmaciones.Emitir(usuarioId, request.Accion.Trim(), request.Usos);
        return Result<ConfirmarAccionResponse>.Ok(new ConfirmarAccionResponse { Token = token }, SuccessMessages.OperacionExitosa);
    }

    private async Task<bool> EstaFueraDeHorario(CancellationToken cancellationToken)
    {
        var aperturaCfg = await _db.Configuraciones.FirstOrDefaultAsync(c => c.Clave == "HoraApertura", cancellationToken);
        var cierreCfg = await _db.Configuraciones.FirstOrDefaultAsync(c => c.Clave == "HoraCierre", cancellationToken);
        if (aperturaCfg is null || cierreCfg is null
            || !TimeSpan.TryParse(aperturaCfg.Valor, out var apertura)
            || !TimeSpan.TryParse(cierreCfg.Valor, out var cierre))
        {
            return false;
        }

        return HorarioOperacion.EstaFuera(_clock.LocalNow.TimeOfDay, apertura, cierre);
    }

    private async Task<Result> ValidarLlaveAdministradorAsync(Usuario usuario, PruebaLlaveAdministradorRequest? prueba, CancellationToken cancellationToken)
    {
        var activa = await _db.LlavesAdministrador
            .Where(l => l.UsuarioId == usuario.UsuarioId && l.Estado == EstadoLlaveAdministrador.Activa)
            .OrderByDescending(l => l.FechaActivacion)
            .FirstOrDefaultAsync(cancellationToken);

        if (activa is null)
        {
            return Result.Fail(AuthMessages.LlaveNoValida, 403);
        }

        if (prueba is null
            || string.IsNullOrWhiteSpace(prueba.Codigo)
            || string.IsNullOrWhiteSpace(prueba.Firma)
            || string.IsNullOrWhiteSpace(prueba.HuellaDispositivo))
        {
            return Result.Fail(AuthMessages.LlaveNoDetectada, 403);
        }

        if (!string.Equals(prueba.Codigo, activa.Codigo, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Fail(AuthMessages.LlaveNoCorresponde, 403);
        }

        var unixAhora = new DateTimeOffset(_clock.UtcNow).ToUnixTimeSeconds();
        if (Math.Abs(unixAhora - prueba.Unix) > 300)
        {
            return Result.Fail(AuthMessages.LlavePruebaInvalida, 403);
        }

        if (!string.Equals(prueba.HuellaDispositivo, activa.HuellaDispositivo, StringComparison.OrdinalIgnoreCase))
        {
            activa.Estado = EstadoLlaveAdministrador.Comprometida;
            activa.FechaRevocacion = _clock.UtcNow;
            activa.MotivoRevocacion = "Huella de dispositivo no coincide";
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Fail(AuthMessages.LlaveNoValida, 403);
        }

        var payload = LlaveUsbCriptografia.PayloadLogin(activa.Codigo, usuario.NombreUsuario, activa.HuellaDispositivo, prueba.Unix);
        if (!LlaveUsbCriptografia.Verificar(activa.ClavePublica, payload, prueba.Firma))
        {
            return Result.Fail(AuthMessages.LlavePruebaInvalida, 403);
        }

        activa.FechaUltimoUso = _clock.UtcNow;
        return Result.Ok(SuccessMessages.OperacionExitosa);
    }
}
