using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NewRich.Application.Abstractions;
using NewRich.Shared;

namespace NewRich.Api.Hubs;

[Authorize]
public sealed class ChatHub : Hub
{
    public const string Ruta = HubRutas.Chat;
    public const string EventoMensaje = HubRutas.EventoMensajeChat;
    public const string EventoCodigosOfflineAsignados = HubRutas.EventoCodigosOfflineAsignados;
    public const string EventoLoteriasActualizadas = HubRutas.EventoLoteriasActualizadas;

    private readonly IPresenciaDispositivos _presencia;

    public ChatHub(IPresenciaDispositivos presencia)
    {
        _presencia = presencia;
    }

    public static string Grupo(Guid usuarioId) => $"usuario:{usuarioId:D}";

    public override async Task OnConnectedAsync()
    {
        var claim = Context.User;
        if (ClaimsUsuario.TryId(claim, out var usuarioId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, Grupo(usuarioId));
        }

        if (TryDispositivoId(claim, out var dispositivoId))
        {
            _presencia.ConexionIniciada(dispositivoId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _presencia.ConexionTerminada(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    private static bool TryDispositivoId(System.Security.Claims.ClaimsPrincipal? user, out Guid dispositivoId)
    {
        dispositivoId = default;
        var valor = user?.FindFirst("dispositivoId")?.Value;
        return Guid.TryParse(valor, out dispositivoId);
    }
}
