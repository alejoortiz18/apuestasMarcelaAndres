using System.Collections.ObjectModel;
using Microsoft.Maui.Controls.Shapes;
using NewRich.Application.Chat;
using NewRich.Application.Contracts.Chat;
using NewRich.Maui.Services;
using NewRich.Maui.Views;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;

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
    private readonly Label _pendiente;
    private readonly CollectionView _lista;
    private Guid? _conversacionId;
    private readonly HashSet<Guid> _vistos = [];
    private string? _archivoNombre;
    private byte[]? _archivoBytes;

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

        _pendiente = new Label
        {
            FontSize = 12,
            TextColor = Ui.Muted,
            IsVisible = false,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        var adjuntar = new Button
        {
            Text = "+",
            BackgroundColor = Ui.Dark,
            TextColor = Colors.White,
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 22,
            WidthRequest = 44,
            HeightRequest = 44,
            Padding = 0
        };
        SemanticProperties.SetDescription(adjuntar, PdaTexts.AdjuntarAyuda);
        adjuntar.Clicked += async (_, _) => await ElegirArchivoAsync();

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
                texto.SetBinding(VisualElement.IsVisibleProperty, nameof(BurbujaChat.TieneTexto));

                var imagen = new Image
                {
                    HeightRequest = 160,
                    Aspect = Aspect.AspectFit
                };
                imagen.SetBinding(Image.SourceProperty, nameof(BurbujaChat.Imagen));
                imagen.SetBinding(VisualElement.IsVisibleProperty, nameof(BurbujaChat.EsImagen));
                var abrirImagen = new TapGestureRecognizer();
                abrirImagen.Tapped += async (_, _) =>
                {
                    if (imagen.BindingContext is BurbujaChat burbuja && burbuja.AdjuntoId is Guid id)
                    {
                        await AbrirAdjuntoAsync(id, burbuja.NombreArchivo);
                    }
                };
                imagen.GestureRecognizers.Add(abrirImagen);

                var abrir = new Button
                {
                    Text = PdaTexts.AbrirAdjunto,
                    FontSize = 12,
                    HeightRequest = 32,
                    Padding = new Thickness(10, 0)
                };
                abrir.SetBinding(VisualElement.IsVisibleProperty, nameof(BurbujaChat.TieneAdjunto));
                abrir.SetBinding(Button.BackgroundColorProperty, nameof(BurbujaChat.FondoBoton));
                abrir.SetBinding(Button.TextColorProperty, nameof(BurbujaChat.ColorBoton));
                abrir.Clicked += async (_, _) =>
                {
                    if (abrir.BindingContext is BurbujaChat burbuja && burbuja.AdjuntoId is Guid id)
                    {
                        await AbrirAdjuntoAsync(id, burbuja.NombreArchivo);
                    }
                };

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
                    Children = { texto, imagen, abrir, hora }
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
                return new Grid
                {
                    Padding = new Thickness(12, 4),
                    Children = { burbuja }
                };
            })
        };

        var compositor = new Grid
        {
            BackgroundColor = Color.FromArgb("#F4F8F6"),
            Padding = new Thickness(12, 10, 12, 12),
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            },
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8,
            RowSpacing = 6
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
        compositor.Add(_pendiente);
        Grid.SetColumnSpan(_pendiente, 3);
        compositor.Add(adjuntar);
        Grid.SetRow(adjuntar, 1);
        compositor.Add(campo);
        Grid.SetRow(campo, 1);
        Grid.SetColumn(campo, 1);
        compositor.Add(enviar);
        Grid.SetRow(enviar, 1);
        Grid.SetColumn(enviar, 2);

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

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await AgregarAsync(aviso.Mensaje, aviso.Mensaje.UsuarioEmisorId == _sesion.Usuario?.UsuarioId);
            if (_conversacionId is null)
            {
                _conversacionId = aviso.ConversacionId;
            }
        });
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
            await AgregarAsync(mensaje, mensaje.UsuarioEmisorId == _sesion.Usuario?.UsuarioId);
        }

        DesplazarAlFinal();
    }

    private async Task ElegirArchivoAsync()
    {
        try
        {
            var elegido = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = PdaTexts.AdjuntarAyuda,
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, ["image/jpeg", "image/png", "image/webp", "application/pdf"] },
                    { DevicePlatform.WinUI, [".jpg", ".jpeg", ".png", ".webp", ".pdf"] },
                    { DevicePlatform.iOS, ["public.image", "com.adobe.pdf"] }
                })
            });
            if (elegido is null)
            {
                return;
            }

            await using var stream = await elegido.OpenReadAsync();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            var bytes = buffer.ToArray();
            var validacion = ChatAdjunto.Validar(elegido.FileName, bytes);
            if (!validacion.IsSuccess)
            {
                await DisplayAlertAsync(PdaTexts.Soporte, validacion.Message, PdaTexts.Cerrar);
                return;
            }

            _archivoNombre = ChatAdjunto.NombreSeguro(elegido.FileName);
            _archivoBytes = bytes;
            _pendiente.Text = string.Format(PdaTexts.AdjuntoPendiente, _archivoNombre);
            _pendiente.IsVisible = true;
        }
        catch (Exception excepcion)
        {
            await DisplayAlertAsync(PdaTexts.Soporte, excepcion.Message, PdaTexts.Cerrar);
        }
    }

    private async Task EnviarAsync()
    {
        var texto = _texto.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(texto) && _archivoBytes is null)
        {
            return;
        }

        var nombre = _archivoNombre;
        var bytes = _archivoBytes;
        var base64 = bytes is null ? null : Convert.ToBase64String(bytes);
        _texto.Text = string.Empty;
        LimpiarPendiente();

        if (_conversacionId is null)
        {
            var inicio = await _api.IniciarChatAsync(new IniciarChatRequest
            {
                Texto = texto,
                NombreArchivo = nombre,
                ContenidoBase64 = base64
            }, CancellationToken.None);
            if (!inicio.IsSuccess)
            {
                await DisplayAlertAsync(PdaTexts.Soporte, inicio.Message, PdaTexts.Cerrar);
                RestaurarBorrador(texto, nombre, bytes);
                return;
            }

            _conversacionId = inicio.Data?.ConversacionId;
            await CargarAsync();
            return;
        }

        var enviado = await _api.EnviarMensajeAsync(_conversacionId.Value, new EnviarMensajeRequest
        {
            Texto = texto,
            NombreArchivo = nombre,
            ContenidoBase64 = base64
        }, CancellationToken.None);
        if (!enviado.IsSuccess || enviado.Data is null)
        {
            await DisplayAlertAsync(PdaTexts.Soporte, enviado.Message, PdaTexts.Cerrar);
            RestaurarBorrador(texto, nombre, bytes);
            return;
        }

        await AgregarAsync(enviado.Data, true);
        DesplazarAlFinal();
    }

    private void RestaurarBorrador(string texto, string? nombre, byte[]? bytes)
    {
        _texto.Text = texto;
        _archivoNombre = nombre;
        _archivoBytes = bytes;
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            _pendiente.Text = string.Format(PdaTexts.AdjuntoPendiente, nombre);
            _pendiente.IsVisible = true;
        }
    }

    private void LimpiarPendiente()
    {
        _archivoNombre = null;
        _archivoBytes = null;
        _pendiente.IsVisible = false;
        _pendiente.Text = string.Empty;
    }

    private async Task AgregarAsync(MensajeResponse mensaje, bool mio)
    {
        if (!_vistos.Add(mensaje.MensajeId) && mensaje.MensajeId != Guid.Empty)
        {
            return;
        }

        var burbuja = BurbujaChat.De(mensaje, mio);
        if (mensaje.AdjuntoId is Guid adjuntoId && ChatAdjunto.EsImagen(mensaje.NombreArchivo))
        {
            var descarga = await _api.DescargarAdjuntoAsync(adjuntoId, CancellationToken.None);
            if (descarga.IsSuccess && descarga.Data is not null)
            {
                var copia = descarga.Data.Bytes;
                burbuja.Imagen = ImageSource.FromStream(() => new MemoryStream(copia));
                burbuja.EsImagen = true;
            }
        }

        _burbujas.Add(burbuja);
        DesplazarAlFinal();
    }

    private async Task AbrirAdjuntoAsync(Guid adjuntoId, string nombre)
    {
        var descarga = await _api.DescargarAdjuntoAsync(adjuntoId, CancellationToken.None);
        if (!descarga.IsSuccess || descarga.Data is null)
        {
            await DisplayAlertAsync(PdaTexts.Soporte, descarga.Message, PdaTexts.Cerrar);
            return;
        }

        var ruta = System.IO.Path.Combine(FileSystem.CacheDirectory, ChatAdjunto.NombreSeguro(nombre));
        await File.WriteAllBytesAsync(ruta, descarga.Data.Bytes);
        await Launcher.Default.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(ruta) });
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
        public bool TieneTexto { get; init; }
        public bool EsImagen { get; set; }
        public bool TieneAdjunto { get; init; }
        public Guid? AdjuntoId { get; init; }
        public string NombreArchivo { get; init; } = string.Empty;
        public ImageSource? Imagen { get; set; }
        public string Hora { get; init; } = string.Empty;
        public Color Fondo { get; init; } = Colors.White;
        public Color ColorTexto { get; init; } = Ui.Ink;
        public Color ColorHora { get; init; } = Ui.Muted;
        public Color FondoBoton { get; init; } = Color.FromArgb("#14000000");
        public Color ColorBoton { get; init; } = Ui.Ink;
        public LayoutOptions Alineacion { get; init; } = LayoutOptions.Start;
        public RoundRectangle Forma { get; init; } = new();

        public static BurbujaChat De(MensajeResponse mensaje, bool mio)
        {
            var nombre = mensaje.NombreArchivo ?? string.Empty;
            var texto = string.IsNullOrWhiteSpace(mensaje.Texto)
                ? (ChatAdjunto.EsPdf(nombre) ? nombre : string.Empty)
                : mensaje.Texto;
            return new BurbujaChat
            {
                Texto = texto,
                TieneTexto = !string.IsNullOrWhiteSpace(texto),
                EsImagen = false,
                TieneAdjunto = mensaje.AdjuntoId.HasValue,
                AdjuntoId = mensaje.AdjuntoId,
                NombreArchivo = nombre,
                Hora = mensaje.FechaEnvio.ToLocalTime().ToString("HH:mm"),
                Fondo = mio ? Ui.Dark : Colors.White,
                ColorTexto = mio ? Colors.White : Ui.Ink,
                ColorHora = mio ? Color.FromArgb("#C5D4CC") : Ui.Muted,
                FondoBoton = mio ? Color.FromArgb("#33FFFFFF") : Color.FromArgb("#14000000"),
                ColorBoton = mio ? Colors.White : Ui.Ink,
                Alineacion = mio ? LayoutOptions.End : LayoutOptions.Start,
                Forma = new RoundRectangle
                {
                    CornerRadius = mio ? new CornerRadius(18, 18, 18, 6) : new CornerRadius(18, 18, 6, 18)
                }
            };
        }
    }
}
