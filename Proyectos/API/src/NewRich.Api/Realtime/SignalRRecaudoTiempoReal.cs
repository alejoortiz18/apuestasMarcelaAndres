using Microsoft.AspNetCore.SignalR;
using NewRich.Api.Hubs;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Shared;

namespace NewRich.Api.Realtime;

public sealed class SignalRRecaudoTiempoReal : IRecaudoTiempoReal
{
    private readonly IHubContext<ChatHub> _hub;

    public SignalRRecaudoTiempoReal(IHubContext<ChatHub> hub)
    {
        _hub = hub;
    }

    public async Task AvisarCambioAsync(RecaudoCambioAviso aviso, CancellationToken cancellationToken)
    {
        await _hub.Clients.Group(ChatHub.Grupo(aviso.VendedorId))
            .SendAsync(HubRutas.EventoRecaudoActualizado, aviso, cancellationToken);
        if (aviso.RecaudadorId != aviso.VendedorId)
        {
            await _hub.Clients.Group(ChatHub.Grupo(aviso.RecaudadorId))
                .SendAsync(HubRutas.EventoRecaudoActualizado, aviso, cancellationToken);
        }
    }
}
