using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NewRich.Shared;

namespace NewRich.Api.Hubs;

[Authorize]
public sealed class ChatHub : Hub
{
    public const string Ruta = HubRutas.Chat;
    public const string EventoMensaje = HubRutas.EventoMensajeChat;

    public static string Grupo(Guid usuarioId) => $"usuario:{usuarioId:D}";

    public override async Task OnConnectedAsync()
    {
        var claim = Context.User;
        if (ClaimsUsuario.TryId(claim, out var usuarioId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, Grupo(usuarioId));
        }

        await base.OnConnectedAsync();
    }
}
