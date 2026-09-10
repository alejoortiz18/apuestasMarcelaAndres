using System.Collections.ObjectModel;
using Microsoft.Maui.Controls.Shapes;
using NewRich.Application.Contracts.Chat;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;
using NewRich.Maui.Services;
using NewRich.Maui.Views;

namespace NewRich.Maui.Views.Shared;

public sealed class SoportePage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly SesionPda _sesion;
    private readonly ApiOpciones _opciones;
    private readonly ITokenStore _tokens;
    private readonly ChatEnVivoServicio _vivo;
    private readonly ObservableCollection<BurbujaChat> _burbujas = [];
    private readonly Entry _texto;
    private readonly CollectionView _lista;
    private Guid? _conversacionId;
    private readonly HashSet<Guid> _vistos = [];

    public SoportePage(
        NewRichApiClient api,
        SesionPda sesion,
        ApiOpciones opciones,
        ITokenStore tokens,
        ChatEnVivoServicio vivo)
    {
        _api = api;
        _sesion = sesion;
        _opciones = opciones;
        _tokens = tokens;
        _vivo = vivo;
        Title = PdaTexts.Soporte;
        BackgroundColor = Color.FromArgb("#DCE8E2");

        _texto = new Entry
        {
            Placeholder = PdaTexts.NuevoMensaje,
            BackgroundColor = Colors.White,
            TextColor = Ui.Ink,
            FontSize = 15,
            HeightRequest = 44,
            Margin = new Thickness(0),
            ReturnType = ReturnType.Send
        };
        _texto.Completed += async (_, _) => await EnviarAsync();

        var enviar = new Button
        {
            Text = PdaTexts.Enviar,
            BackgroundColor = Ui.Dark,
            TextColor = Colors.White,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 22,
            WidthRequest = 88,
            HeightRequest = 44,
            Padding = 0
        };
        SemanticProperties.SetDescription(enviar, PdaTexts.EnviarMensajeAria);
        enviar.Clicked += async (_, _) => await EnviarAsync();

        _lista = new CollectionView
        {
            ItemsSource = _burbujas,
            RemainingItemsThreshold = 1,
            ItemTemplate = new DataTemplate(() =>
            {
                var texto = new Label
                {
                    FontSize = 15,
                    LineBreakMode = LineBreakMode.WordWrap
                };
                texto.SetBinding(Label.TextProperty, nameof(BurbujaChat.Texto));
                texto.SetBinding(Label.TextColorProperty, nameof(BurbujaChat.ColorTexto));

                var hora = new Label
                {
                    FontSize = 10,
                    HorizontalTextAlignment = TextAlignment.End
                };
                hora.SetBinding(Label.TextProperty, nameof(BurbujaChat.Hora));
                hora.SetBinding(Label.TextColorProperty, nameof(BurbujaChat.ColorHora));

                var pila = new VerticalStackLayout
                {
                    Spacing = 4,
                    Children = { texto, hora }
                };

                var burbuja = new Border
                {
                    StrokeThickness = 0,
                    Padding = new Thickness(12, 8),
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18, 18, 6, 18) },
                    Content = pila,
                    MaximumWidthRequest = 280
                };
                burbuja.SetBinding(Border.BackgroundColorProperty, nameof(BurbujaChat.Fondo));
                burbuja.SetBinding(Border.HorizontalOptionsProperty, nameof(BurbujaChat.Alineacion));
                burbuja.SetBinding(Border.StrokeShapeProperty, nameof(BurbujaChat.Forma));
                var fila = new Grid
                {
                    Padding = new Thickness(12, 4),
                    Children = { burbuja }
                };
                return fila;
            })
        };

        var compositor = new Grid
        {
            BackgroundColor = Color.FromArgb("#F4F8F6"),
            Padding = new Thickness(12, 10, 12, 12),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8
        };
        var campo = new Border
        {
            Stroke = Color.FromArgb("#C5D4CC"),
            StrokeThickness = 1,
            BackgroundColor = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            Padding = new Thickness(14, 0),
            Content = _texto
        };
        compositor.Add(campo);
        compositor.Add(enviar);
        Grid.SetColumn(enviar, 1);

        var raiz = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };
        raiz.Add(_lista);
        raiz.Add(compositor);
        Grid.SetRow(compositor, 1);
        Content = raiz;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _vivo.Recibido -= EnVivo;
        _vivo.Recibido += EnVivo;
        await CargarAsync();
        await ConectarVivoAsync();
    }

    protected override async void OnDisappearing()
    {
        _vivo.Recibido -= EnVivo;
        await _vivo.DesconectarAsync();
        base.OnDisappearing();
    }

    private async Task ConectarVivoAsync()
    {
        var token = await _tokens.ObtenerAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        try
        {
            await _vivo.ConectarAsync(_opciones.BaseUrl, token, CancellationToken.None);
        }
        catch (Exception)
        {
        }
    }

    private void EnVivo(MensajeChatEnVivoResponse aviso)
    {
        if (_conversacionId is Guid id && aviso.ConversacionId != id && aviso.ConversacionId != Guid.Empty)
        {
            return;
        }

        Agregar(aviso.Mensaje, aviso.Mensaje.UsuarioEmisorId == _sesion.Usuario?.UsuarioId);
        if (_conversacionId is null)
        {
            _conversacionId = aviso.ConversacionId;
        }
    }

    private async Task CargarAsync()
    {
        _burbujas.Clear();
        _vistos.Clear();
        var lista = await _api.ChatsAsync(CancellationToken.None);
        if (!lista.IsSuccess || lista.Data is null || lista.Data.Count == 0)
        {
            return;
        }

        _conversacionId = lista.Data[0].ConversacionId;
        var detalle = await _api.ChatAsync(_conversacionId.Value, CancellationToken.None);
        if (!detalle.IsSuccess || detalle.Data is null)
        {
            return;
        }

        foreach (var mensaje in detalle.Data.Mensajes)
        {
            Agregar(mensaje, mensaje.UsuarioEmisorId == _sesion.Usuario?.UsuarioId);
        }

        DesplazarAlFinal();
    }

    private async Task EnviarAsync()
    {
        var texto = _texto.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(texto))
        {
            return;
        }

        _texto.Text = string.Empty;
        if (_conversacionId is null)
        {
            var inicio = await _api.IniciarChatAsync(new IniciarChatRequest { Texto = texto }, CancellationToken.None);
            if (!inicio.IsSuccess)
            {
                await DisplayAlertAsync(PdaTexts.Soporte, inicio.Message, PdaTexts.Cerrar);
                _texto.Text = texto;
                return;
            }

            _conversacionId = inicio.Data?.ConversacionId;
            await CargarAsync();
            return;
        }

        var enviado = await _api.EnviarMensajeAsync(_conversacionId.Value, new EnviarMensajeRequest { Texto = texto }, CancellationToken.None);
        if (!enviado.IsSuccess || enviado.Data is null)
        {
            await DisplayAlertAsync(PdaTexts.Soporte, enviado.Message, PdaTexts.Cerrar);
            _texto.Text = texto;
            return;
        }

        Agregar(enviado.Data, true);
        DesplazarAlFinal();
    }

    private void Agregar(MensajeResponse mensaje, bool mio)
    {
        if (!_vistos.Add(mensaje.MensajeId) && mensaje.MensajeId != Guid.Empty)
        {
            return;
        }

        _burbujas.Add(BurbujaChat.De(mensaje, mio));
        DesplazarAlFinal();
    }

    private void DesplazarAlFinal()
    {
        if (_burbujas.Count == 0)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _lista.ScrollTo(_burbujas.Count - 1, position: ScrollToPosition.End, animate: false);
        });
    }

    public sealed class BurbujaChat
    {
        public string Texto { get; init; } = string.Empty;
        public string Hora { get; init; } = string.Empty;
        public Color Fondo { get; init; } = Colors.White;
        public Color ColorTexto { get; init; } = Ui.Ink;
        public Color ColorHora { get; init; } = Ui.Muted;
        public LayoutOptions Alineacion { get; init; } = LayoutOptions.Start;
        public RoundRectangle Forma { get; init; } = new();

        public static BurbujaChat De(MensajeResponse mensaje, bool mio) => new()
        {
            Texto = mensaje.Texto,
            Hora = mensaje.FechaEnvio.ToLocalTime().ToString("HH:mm"),
            Fondo = mio ? Ui.Dark : Colors.White,
            ColorTexto = mio ? Colors.White : Ui.Ink,
            ColorHora = mio ? Color.FromArgb("#C5D4CC") : Ui.Muted,
            Alineacion = mio ? LayoutOptions.End : LayoutOptions.Start,
            Forma = new RoundRectangle
            {
                CornerRadius = mio ? new CornerRadius(18, 18, 18, 6) : new CornerRadius(18, 18, 6, 18)
            }
        };
    }
}
