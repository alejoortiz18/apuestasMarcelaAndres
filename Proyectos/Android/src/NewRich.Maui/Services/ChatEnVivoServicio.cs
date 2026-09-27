using Microsoft.AspNetCore.SignalR.Client;
using NewRich.Application.Contracts.Chat;
using NewRich.Application.Contracts.Offline;
using NewRich.Application.Contracts.Versiones;
using NewRich.Pda.Core.Api;
using NewRich.Shared;

namespace NewRich.Maui.Services;

public sealed class ChatEnVivoServicio : IAsyncDisposable
{
    private HubConnection? _hub;
    private string? _token;

    public event Action<MensajeChatEnVivoResponse>? Recibido;
    public event Action<CodigosOfflineAsignadosAviso>? CodigosAsignados;
    public event Action? LoteriasActualizadas;
    public event Action? ConexionLista;
    public event Action<VersionAplicacionResponse>? ActualizacionRecibida;

    public async Task AsegurarConectadoAsync(string baseUrl, string token, CancellationToken cancellationToken)
    {
        if (_hub is not null && _token == token && _hub.State == HubConnectionState.Connected)
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
            .WithServerTimeout(TimeSpan.FromMinutes(2))
            .WithKeepAliveInterval(TimeSpan.FromSeconds(15))
            .Build();
        _hub.On<MensajeChatEnVivoResponse>(HubRutas.EventoMensajeChat, aviso =>
        {
            MainThread.BeginInvokeOnMainThread(() => Recibido?.Invoke(aviso));
        });
        _hub.On<CodigosOfflineAsignadosAviso>(HubRutas.EventoCodigosOfflineAsignados, aviso =>
        {
            MainThread.BeginInvokeOnMainThread(() => CodigosAsignados?.Invoke(aviso));
        });
        _hub.On(HubRutas.EventoLoteriasActualizadas, () =>
        {
            MainThread.BeginInvokeOnMainThread(() => LoteriasActualizadas?.Invoke());
        });
        _hub.On<VersionAplicacionResponse>(HubRutas.EventoActualizacionAplicacion, aviso =>
        {
            MainThread.BeginInvokeOnMainThread(() => ActualizacionRecibida?.Invoke(aviso));
        });
        _hub.Reconnected += _ =>
        {
            MainThread.BeginInvokeOnMainThread(() => ConexionLista?.Invoke());
            return Task.CompletedTask;
        };
        try
        {
            await _hub.StartAsync(cancellationToken);
            MainThread.BeginInvokeOnMainThread(() => ConexionLista?.Invoke());
        }
        catch (Exception)
        {
            await DesconectarAsync();
        }
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
