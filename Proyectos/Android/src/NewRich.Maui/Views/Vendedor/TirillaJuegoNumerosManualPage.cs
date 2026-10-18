using NewRich.Application.Services;
using NewRich.Maui.Services;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Ventas;

namespace NewRich.Maui.Views.Vendedor;

/// <summary>Jugada registrada: muestra la tirilla, la imprime una vez al abrir y permite reimprimir.</summary>
public sealed class TirillaJuegoNumerosManualPage : ContentPage
{
    private readonly JuegoNumerosManualServicio _juego;
    private readonly IPrinterService _printer;
    private readonly Label _avisoImpresion = new() { FontSize = 13, IsVisible = false };
    private bool _impresionAutomaticaHecha;
    private bool _imprimiendo;

    public TirillaJuegoNumerosManualPage(JuegoNumerosManualServicio juego, IPrinterService printer)
    {
        _juego = juego;
        _printer = printer;
        Title = JuegoNumerosManualTexts.Registrada;
        BackgroundColor = Ui.Paper;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BarraMenuVendedor.Asegurar(this, "vender");
        var recibo = _juego.Recibo;
        if (recibo is null)
        {
            Content = new Label { Text = PdaTexts.SinVentas, Padding = 16, TextColor = Ui.Ink };
            return;
        }

        var imprimir = Ui.Secundario(JuegoNumerosManualTexts.Imprimir);
        imprimir.Clicked += async (_, _) => await ImprimirAsync(recibo, imprimir);
        var finalizar = Ui.Primario(JuegoNumerosManualTexts.Finalizar);
        finalizar.Clicked += async (_, _) => await FinalizarAsync();

        var cuerpo = new VerticalStackLayout
        {
            Padding = new Thickness(16, 16, 16, 24),
            Spacing = 12,
            Children =
            {
                new Label { Text = JuegoNumerosManualTexts.Registrada, FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink }
            }
        };
        cuerpo.Children.Add(Recibo(recibo));
        cuerpo.Children.Add(_avisoImpresion);
        cuerpo.Children.Add(finalizar);
        cuerpo.Children.Add(imprimir);
        Content = new ScrollView { Content = cuerpo };

        if (!_impresionAutomaticaHecha)
        {
            _impresionAutomaticaHecha = true;
            _ = ImprimirAsync(recibo, imprimir);
        }
    }

    /// <summary>Misma tirilla que se imprime, con el QR del boleto en el lugar de la marca.</summary>
    private static View Recibo(ReciboJuegoNumerosManual recibo)
    {
        var partes = recibo.Texto.Split(TirillaTexto.MarcaQr);
        var contenido = new VerticalStackLayout { Spacing = 4 };
        contenido.Children.Add(Mono(partes[0].TrimEnd()));
        byte[] png = [];
        try
        {
            png = QrImagen.PngParaTirilla(recibo.Qr, 384);
        }
        catch (Exception)
        {
        }

        if (png.Length > 0)
        {
            contenido.Children.Add(new Image
            {
                Source = ImageSource.FromStream(() => new MemoryStream(png)),
                HeightRequest = 128,
                WidthRequest = 128,
                HorizontalOptions = LayoutOptions.Center
            });
        }

        if (partes.Length > 1)
        {
            contenido.Children.Add(Mono(partes[1].Trim('\r', '\n')));
        }

        return new Border
        {
            Stroke = Ui.Line,
            StrokeThickness = 1,
            StrokeDashArray = [4, 3],
            Padding = 14,
            BackgroundColor = Color.FromArgb("#fbfdfc"),
            HorizontalOptions = LayoutOptions.Center,
            Content = contenido
        };
    }

    private static Label Mono(string texto) => new()
    {
        Text = texto,
        FontFamily = "monospace",
        FontSize = 12,
        TextColor = Ui.Ink,
        LineBreakMode = LineBreakMode.WordWrap
    };

    private async Task ImprimirAsync(ReciboJuegoNumerosManual recibo, Button boton)
    {
        if (_imprimiendo)
        {
            return;
        }

        _imprimiendo = true;
        boton.IsEnabled = false;
        MostrarAviso(EstadoImpresora.Aviso(null), false);
        try
        {
            var resultado = await _printer.ImprimirAsync(recibo.Texto, recibo.Qr);
            MostrarAviso(resultado.Ok ? EstadoImpresora.Aviso(true) : resultado.Mensaje, !resultado.Ok);
        }
        catch (Exception)
        {
            MostrarAviso(EstadoImpresora.Aviso(false), true);
        }
        finally
        {
            _imprimiendo = false;
            MainThread.BeginInvokeOnMainThread(() => boton.IsEnabled = true);
        }
    }

    private void MostrarAviso(string mensaje, bool esError) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _avisoImpresion.Text = mensaje;
            _avisoImpresion.TextColor = esError ? Ui.Danger : Ui.Muted;
            _avisoImpresion.IsVisible = !string.IsNullOrWhiteSpace(mensaje);
        });

    /// <summary>La jugada ya quedó registrada: atrás no vuelve al resumen para no registrarla dos veces.</summary>
    protected override bool OnBackButtonPressed()
    {
        _ = FinalizarAsync();
        return true;
    }

    private async Task FinalizarAsync()
    {
        _juego.Recibo = null;
        _juego.Borrador = null;
        try
        {
            await Shell.Current.GoToAsync("//inicio");
        }
        catch (Exception)
        {
        }
    }
}
