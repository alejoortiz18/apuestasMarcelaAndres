using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Dispositivos;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class DispositivoService : IDispositivoService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;

    public DispositivoService(INewRichDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<DispositivoResponse>>> ListarAsync(CancellationToken cancellationToken)
    {
        var items = await _db.Dispositivos
            .Include(d => d.DispositivosUsuarios)
            .ThenInclude(x => x.Usuario)
            .ToListAsync(cancellationToken);

        var sesiones = await _db.Sesiones
            .Where(s => s.Activa && s.FechaExpiracion > _clock.UtcNow && s.DispositivoId != null)
            .Select(s => s.DispositivoId!.Value)
            .ToListAsync(cancellationToken);

        var response = items.Select(d => Map(d, sesiones.Contains(d.DispositivoId))).ToList();
        return Result<IReadOnlyList<DispositivoResponse>>.Ok(response, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<DispositivoResponse>> CrearAsync(CrearDispositivoRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CodigoDispositivo))
        {
            return Result<DispositivoResponse>.Fail(ValidationMessages.CodigoDispositivoRequerido);
        }

        if (await _db.Dispositivos.AnyAsync(d => d.CodigoDispositivo == request.CodigoDispositivo, cancellationToken))
        {
            return Result<DispositivoResponse>.Fail(UsuarioMessages.DispositivoCodigoDuplicado, 409);
        }

        if (!string.IsNullOrWhiteSpace(request.NumeroSerie) &&
            await _db.Dispositivos.AnyAsync(d => d.NumeroSerie == request.NumeroSerie, cancellationToken))
        {
            return Result<DispositivoResponse>.Fail(UsuarioMessages.DispositivoSerieDuplicada, 409);
        }

        var dispositivo = new Dispositivo
        {
            DispositivoId = Guid.NewGuid(),
            CodigoDispositivo = request.CodigoDispositivo.Trim(),
            Tipo = request.Tipo,
            Estado = EstadoGeneral.Activo,
            Modelo = request.Modelo,
            NumeroSerie = request.NumeroSerie,
            CapacidadCodigosOffline = 3000,
            FechaRegistro = _clock.UtcNow
        };
        _db.Dispositivos.Add(dispositivo);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<DispositivoResponse>.Created(Map(dispositivo, false), SuccessMessages.RegistroCreado);
    }

    public async Task<Result<DispositivoResponse>> ActualizarAsync(Guid dispositivoId, ActualizarDispositivoRequest request, CancellationToken cancellationToken)
    {
        var dispositivo = await _db.Dispositivos
            .Include(d => d.DispositivosUsuarios)
            .ThenInclude(x => x.Usuario)
            .FirstOrDefaultAsync(d => d.DispositivoId == dispositivoId, cancellationToken);

        if (dispositivo is null)
        {
            return Result<DispositivoResponse>.Fail(UsuarioMessages.DispositivoNoEncontrado, 404);
        }

        dispositivo.Modelo = request.Modelo;
        dispositivo.Estado = request.Estado;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<DispositivoResponse>.Ok(Map(dispositivo, false), SuccessMessages.RegistroActualizado);
    }

    public async Task<Result> AsociarAsync(Guid dispositivoId, Guid usuarioId, CancellationToken cancellationToken)
    {
        var dispositivo = await _db.Dispositivos.FirstOrDefaultAsync(d => d.DispositivoId == dispositivoId, cancellationToken);
        if (dispositivo is null)
        {
            return Result.Fail(UsuarioMessages.DispositivoNoEncontrado, 404);
        }

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, cancellationToken);
        if (usuario is null)
        {
            return Result.Fail(UsuarioMessages.UsuarioNoEncontrado, 404);
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
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.RegistroActualizado);
    }

    public async Task<Result> DesasociarAsync(Guid dispositivoId, Guid usuarioId, CancellationToken cancellationToken)
    {
        var existente = await _db.DispositivosUsuarios
            .FirstOrDefaultAsync(x => x.DispositivoId == dispositivoId && x.UsuarioId == usuarioId, cancellationToken);

        if (existente is null)
        {
            return Result.Fail(UsuarioMessages.DispositivoAsociacionNoEncontrada, 404);
        }

        existente.Activo = false;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.RegistroActualizado);
    }

    private static DispositivoResponse Map(Dispositivo dispositivo, bool conectado)
    {
        var asociacion = dispositivo.DispositivosUsuarios.FirstOrDefault(x => x.Activo);
        return new DispositivoResponse
        {
            DispositivoId = dispositivo.DispositivoId,
            CodigoDispositivo = dispositivo.CodigoDispositivo,
            Tipo = dispositivo.Tipo,
            Estado = dispositivo.Estado,
            Modelo = dispositivo.Modelo,
            NumeroSerie = dispositivo.NumeroSerie,
            UsuarioAsociadoId = asociacion?.UsuarioId,
            UsuarioAsociado = asociacion?.Usuario?.NombreCompleto,
            Conectado = conectado
        };
    }
}
