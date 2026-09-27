using Microsoft.AspNetCore.SignalR;
using NewRich.Api.Hubs;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Versiones;
using NewRich.Shared;

namespace NewRich.Api.Realtime;

public sealed class SignalRVersionesTiempoReal : IVersionesTiempoReal
{
    private readonly IHubContext<ChatHub> _hub;

    public SignalRVersionesTiempoReal(IHubContext<ChatHub> hub)
    {
        _hub = hub;
    }

    public Task AvisarVersionPublicadaAsync(VersionAplicacionResponse version, CancellationToken cancellationToken) =>
        _hub.Clients.All.SendAsync(HubRutas.EventoActualizacionAplicacion, version, cancellationToken);
}
