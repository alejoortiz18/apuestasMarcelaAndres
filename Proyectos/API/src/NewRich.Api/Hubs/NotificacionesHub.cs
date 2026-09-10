using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NewRich.Api.Hubs;

[Authorize]
public sealed class NotificacionesHub : Hub
{
    public const string Ruta = "/hubs/notificaciones";
    public const string EventoNueva = "nuevaNotificacion";

    public static string Grupo(Guid usuarioId) => $"usuario:{usuarioId:D}";

    public override async Task OnConnectedAsync()
    {
        var claim = Context.User;
        if (NewRich.Shared.ClaimsUsuario.TryId(claim, out var usuarioId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, Grupo(usuarioId));
        }

        await base.OnConnectedAsync();
    }
}
