using NewRich.Application.Contracts.Chat;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;

namespace NewRich.Maui.Views.Shared;

public sealed class SoportePage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly SesionPda _sesion;
    private readonly VerticalStackLayout _mensajes = new() { Spacing = 8 };
    private readonly Entry _texto = Ui.Entrada(PdaTexts.NuevoMensaje);
    private Guid? _conversacionId;

    public SoportePage(NewRichApiClient api, SesionPda sesion)
    {
        _api = api;
        _sesion = sesion;
        Title = PdaTexts.Soporte;
        BackgroundColor = Ui.Paper;

        var enviar = Ui.Primario(PdaTexts.Enviar);
        enviar.Clicked += async (_, _) => await EnviarAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 12,
                Children =
                {
                    _mensajes,
                    new HorizontalStackLayout
                    {
                        Spacing = 8,
                        Children = { _texto, enviar }
                    }
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarAsync();
    }

    private async Task CargarAsync()
    {
        _mensajes.Children.Clear();
        var lista = await _api.ChatsAsync(CancellationToken.None);
        if (!lista.IsSuccess || lista.Data is null || lista.Data.Count == 0)
        {
            _mensajes.Children.Add(new Label { Text = lista.IsSuccess ? PdaTexts.SinConversaciones : lista.Message, TextColor = Ui.Muted });
            return;
        }

        _conversacionId = lista.Data[0].ConversacionId;
        var detalle = await _api.ChatAsync(_conversacionId.Value, CancellationToken.None);
        if (!detalle.IsSuccess || detalle.Data is null)
        {
            _mensajes.Children.Add(new Label { Text = detalle.Message, TextColor = Ui.Danger });
            return;
        }

        foreach (var mensaje in detalle.Data.Mensajes)
        {
            var mio = mensaje.UsuarioEmisorId == _sesion.Usuario?.UsuarioId;
            _mensajes.Children.Add(new Frame
            {
                BackgroundColor = mio ? Ui.Dark : Colors.White,
                BorderColor = mio ? Ui.Dark : Ui.Line,
                CornerRadius = 12,
                Padding = 10,
                HorizontalOptions = mio ? LayoutOptions.End : LayoutOptions.Start,
                Content = new Label
                {
                    Text = $"{mensaje.Texto}\n{mensaje.FechaEnvio:HH:mm}",
                    TextColor = mio ? Colors.White : Ui.Ink,
                    FontSize = 12.5
                }
            });
        }
    }

    private async Task EnviarAsync()
    {
        var texto = _texto.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(texto))
        {
            return;
        }

        if (_conversacionId is null)
        {
            var inicio = await _api.IniciarChatAsync(new IniciarChatRequest { Texto = texto }, CancellationToken.None);
            if (!inicio.IsSuccess)
            {
                await DisplayAlert(PdaTexts.Soporte, inicio.Message, PdaTexts.Cerrar);
                return;
            }

            _texto.Text = string.Empty;
            await CargarAsync();
            return;
        }

        var enviado = await _api.EnviarMensajeAsync(_conversacionId.Value, new EnviarMensajeRequest { Texto = texto }, CancellationToken.None);
        if (!enviado.IsSuccess)
        {
            await DisplayAlert(PdaTexts.Soporte, enviado.Message, PdaTexts.Cerrar);
            return;
        }

        _texto.Text = string.Empty;
        await CargarAsync();
    }
}
