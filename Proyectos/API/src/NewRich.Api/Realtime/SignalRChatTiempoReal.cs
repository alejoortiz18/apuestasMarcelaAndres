using Microsoft.AspNetCore.SignalR;
using NewRich.Api.Hubs;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Chat;

namespace NewRich.Api.Realtime;

public sealed class SignalRChatTiempoReal : IChatTiempoReal
{
    private readonly IHubContext<ChatHub> _hub;

    public SignalRChatTiempoReal(IHubContext<ChatHub> hub)
    {
        _hub = hub;
    }

    public async Task AvisarMensajeAsync(
        IReadOnlyCollection<Guid> usuarioIds,
        MensajeChatEnVivoResponse aviso,
        CancellationToken cancellationToken)
    {
        foreach (var usuarioId in usuarioIds.Distinct())
        {
            await _hub.Clients.Group(ChatHub.Grupo(usuarioId))
                .SendAsync(ChatHub.EventoMensaje, aviso, cancellationToken);
        }
    }
}
