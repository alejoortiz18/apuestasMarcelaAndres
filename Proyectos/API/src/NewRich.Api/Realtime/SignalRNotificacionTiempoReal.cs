using Microsoft.AspNetCore.SignalR;
using NewRich.Api.Hubs;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Notificaciones;

namespace NewRich.Api.Realtime;

public sealed class SignalRNotificacionTiempoReal : INotificacionTiempoReal
{
    private readonly IHubContext<NotificacionesHub> _hub;

    public SignalRNotificacionTiempoReal(IHubContext<NotificacionesHub> hub)
    {
        _hub = hub;
    }

    public Task AvisarAsync(Guid usuarioId, NotificacionItemResponse aviso, CancellationToken cancellationToken) =>
        _hub.Clients.Group(NotificacionesHub.Grupo(usuarioId))
            .SendAsync(NotificacionesHub.EventoNueva, aviso, cancellationToken);
}
