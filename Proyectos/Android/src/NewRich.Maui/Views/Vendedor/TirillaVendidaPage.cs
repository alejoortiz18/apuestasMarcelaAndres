using NewRich.Application.Contracts.Chat;
using NewRich.Application.Services;
using NewRich.Domain.Enums;
using NewRich.Maui.Data;
using NewRich.Maui.Services;
using NewRich.Maui.Views;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;

namespace NewRich.Maui.Views.Vendedor;

public sealed class TirillaVendidaPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly SesionPda _sesion;
    private readonly IPrinterService _printer;
    private readonly IPdfService _pdf;
    private readonly SincronizacionOfflineServicio _sincronizacion;
    private readonly LocalDatabase _offline;
    private readonly ILectorCodigoBarrasServicio _lector;
    private bool _evidenciaEnviada;
    private bool _capturaOk;
    private bool _impresionAutomaticaHecha;
    private bool _escuchandoLector;
    private Label? _avisoImpresion;
    private Button? _reportar;
    private Label? _estadoReporte;
    private VerticalStackLayout? _formularioReporte;
    private Entry? _detalleReporte;
    private readonly CargandoOverlay _cargando = new();

    public TirillaVendidaPage(
        NewRichApiClient api,
        SesionPda sesion,
        IPrinterService printer,
        IPdfService pdf,
        SincronizacionOfflineServicio sincronizacion,
        LocalDatabase offline,
        ILectorCodigoBarrasServicio lector)
    {
        _api = api;
        _sesion = sesion;
        _printer = printer;
        _pdf = pdf;
        _sincronizacion = sincronizacion;
        _offline = offline;
        _lector = lector;
        Title = PdaTexts.TicketVendido;
        BackgroundColor = Ui.Paper;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            PintarTirilla();
        }
        catch (Exception)
        {
            Content = new Label { Text = PdaTexts.ErrorImpresion, Padding = 16, TextColor = Ui.Danger };
        }
    }

    private void PintarTirilla()
    {
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

        _reportar = Ui.Secundario(PdaTexts.Reportar);
        _reportar.Clicked += (_, _) => MostrarFormularioReporte(true);

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

        _estadoReporte = new Label
        {
            Text = string.Empty,
            TextColor = Ui.Muted,
            FontSize = 13,
            IsVisible = false
        };

        _detalleReporte = new Entry
        {
            Placeholder = PdaTexts.ObservacionReportePlaceholder,
            BackgroundColor = Colors.White,
            TextColor = Ui.Ink,
            FontSize = 15,
            HeightRequest = 44
        };
        var enviarReporte = Ui.Primario(PdaTexts.EnviarReporte);
        enviarReporte.Clicked += async (_, _) => await EnviarReporteAsync(tirilla);
        var cancelarReporte = Ui.Secundario(PdaTexts.CancelarReporte);
        cancelarReporte.Clicked += (_, _) => MostrarFormularioReporte(false);
        _formularioReporte = new VerticalStackLayout
        {
            Spacing = 8,
            IsVisible = false,
            Children =
            {
                new Label
                {
                    Text = $"{PdaTexts.ObservacionReporte} *",
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Ui.Ink
                },
                _detalleReporte,
                enviarReporte,
                cancelarReporte
            }
        };

        var pdf = Ui.Secundario(PdaTexts.GenerarPdf);
        pdf.Clicked += async (_, _) => await GenerarPdfAsync(tirilla);

        cuerpo.Add(_reportar);
        cuerpo.Add(_formularioReporte);
        cuerpo.Add(_estadoReporte);
        cuerpo.Add(cerrar);
        cuerpo.Add(pdf);

        if (tirilla.Offline)
        {
            cuerpo.Add(Ui.Banner(PdaTexts.FotoOffline, Ui.WarnBg, Ui.Warn));
            var leer = Ui.Secundario(PdaTexts.LeerCodigoBarras);
            leer.Clicked += (_, _) =>
            {
                EscucharLector();
                _lector.Disparar();
            };
            var listo = Ui.Primario(PdaTexts.FotoLista);
            listo.Clicked += async (_, _) => await EnviarEvidenciaAsync(tirilla, listo);
            cuerpo.Add(leer);
            cuerpo.Add(listo);
            EscucharLector();
        }

        Content = new Grid
        {
            Children =
            {
                new ScrollView
                {
                    Content = new VerticalStackLayout
                    {
                        Padding = 16,
                        Spacing = 12,
                        Children = { cuerpo }
                    }
                },
                _cargando
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
            byte[] png = [];
            try
            {
                // Version liviana: el QR en pantalla se ve a 128 puntos y el grande tardaba en generarse.
                png = QrImagen.PngParaTirilla(tirilla.QrContenido, 384);
            }
            catch (Exception)
            {
            }

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
            FontSize = 11,
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

    private void MostrarFormularioReporte(bool visible)
    {
        if (_formularioReporte is null || _detalleReporte is null)
        {
            return;
        }

        _formularioReporte.IsVisible = visible;
        if (_reportar is not null)
        {
            _reportar.IsVisible = !visible;
        }

        if (visible)
        {
            _detalleReporte.Focus();
        }
        else
        {
            _detalleReporte.Text = string.Empty;
        }
    }

    private async Task EnviarReporteAsync(TirillaVenta tirilla)
    {
        var detalle = _detalleReporte?.Text;
        var error = ReporteTecnicoRegla.ValidarObservacion(detalle);
        if (error is not null)
        {
            MostrarAviso(error);
            return;
        }

        MostrarAviso(string.Empty);
        _cargando.Mostrar(PdaTexts.EnviandoReporte);
        try
        {
            var pdf = TirillaPdf.Generar(tirilla.ARespuesta());
            var nombre = $"{tirilla.CodigoImpreso}.pdf";
            var request = new ReporteTecnicoRequest
            {
                Observacion = detalle!.Trim(),
                CodigoTicket = tirilla.CodigoImpreso,
                FechaTicket = tirilla.Fecha,
                NombreArchivo = nombre,
                ContenidoBase64 = Convert.ToBase64String(pdf)
            };

            var envio = await _api.ReportarTecnicoAsync(request, CancellationToken.None);
            if (envio.IsSuccess)
            {
                MostrarEstadoReporte(EstadoReporteTecnico.Enviado);
                MostrarFormularioReporte(false);
                return;
            }

            await EncolarReporteLocalAsync(tirilla, detalle.Trim(), pdf, nombre);
            MostrarEstadoReporte(EstadoReporteTecnico.EsperandoConexion);
            MostrarFormularioReporte(false);
        }
        catch (Exception)
        {
            try
            {
                var pdf = TirillaPdf.Generar(tirilla.ARespuesta());
                await EncolarReporteLocalAsync(tirilla, detalle!.Trim(), pdf, $"{tirilla.CodigoImpreso}.pdf");
                MostrarEstadoReporte(EstadoReporteTecnico.EsperandoConexion);
                MostrarFormularioReporte(false);
            }
            catch (Exception)
            {
                MostrarAviso(PdaTexts.SinConexionServidor);
            }
        }
        finally
        {
            _cargando.Ocultar();
        }
    }

    private async Task EncolarReporteLocalAsync(TirillaVenta tirilla, string observacion, byte[] pdf, string nombre)
    {
        await _offline.AsegurarAsync();
        var id = Guid.NewGuid().ToString("N");
        var ruta = Path.Combine(FileSystem.AppDataDirectory, $"reporte-tecnico-{id}.pdf");
        await File.WriteAllBytesAsync(ruta, pdf);
        await _offline.GuardarReporteTecnicoAsync(new ReporteTecnicoLocal
        {
            Id = id,
            Observacion = observacion,
            CodigoTicket = tirilla.CodigoImpreso,
            FechaTicket = tirilla.Fecha,
            NombreArchivo = nombre,
            RutaPdf = ruta,
            Enviado = false,
            Estado = EstadoReporteTecnico.EsperandoConexion,
            FechaLocal = DateTime.Now.ToString("O")
        });
    }

    private void MostrarEstadoReporte(string estado)
    {
        if (_estadoReporte is null)
        {
            return;
        }

        _estadoReporte.Text = estado;
        _estadoReporte.IsVisible = !string.IsNullOrWhiteSpace(estado);
    }

    private async Task ImprimirAsync(TirillaVenta tirilla)
    {
        MostrarAviso(EstadoImpresora.Aviso(null), EstadoImpresora.EsError(null));
        try
        {
            var resultado = await _printer.ImprimirAsync(tirilla.Texto, tirilla.QrContenido);
            if (resultado.Ok)
            {
                MostrarAviso(EstadoImpresora.Aviso(true), EstadoImpresora.EsError(true));
                return;
            }

            MostrarAviso(resultado.Mensaje, true);
        }
        catch (Exception)
        {
            MostrarAviso(EstadoImpresora.Aviso(false), EstadoImpresora.EsError(false));
        }
    }

    private async Task GenerarPdfAsync(TirillaVenta tirilla)
    {
        _cargando.Mostrar(PdaTexts.GenerandoPdf);
        try
        {
            var bytes = TirillaPdf.Generar(tirilla.ARespuesta());
            await _pdf.CompartirAsync($"{tirilla.CodigoImpreso}.pdf", bytes);
        }
        catch (Exception)
        {
            MostrarAviso(PdaTexts.ErrorPdf);
        }
        finally
        {
            _cargando.Ocultar();
        }
    }

    private void MostrarAviso(string mensaje, bool esError = true)
    {
        if (_avisoImpresion is null)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _avisoImpresion.Text = mensaje;
            _avisoImpresion.TextColor = esError ? Ui.Danger : Ui.Muted;
            _avisoImpresion.IsVisible = !string.IsNullOrWhiteSpace(mensaje);
        });
    }

    protected override void OnDisappearing()
    {
        SoltarLector();
        base.OnDisappearing();
    }

    private void EscucharLector()
    {
        if (_escuchandoLector)
        {
            return;
        }

        try
        {
            _lector.CodigoLeido += AlLeerCodigo;
            _lector.Activar();
            _escuchandoLector = true;
        }
        catch (Exception)
        {
        }
    }

    private void SoltarLector()
    {
        if (!_escuchandoLector)
        {
            return;
        }

        _lector.CodigoLeido -= AlLeerCodigo;
        _lector.Desactivar();
        _escuchandoLector = false;
    }

    private void AlLeerCodigo(object? sender, string codigo)
    {
        var esperado = _sesion.Tirilla?.QrContenido ?? string.Empty;
        if (!EvidenciaOffline.EsElMismoQr(codigo, esperado))
        {
            MostrarAviso(PdaTexts.QrNoCoincide);
            return;
        }

        _capturaOk = true;
        MostrarAviso(string.Empty);
    }

    private async Task EnviarEvidenciaAsync(TirillaVenta tirilla, Button boton)
    {
        boton.IsEnabled = false;
        try
        {
            if (!_capturaOk)
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    await this.AvisoAsync(PdaTexts.TicketVendido, PdaTexts.FotoObligatoria, PdaTexts.Cerrar);
                    return;
                }

                var foto = await MediaPicker.Default.CapturePhotoAsync();
                if (foto is null)
                {
                    await this.AvisoAsync(PdaTexts.TicketVendido, PdaTexts.FotoObligatoria, PdaTexts.Cerrar);
                    return;
                }

                _capturaOk = true;
            }

            byte[] png = [];
            try
            {
                png = QrImagen.Png(tirilla.QrContenido);
            }
            catch (Exception)
            {
            }

            var enviado = DateTime.Now;
            try
            {
                var inicio = await _api.IniciarChatAsync(new IniciarChatRequest
                {
                    Texto = EvidenciaOffline.TextoChat(tirilla.CodigoImpreso, enviado)
                }, CancellationToken.None);
                if (inicio.IsSuccess && inicio.Data is not null && png.Length > 0)
                {
                    await _api.EnviarMensajeAsync(
                        inicio.Data.ConversacionId,
                        EvidenciaOffline.MensajeConQr(tirilla.CodigoImpreso, png, enviado),
                        CancellationToken.None);
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception)
            {
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
        try
        {
            var rol = _sesion.Usuario?.Rol ?? RolUsuario.Vendedor;
            await _sincronizacion.SincronizarEnSilencioAsync(false, rol, false, CancellationToken.None);
        }
        catch (Exception)
        {
        }

        _sesion.Tirilla = null;
        _sesion.Borrador = null;
        try
        {
            await Shell.Current.GoToAsync("//inicio");
        }
        catch (Exception)
        {
        }
    }
}
