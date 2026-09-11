using Microsoft.AspNetCore.SignalR;
using NewRich.Api.Hubs;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Offline;
using NewRich.Shared;

namespace NewRich.Api.Realtime;

public sealed class SignalRCodigosOfflineTiempoReal : ICodigosOfflineTiempoReal
{
    private readonly IHubContext<ChatHub> _hub;

    public SignalRCodigosOfflineTiempoReal(IHubContext<ChatHub> hub)
    {
        _hub = hub;
    }

    public Task AvisarAsignadosAsync(
        Guid usuarioId,
        CodigosOfflineAsignadosAviso aviso,
        CancellationToken cancellationToken) =>
        _hub.Clients.Group(ChatHub.Grupo(usuarioId))
            .SendAsync(HubRutas.EventoCodigosOfflineAsignados, aviso, cancellationToken);
}
