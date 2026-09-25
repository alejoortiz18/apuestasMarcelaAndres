using Microsoft.AspNetCore.SignalR;
using NewRich.Api.Hubs;
using NewRich.Application.Abstractions;
using NewRich.Shared;

namespace NewRich.Api.Realtime;

public sealed class SignalRLoteriasTiempoReal : ILoteriasTiempoReal
{
    private readonly IHubContext<ChatHub> _hub;

    public SignalRLoteriasTiempoReal(IHubContext<ChatHub> hub)
    {
        _hub = hub;
    }

    public Task AvisarCatalogoActualizadoAsync(CancellationToken cancellationToken) =>
        _hub.Clients.All.SendAsync(HubRutas.EventoLoteriasActualizadas, cancellationToken);
}
