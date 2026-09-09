using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Notificaciones;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class NotificacionService : INotificacionService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;
    private readonly INotificacionTiempoReal _tiempoReal;

    public NotificacionService(INewRichDbContext db, IClock clock, INotificacionTiempoReal tiempoReal)
    {
        _db = db;
        _clock = clock;
        _tiempoReal = tiempoReal;
    }

    public async Task<Result<NotificacionesResponse>> ListarAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var items = await _db.Notificaciones
            .Where(n => n.UsuarioId == usuarioId)
            .OrderByDescending(n => n.FechaCreacion)
            .ToListAsync(cancellationToken);

        return Result<NotificacionesResponse>.Ok(new NotificacionesResponse
        {
            Pendientes = items.Count(n => !n.Leida),
            Items = items.Select(Mapear).ToList()
        }, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<NotificacionItemResponse>> ObtenerAsync(Guid notificacionId, Guid usuarioId, CancellationToken cancellationToken)
    {
        var item = await _db.Notificaciones
            .FirstOrDefaultAsync(n => n.NotificacionId == notificacionId && n.UsuarioId == usuarioId, cancellationToken);
        if (item is null)
        {
            return Result<NotificacionItemResponse>.Fail(NotificacionMessages.NotificacionNoEncontrada, 404);
        }

        if (!item.Leida)
        {
            item.Leida = true;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result<NotificacionItemResponse>.Ok(Mapear(item), SuccessMessages.OperacionExitosa);
    }

    public async Task CrearParaAsync(IReadOnlyCollection<Guid> usuarioIds, string tipo, string mensaje, CancellationToken cancellationToken)
    {
        if (usuarioIds.Count == 0)
        {
            return;
        }

        var ahora = _clock.UtcNow;
        var creadas = new List<Notificacion>();
        foreach (var usuarioId in usuarioIds.Distinct())
        {
            var item = new Notificacion
            {
                NotificacionId = Guid.NewGuid(),
                UsuarioId = usuarioId,
                Tipo = tipo,
                Mensaje = mensaje,
                FechaCreacion = ahora
            };
            _db.Notificaciones.Add(item);
            creadas.Add(item);
        }

        await _db.SaveChangesAsync(cancellationToken);
        foreach (var item in creadas)
        {
            await _tiempoReal.AvisarAsync(item.UsuarioId, Mapear(item), cancellationToken);
        }
    }

    public async Task<Result> MarcarLeidasAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var pendientes = await _db.Notificaciones.Where(n => n.UsuarioId == usuarioId && !n.Leida).ToListAsync(cancellationToken);
        foreach (var item in pendientes)
        {
            item.Leida = true;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.NotificacionesMarcadasLeidas);
    }

    private static NotificacionItemResponse Mapear(Notificacion n) => new()
    {
        NotificacionId = n.NotificacionId,
        Tipo = n.Tipo,
        Mensaje = n.Mensaje,
        Leida = n.Leida,
        FechaCreacion = n.FechaCreacion
    };
}
