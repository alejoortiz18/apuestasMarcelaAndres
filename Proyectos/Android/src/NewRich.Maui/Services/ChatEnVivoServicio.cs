using Microsoft.AspNetCore.SignalR.Client;
using NewRich.Application.Contracts.Chat;
using NewRich.Application.Contracts.Offline;
using NewRich.Pda.Core.Api;
using NewRich.Shared;

namespace NewRich.Maui.Services;

public sealed class ChatEnVivoServicio : IAsyncDisposable
{
    private HubConnection? _hub;
    private string? _token;

    public event Action<MensajeChatEnVivoResponse>? Recibido;
    public event Action<CodigosOfflineAsignadosAviso>? CodigosAsignados;

    public async Task AsegurarConectadoAsync(string baseUrl, string token, CancellationToken cancellationToken)
    {
        if (_hub is not null
            && _token == token
            && (_hub.State == HubConnectionState.Connected
                || _hub.State == HubConnectionState.Connecting
                || _hub.State == HubConnectionState.Reconnecting))
        {
            return;
        }

        await ConectarAsync(baseUrl, token, cancellationToken);
    }

    public async Task ConectarAsync(string baseUrl, string token, CancellationToken cancellationToken)
    {
        await DesconectarAsync();
        _token = token;
        _hub = new HubConnectionBuilder()
            .WithUrl(PdaConexion.HubChat(baseUrl), opciones =>
            {
                opciones.AccessTokenProvider = () => Task.FromResult<string?>(_token);
            })
            .WithAutomaticReconnect()
            .Build();
        _hub.On<MensajeChatEnVivoResponse>(HubRutas.EventoMensajeChat, aviso =>
        {
            MainThread.BeginInvokeOnMainThread(() => Recibido?.Invoke(aviso));
        });
        _hub.On<CodigosOfflineAsignadosAviso>(HubRutas.EventoCodigosOfflineAsignados, aviso =>
        {
            MainThread.BeginInvokeOnMainThread(() => CodigosAsignados?.Invoke(aviso));
        });
        await _hub.StartAsync(cancellationToken);
    }

    public async Task DesconectarAsync()
    {
        _token = null;
        if (_hub is null)
        {
            return;
        }

        await _hub.DisposeAsync();
        _hub = null;
    }

    public async ValueTask DisposeAsync() => await DesconectarAsync();
}
