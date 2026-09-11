using NewRich.Application.Services;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;
using NewRich.Maui.Services;
using NewRich.Maui.Views;

namespace NewRich.Maui.Views.Vendedor;

public sealed class TirillaVendidaPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly SesionPda _sesion;
    private readonly IPrinterService _printer;
    private readonly IPdfService _pdf;
    private bool _evidenciaEnviada;
    private bool _impresionAutomaticaHecha;
    private Label? _avisoImpresion;

    public TirillaVendidaPage(NewRichApiClient api, SesionPda sesion, IPrinterService printer, IPdfService pdf)
    {
        _api = api;
        _sesion = sesion;
        _printer = printer;
        _pdf = pdf;
        Title = PdaTexts.TicketVendido;
        BackgroundColor = Ui.Paper;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var tirilla = _sesion.Tirilla;
        if (tirilla is null)
        {
            Content = new Label { Text = PdaTexts.SinVentas, Padding = 16 };
            return;
        }

        _avisoImpresion = new Label
        {
            Text = string.Empty,
            TextColor = Ui.Danger,
            FontSize = 13,
            IsVisible = false
        };

        var cuerpo = new VerticalStackLayout { Spacing = 12 };
        cuerpo.Add(new Label
        {
            Text = PdaTexts.TicketVendido,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = Ui.Ink
        });
        cuerpo.Add(Recibo(tirilla));
        cuerpo.Add(_avisoImpresion);

        var cerrar = Ui.Secundario(PdaTexts.Cerrar);
        cerrar.Clicked += async (_, _) => await CerrarAsync();
        if (tirilla.Offline && !_evidenciaEnviada)
        {
            cerrar.IsEnabled = false;
        }

        var imprimir = Ui.Primario(PdaTexts.ImprimirTirilla);
        imprimir.Clicked += async (_, _) => await ImprimirAsync(tirilla);

        var pdf = Ui.Secundario(PdaTexts.GenerarPdf);
        pdf.Clicked += async (_, _) => await GenerarPdfAsync(tirilla);

        cuerpo.Add(cerrar);
        cuerpo.Add(imprimir);
        cuerpo.Add(pdf);

        if (tirilla.Offline)
        {
            cuerpo.Add(Ui.Banner(PdaTexts.FotoOffline, Ui.WarnBg, Ui.Warn));
            var listo = Ui.Primario(PdaTexts.FotoLista);
            listo.Clicked += async (_, _) => await EnviarEvidenciaAsync(tirilla, listo);
            cuerpo.Add(listo);
        }

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 12,
                Children = { cuerpo }
            }
        };

        if (!_impresionAutomaticaHecha)
        {
            _impresionAutomaticaHecha = true;
            _ = ImprimirAsync(tirilla);
        }
    }

    private static View Recibo(TirillaVenta tirilla)
    {
        var combinada = TirillaCuerpo.EsCombinada(tirilla.Tipo);
        var fecha = tirilla.Fecha;
        var recuadro = new VerticalStackLayout
        {
            Spacing = 6,
            HorizontalOptions = LayoutOptions.Fill
        };
        recuadro.Add(Regla());
        recuadro.Add(Fila("RECIBO DE VENTA", tirilla.CodigoImpreso));
        recuadro.Add(Regla());
        recuadro.Add(Fila($"Fecha: {fecha:yyyy-MM-dd}", $"Hora: {fecha:HH:mm}"));
        recuadro.Add(Fila("Tipo de apuesta:", TirillaCuerpo.EtiquetaTipo(tirilla.Tipo)));
        recuadro.Add(Regla());
        recuadro.Add(new Label
        {
            Text = "JUGADO",
            FontFamily = "OpenSansRegular",
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            TextColor = Ui.Ink
        });
        recuadro.Add(Regla());
        if (combinada && tirilla.Lineas.Count > 0)
        {
            var linea = tirilla.Lineas[0];
            recuadro.Add(EncabezadoGrilla("NUMERO", "VALOR", "TOTAL"));
            recuadro.Add(FilaGrilla(linea.Numero, FormatoDinero.Pesos(linea.Valor), FormatoDinero.Pesos(linea.TotalLinea)));
            recuadro.Add(Mono("LOTERIAS: " + string.Join(", ", linea.LoteriaNombres).ToUpperInvariant()));
        }
        else
        {
            recuadro.Add(EncabezadoGrilla("NUMERO", "VALOR", "LOTERIA"));
            foreach (var linea in tirilla.Lineas)
            {
                var loteria = (linea.LoteriaNombres.FirstOrDefault() ?? string.Empty).ToUpperInvariant();
                recuadro.Add(FilaGrilla(linea.Numero, FormatoDinero.Pesos(linea.Valor), loteria));
            }
        }

        recuadro.Add(Regla());
        recuadro.Add(Fila("TOTAL APOSTADO", FormatoDinero.Pesos(tirilla.Total), true));
        recuadro.Add(Regla());
        if (!string.IsNullOrWhiteSpace(tirilla.QrContenido))
        {
            var png = QrImagen.Png(tirilla.QrContenido);
            if (png.Length > 0)
            {
                var copia = png;
                recuadro.Add(new Image
                {
                    Source = ImageSource.FromStream(() => new MemoryStream(copia)),
                    HeightRequest = 128,
                    WidthRequest = 128,
                    HorizontalOptions = LayoutOptions.Center
                });
            }
        }

        recuadro.Add(Regla());
        recuadro.Add(new Label
        {
            Text = string.IsNullOrWhiteSpace(tirilla.LeyendaCompleta)
                ? TirillaCuerpo.LeyendaDeRespuesta(tirilla.VigenciaDias, null)
                : tirilla.LeyendaCompleta,
            FontFamily = "OpenSansRegular",
            FontSize = 12,
            TextColor = Ui.Ink
        });
        recuadro.Add(Regla());

        return new Border
        {
            Stroke = Ui.Line,
            StrokeThickness = 1,
            StrokeDashArray = [4, 3],
            Padding = 14,
            BackgroundColor = Color.FromArgb("#fbfdfc"),
            Content = recuadro
        };
    }

    private static Label Mono(string texto) => new()
    {
        Text = texto,
        FontFamily = "OpenSansRegular",
        FontSize = 12,
        TextColor = Ui.Ink
    };

    private static View Regla() => new Grid
    {
        HeightRequest = 16,
        HorizontalOptions = LayoutOptions.Fill,
        IsClippedToBounds = true,
        Children =
        {
            new Label
            {
                Text = TirillaRegla.De(TirillaRegla.CaracteresPantalla),
                FontFamily = "OpenSansRegular",
                FontSize = 12,
                LineBreakMode = LineBreakMode.NoWrap,
                MaxLines = 1,
                TextColor = Ui.Ink
            }
        }
    };

    private static Grid Fila(string izquierda, string derecha, bool negrita = false)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        grid.Add(new Label
        {
            Text = izquierda,
            FontFamily = "OpenSansRegular",
            FontAttributes = negrita ? FontAttributes.Bold : FontAttributes.None,
            FontSize = 12,
            TextColor = Ui.Ink
        }, 0);
        grid.Add(new Label
        {
            Text = derecha,
            FontFamily = "OpenSansRegular",
            FontAttributes = FontAttributes.Bold,
            FontSize = 12,
            HorizontalTextAlignment = TextAlignment.End,
            TextColor = Ui.Ink
        }, 1);
        return grid;
    }

    private static Grid EncabezadoGrilla(string a, string b, string c) => FilaGrilla(a, b, c, true);

    private static Grid FilaGrilla(string a, string b, string c, bool encabezado = false)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            }
        };
        var peso = encabezado ? FontAttributes.Bold : FontAttributes.None;
        grid.Add(new Label { Text = a, FontFamily = "OpenSansRegular", FontAttributes = peso, FontSize = 12, TextColor = Ui.Ink }, 0);
        grid.Add(new Label { Text = b, FontFamily = "OpenSansRegular", FontAttributes = peso, FontSize = 12, HorizontalTextAlignment = TextAlignment.Center, TextColor = Ui.Ink }, 1);
        grid.Add(new Label { Text = c, FontFamily = "OpenSansRegular", FontAttributes = peso, FontSize = 12, HorizontalTextAlignment = TextAlignment.End, TextColor = Ui.Ink }, 2);
        return grid;
    }

    protected override bool OnBackButtonPressed() =>
        _sesion.Tirilla?.Offline == true && !_evidenciaEnviada || base.OnBackButtonPressed();

    private async Task ImprimirAsync(TirillaVenta tirilla)
    {
        var resultado = await _printer.ImprimirAsync(tirilla.Texto, tirilla.QrContenido);
        if (resultado.Ok)
        {
            return;
        }

        MostrarAviso(resultado.Mensaje);
    }

    private async Task GenerarPdfAsync(TirillaVenta tirilla)
    {
        try
        {
            var bytes = TirillaPdf.Generar(tirilla.ARespuesta());
            await _pdf.CompartirAsync($"{tirilla.CodigoImpreso}.pdf", bytes);
        }
        catch (Exception)
        {
            MostrarAviso(PdaTexts.ErrorPdf);
        }
    }

    private void MostrarAviso(string mensaje)
    {
        if (_avisoImpresion is null)
        {
            return;
        }

        _avisoImpresion.Text = mensaje;
        _avisoImpresion.IsVisible = true;
    }

    private async Task EnviarEvidenciaAsync(TirillaVenta tirilla, Button boton)
    {
        boton.IsEnabled = false;
        try
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                var foto = await MediaPicker.Default.CapturePhotoAsync();
                if (foto is null)
                {
                    return;
                }
            }

            var png = QrImagen.Png(tirilla.QrContenido);
            if (png.Length == 0)
            {
                await this.AvisoAsync(PdaTexts.TicketVendido, PdaTexts.FotoOffline, PdaTexts.Cerrar);
                return;
            }

            var inicio = await _api.IniciarChatAsync(new NewRich.Application.Contracts.Chat.IniciarChatRequest
            {
                Texto = EvidenciaOffline.MensajeChat
            }, CancellationToken.None);
            if (!inicio.IsSuccess || inicio.Data is null)
            {
                await this.AvisoAsync(PdaTexts.Soporte, inicio.Message, PdaTexts.Cerrar);
                return;
            }

            var envio = await _api.EnviarMensajeAsync(
                inicio.Data.ConversacionId,
                EvidenciaOffline.MensajeConQr(tirilla.CodigoImpreso, png),
                CancellationToken.None);
            if (!envio.IsSuccess)
            {
                await this.AvisoAsync(PdaTexts.Soporte, envio.Message, PdaTexts.Cerrar);
                return;
            }

            _evidenciaEnviada = true;
            await CerrarAsync();
        }
        finally
        {
            boton.IsEnabled = true;
        }
    }

    private async Task CerrarAsync()
    {
        _sesion.Tirilla = null;
        _sesion.Borrador = null;
        await Shell.Current.GoToAsync("//inicio");
    }
}
