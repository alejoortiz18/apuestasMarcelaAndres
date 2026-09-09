using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Notificaciones;

namespace NewRich.Application.Services;

public sealed class NotificacionTiempoRealNulo : INotificacionTiempoReal
{
    public Task AvisarAsync(Guid usuarioId, NotificacionItemResponse aviso, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
