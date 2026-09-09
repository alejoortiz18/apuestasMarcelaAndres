using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Notificaciones;
using NewRich.Constants.Messages;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class NotificacionService : INotificacionService
{
    private readonly INewRichDbContext _db;

    public NotificacionService(INewRichDbContext db)
    {
        _db = db;
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
            Items = items.Select(n => new NotificacionItemResponse
            {
                NotificacionId = n.NotificacionId,
                Tipo = n.Tipo,
                Mensaje = n.Mensaje,
                Leida = n.Leida,
                FechaCreacion = n.FechaCreacion
            }).ToList()
        }, SuccessMessages.OperacionExitosa);
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
}
