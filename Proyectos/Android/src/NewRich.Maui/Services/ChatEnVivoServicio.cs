using Microsoft.AspNetCore.SignalR.Client;
using NewRich.Application.Contracts.Chat;
using NewRich.Pda.Core.Api;
using NewRich.Shared;

namespace NewRich.Maui.Services;

public sealed class ChatEnVivoServicio : IAsyncDisposable
{
    private HubConnection? _hub;

    public event Action<MensajeChatEnVivoResponse>? Recibido;

    public async Task ConectarAsync(string baseUrl, string token, CancellationToken cancellationToken)
    {
        await DesconectarAsync();
        _hub = new HubConnectionBuilder()
            .WithUrl(PdaConexion.HubChat(baseUrl), opciones =>
            {
                opciones.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .WithAutomaticReconnect()
            .Build();
        _hub.On<MensajeChatEnVivoResponse>(HubRutas.EventoMensajeChat, aviso =>
        {
            MainThread.BeginInvokeOnMainThread(() => Recibido?.Invoke(aviso));
        });
        await _hub.StartAsync(cancellationToken);
    }

    public async Task DesconectarAsync()
    {
        if (_hub is null)
        {
            return;
        }

        await _hub.DisposeAsync();
        _hub = null;
    }

    public async ValueTask DisposeAsync() => await DesconectarAsync();
}
