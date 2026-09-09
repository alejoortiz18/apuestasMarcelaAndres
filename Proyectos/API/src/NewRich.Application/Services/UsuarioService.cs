using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class UsuarioService : IUsuarioService
{
    private readonly INewRichDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;

    public UsuarioService(INewRichDbContext db, IPasswordHasher hasher, IClock clock)
    {
        _db = db;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<UsuarioResponse>>> ListarAsync(CancellationToken cancellationToken)
    {
        var items = await QueryUsuarios()
            .OrderBy(u => u.NombreCompleto)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<UsuarioResponse>>.Ok(items.Select(Map).ToList(), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<UsuarioResponse>> ObtenerAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await QueryUsuarios().FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, cancellationToken);
        return usuario is null
            ? Result<UsuarioResponse>.Fail(UsuarioMessages.UsuarioNoEncontrado, 404)
            : Result<UsuarioResponse>.Ok(Map(usuario), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<UsuarioResponse>> CrearAsync(CrearUsuarioRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NombreCompleto))
        {
            return Result<UsuarioResponse>.Fail(ValidationMessages.NombreCompletoRequerido);
        }

        if (string.IsNullOrWhiteSpace(request.Usuario))
        {
            return Result<UsuarioResponse>.Fail(ValidationMessages.NombreUsuarioRequerido);
        }

        if (await _db.Usuarios.AnyAsync(u => u.NombreUsuario == request.Usuario, cancellationToken))
        {
            return Result<UsuarioResponse>.Fail(UsuarioMessages.NombreUsuarioDuplicado, 409);
        }

        if (!string.IsNullOrWhiteSpace(request.Documento) &&
            await _db.Usuarios.AnyAsync(u => u.Documento == request.Documento, cancellationToken))
        {
            return Result<UsuarioResponse>.Fail(UsuarioMessages.DocumentoDuplicado, 409);
        }

        var temporal = GenerarPasswordTemporal();
        var hashed = _hasher.Hash(temporal);
        var usuario = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = request.NombreCompleto.Trim(),
            NombreUsuario = request.Usuario.Trim(),
            Alias = request.Alias,
            Documento = request.Documento,
            Celular = request.Celular,
            Email = request.Email,
            PasswordHash = hashed.Hash,
            PasswordSalt = hashed.Salt,
            Rol = request.Rol,
            Estado = EstadoUsuario.Activo,
            EstadoValidado = true,
            FechaCreacion = _clock.UtcNow
        };

        _db.Usuarios.Add(usuario);

        var rol = await _db.Roles.FirstOrDefaultAsync(r => r.Nombre == request.Rol.ToString(), cancellationToken);
        if (rol is not null)
        {
            _db.UsuariosRoles.Add(new UsuarioRol { UsuarioId = usuario.UsuarioId, RolId = rol.RolId });
        }

        if (request.DispositivoId.HasValue)
        {
            var asociar = await AsociarInterno(usuario.UsuarioId, request.DispositivoId.Value, cancellationToken);
            if (!asociar.IsSuccess)
            {
                return Result<UsuarioResponse>.Fail(asociar.Message, asociar.StatusCode);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        var creado = await QueryUsuarios().FirstAsync(u => u.UsuarioId == usuario.UsuarioId, cancellationToken);
        return Result<UsuarioResponse>.Created(Map(creado), SuccessMessages.UsuarioCreado + " Contraseña temporal: " + temporal);
    }

    public async Task<Result<UsuarioResponse>> ActualizarAsync(Guid usuarioId, ActualizarUsuarioRequest request, CancellationToken cancellationToken)
    {
        var usuario = await QueryUsuarios().FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, cancellationToken);
        if (usuario is null)
        {
            return Result<UsuarioResponse>.Fail(UsuarioMessages.UsuarioNoEncontrado, 404);
        }

        if (!string.IsNullOrWhiteSpace(request.Documento) &&
            await _db.Usuarios.AnyAsync(u => u.Documento == request.Documento && u.UsuarioId != usuarioId, cancellationToken))
        {
            return Result<UsuarioResponse>.Fail(UsuarioMessages.DocumentoDuplicado, 409);
        }

        usuario.NombreCompleto = request.NombreCompleto.Trim();
        usuario.Alias = request.Alias;
        usuario.Documento = request.Documento;
        usuario.Celular = request.Celular;
        usuario.Email = request.Email;
        usuario.Estado = request.Estado;

        if (request.DispositivoId.HasValue)
        {
            foreach (var asociacion in usuario.DispositivosUsuarios.Where(x => x.Activo))
            {
                asociacion.Activo = false;
            }

            var asociar = await AsociarInterno(usuarioId, request.DispositivoId.Value, cancellationToken);
            if (!asociar.IsSuccess)
            {
                return Result<UsuarioResponse>.Fail(asociar.Message, asociar.StatusCode);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<UsuarioResponse>.Ok(Map(await QueryUsuarios().FirstAsync(u => u.UsuarioId == usuarioId, cancellationToken)), SuccessMessages.UsuarioActualizado);
    }

    public async Task<Result> EliminarAsync(Guid usuarioId, Guid solicitanteId, CancellationToken cancellationToken)
    {
        if (usuarioId == solicitanteId)
        {
            return Result.Fail(UsuarioMessages.NoPuedeEliminarseASiMismo);
        }

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, cancellationToken);
        if (usuario is null)
        {
            return Result.Fail(UsuarioMessages.UsuarioNoEncontrado, 404);
        }

        if (usuario.Rol == RolUsuario.Administrador)
        {
            var otros = await _db.Usuarios.CountAsync(u => u.Rol == RolUsuario.Administrador && u.UsuarioId != usuarioId && u.Estado == EstadoUsuario.Activo, cancellationToken);
            if (otros == 0)
            {
                return Result.Fail(UsuarioMessages.NoPuedeEliminarUltimoAdministrador);
            }
        }

        await _db.ExecuteInTransactionAsync(async ct =>
        {
            var ventas = await _db.Ventas.Where(v => v.UsuarioId == usuarioId).Select(v => v.VentaId).ToListAsync(ct);
            var boletos = await _db.Boletos.Where(b => ventas.Contains(b.VentaId)).Select(b => b.BoletoId).ToListAsync(ct);
            var juegos = await _db.Juegos.Where(j => boletos.Contains(j.BoletoId)).Select(j => j.JuegoId).ToListAsync(ct);

            _db.JuegoLoterias.RemoveRange(await _db.JuegoLoterias.Where(x => juegos.Contains(x.JuegoId)).ToListAsync(ct));
            _db.Juegos.RemoveRange(await _db.Juegos.Where(x => juegos.Contains(x.JuegoId)).ToListAsync(ct));
            _db.ClavesValidacionBoleto.RemoveRange(await _db.ClavesValidacionBoleto.Where(x => boletos.Contains(x.BoletoId)).ToListAsync(ct));
            _db.Boletos.RemoveRange(await _db.Boletos.Where(x => boletos.Contains(x.BoletoId)).ToListAsync(ct));
            _db.Ventas.RemoveRange(await _db.Ventas.Where(x => ventas.Contains(x.VentaId)).ToListAsync(ct));
            _db.Sesiones.RemoveRange(await _db.Sesiones.Where(x => x.UsuarioId == usuarioId).ToListAsync(ct));
            _db.IntentosFallidos.RemoveRange(await _db.IntentosFallidos.Where(x => x.UsuarioId == usuarioId).ToListAsync(ct));
            _db.DispositivosUsuarios.RemoveRange(await _db.DispositivosUsuarios.Where(x => x.UsuarioId == usuarioId).ToListAsync(ct));
            _db.UsuariosGrupos.RemoveRange(await _db.UsuariosGrupos.Where(x => x.UsuarioId == usuarioId).ToListAsync(ct));
            _db.UsuariosRoles.RemoveRange(await _db.UsuariosRoles.Where(x => x.UsuarioId == usuarioId).ToListAsync(ct));
            _db.Notificaciones.RemoveRange(await _db.Notificaciones.Where(x => x.UsuarioId == usuarioId).ToListAsync(ct));
            _db.Usuarios.Remove(usuario);
            await _db.SaveChangesAsync(ct);
        }, cancellationToken);

        return Result.Ok(SuccessMessages.UsuarioEliminado);
    }

    public async Task<Result<RestablecerPasswordResponse>> RestablecerPasswordAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, cancellationToken);
        if (usuario is null)
        {
            return Result<RestablecerPasswordResponse>.Fail(UsuarioMessages.UsuarioNoEncontrado, 404);
        }

        if (usuario.Estado != EstadoUsuario.Activo)
        {
            return Result<RestablecerPasswordResponse>.Fail(UsuarioMessages.UsuarioDebeEstarActivo);
        }

        var temporal = GenerarPasswordTemporal();
        var hashed = _hasher.Hash(temporal);
        usuario.PasswordHash = hashed.Hash;
        usuario.PasswordSalt = hashed.Salt;
        usuario.EstadoValidado = true;
        usuario.EstadoBloqueado = false;
        usuario.IntentosFallidos = 0;

        var sesiones = await _db.Sesiones.Where(s => s.UsuarioId == usuarioId && s.Activa).ToListAsync(cancellationToken);
        foreach (var sesion in sesiones)
        {
            sesion.Activa = false;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<RestablecerPasswordResponse>.Ok(new RestablecerPasswordResponse
        {
            UsuarioId = usuarioId,
            PasswordTemporal = temporal
        }, SuccessMessages.PasswordRestablecido);
    }

    public async Task<Result> DesbloquearAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, cancellationToken);
        if (usuario is null)
        {
            return Result.Fail(UsuarioMessages.UsuarioNoEncontrado, 404);
        }

        if (!usuario.EstadoBloqueado)
        {
            return Result.Fail(UsuarioMessages.UsuarioNoBloqueado);
        }

        usuario.EstadoBloqueado = false;
        usuario.EstadoValidado = true;
        usuario.IntentosFallidos = 0;
        var sesiones = await _db.Sesiones.Where(s => s.UsuarioId == usuarioId && s.Activa).ToListAsync(cancellationToken);
        foreach (var sesion in sesiones)
        {
            sesion.Activa = false;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.UsuarioDesbloqueado);
    }

    private IQueryable<Usuario> QueryUsuarios() =>
        _db.Usuarios
            .Include(u => u.DispositivosUsuarios).ThenInclude(d => d.Dispositivo)
            .Include(u => u.UsuarioGrupos).ThenInclude(g => g.Grupo);

    private async Task<Result> AsociarInterno(Guid usuarioId, Guid dispositivoId, CancellationToken cancellationToken)
    {
        var dispositivo = await _db.Dispositivos.FirstOrDefaultAsync(d => d.DispositivoId == dispositivoId, cancellationToken);
        if (dispositivo is null)
        {
            return Result.Fail(UsuarioMessages.DispositivoNoEncontrado, 404);
        }

        var existente = await _db.DispositivosUsuarios
            .FirstOrDefaultAsync(x => x.DispositivoId == dispositivoId && x.UsuarioId == usuarioId, cancellationToken);

        if (existente is null)
        {
            _db.DispositivosUsuarios.Add(new DispositivoUsuario
            {
                DispositivoId = dispositivoId,
                UsuarioId = usuarioId,
                FechaAsociacion = _clock.UtcNow,
                Activo = true
            });
        }
        else
        {
            existente.Activo = true;
            existente.FechaAsociacion = _clock.UtcNow;
        }

        return Result.Ok(SuccessMessages.OperacionExitosa);
    }

    private static UsuarioResponse Map(Usuario usuario)
    {
        var asociacion = usuario.DispositivosUsuarios.FirstOrDefault(x => x.Activo);
        var grupo = usuario.UsuarioGrupos.FirstOrDefault()?.Grupo;
        return new UsuarioResponse
        {
            UsuarioId = usuario.UsuarioId,
            NombreCompleto = usuario.NombreCompleto,
            Usuario = usuario.NombreUsuario,
            Alias = usuario.Alias,
            Documento = usuario.Documento,
            Celular = usuario.Celular,
            Email = usuario.Email,
            Rol = usuario.Rol,
            Estado = usuario.Estado,
            DebeCambiarPassword = usuario.EstadoValidado,
            EstadoBloqueado = usuario.EstadoBloqueado,
            DispositivoId = asociacion?.DispositivoId,
            CodigoDispositivo = asociacion?.Dispositivo?.CodigoDispositivo,
            GrupoId = grupo?.GrupoId,
            GrupoNombre = grupo?.Nombre
        };
    }

    private static string GenerarPasswordTemporal()
    {
        return $"Tmp{Random.Shared.Next(100000, 999999)}!";
    }
}
