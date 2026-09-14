using Microsoft.Maui.Controls.Shapes;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Premios;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Maui.Services;
using NewRich.Maui.Views;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;

namespace NewRich.Maui.Views.Vendedor;

public sealed class ValidarTicketPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly ILectorCodigoBarrasServicio _lector;
    private readonly IEscanerQrVendedor _camaraQr;
    private readonly ILectorQrFotoVendedor _fotos;
    private readonly Entry _codigo;
    private readonly Entry _lecturaQr;
    private readonly Label _aviso;
    private readonly Border _recibo;
    private readonly VerticalStackLayout _cuerpoRecibo;
    private readonly Button _reportar;
    private readonly CargandoOverlay _cargando = new();
    private readonly VerticalStackLayout _opciones;
    private readonly VerticalStackLayout _detalle;
    private readonly VerticalStackLayout _flujoCodigo;
    private readonly VerticalStackLayout _flujoQr;
    private readonly VerticalStackLayout _flujoImagen;
    private string _ticketConsultado = string.Empty;
    private bool _escuchando;
    private bool _validando;
    private bool _modoQr;
    private bool _esperandoEscaner;
    private bool _escribiendoLectura;
    private int _oleadaLectura;

    public ValidarTicketPage(
        NewRichApiClient api,
        ILectorCodigoBarrasServicio lector,
        IEscanerQrVendedor camaraQr,
        ILectorQrFotoVendedor fotos)
    {
        _api = api;
        _lector = lector;
        _camaraQr = camaraQr;
        _fotos = fotos;
        Title = PdaTexts.ValidarTicket;
        Title = PdaTexts.ValidarTicket;
        BackgroundColor = Color.FromArgb("#eef4f0");
        HideSoftInputOnTapped = true;
        _codigo = Ui.Entrada("AOL-0000001");
        _codigo.MaxLength = 32;
        _codigo.ReturnType = ReturnType.Go;
        _codigo.Completed += async (_, _) => await ValidarAsync(_codigo.Text);
        _lecturaQr = Ui.Entrada(PdaTexts.LecturaDelScanner);
        _lecturaQr.MaxLength = TicketCodeLimits.MaxInputLength;
        _lecturaQr.ReturnType = ReturnType.Go;
        _lecturaQr.Completed += async (_, _) => await ValidarAsync(_lecturaQr.Text, desdeQr: true, errorEnPopup: true);
        _lecturaQr.TextChanged += (_, args) =>
        {
            if (_escribiendoLectura || _validando)
            {
                return;
            }

            var n = ++_oleadaLectura;
            var texto = args.NewTextValue;
            Dispatcher.DispatchDelayed(
                TimeSpan.FromMilliseconds(LecturaTicket.MsEstabilizacionWedge),
                async () =>
                {
                    if (n != _oleadaLectura || _validando)
                    {
                        return;
                    }

                    if (LecturaTicket.ListaParaConsultar(texto))
                    {
                        await ValidarAsync(texto, desdeQr: true, errorEnPopup: true);
                    }
                });
        };
#if ANDROID
        _lecturaQr.HandlerChanged += (_, _) =>
        {
            if (_lecturaQr.Handler?.PlatformView is Android.Widget.EditText caja)
            {
                caja.ShowSoftInputOnFocus = false;
            }
        };
#endif
        _aviso = new Label { FontSize = 13, TextColor = Ui.Ink };
        _cuerpoRecibo = new VerticalStackLayout { Spacing = 0 };
        _recibo = new Border
        {
            Stroke = Color.FromArgb("#c9a44a"),
            StrokeThickness = 1.5,
            BackgroundColor = Color.FromArgb("#fffaf0"),
            StrokeShape = new RoundRectangle { CornerRadius = 4 },
            Padding = 0,
            IsVisible = false,
            Content = _cuerpoRecibo,
            Shadow = new Shadow
            {
                Brush = Color.FromArgb("#1a123d35"),
                Offset = new Point(0, 6),
                Radius = 16,
                Opacity = 0.35f
            }
        };
        _reportar = Ui.Primario(PdaTexts.ReportarCaso);
        _reportar.IsVisible = false;
        _reportar.Clicked += async (_, _) => await ReportarAsync();
        var validar = Ui.Primario(PdaTexts.Consultar);
        validar.Clicked += async (_, _) => await ValidarAsync(_codigo.Text);
        var otraForma = Ui.Secundario(PdaTexts.OtraFormaDeValidar);
        otraForma.Clicked += (_, _) => MostrarOpciones();
        _opciones = new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                new Label
                {
                    Text = PdaTexts.ValidarTicketElegir,
                    TextColor = Ui.Muted,
                    FontSize = 15,
                    LineBreakMode = LineBreakMode.WordWrap
                },
                BotonOpcion(PdaTexts.ValidarCodigoVenta, PdaTexts.ValidarCodigoVentaAyuda, MostrarCodigo),
                BotonOpcion(PdaTexts.ValidarQr, PdaTexts.ValidarQrAyuda, () => _ = IniciarQrAsync()),
                BotonOpcion(PdaTexts.ValidarConImagen, PdaTexts.ValidarConImagenAyuda, MostrarImagen)
            }
        };
        _flujoCodigo = new VerticalStackLayout
        {
            Spacing = 12,
            IsVisible = false,
            Children =
            {
                Ui.Campo(PdaTexts.TicketCode),
                _codigo,
                validar
            }
        };
        var leerQr = Ui.Primario(PdaTexts.LeerQr);
        leerQr.Clicked += async (_, _) => await EscanearCamaraAsync();
        var consultarQr = Ui.Secundario(PdaTexts.Consultar);
        consultarQr.Clicked += async (_, _) => await ValidarAsync(_lecturaQr.Text, desdeQr: true, errorEnPopup: true);
        _flujoQr = new VerticalStackLayout
        {
            Spacing = 12,
            IsVisible = false,
            Children =
            {
                Ui.Tarjeta(new Label
                {
                    Text = PdaTexts.EsperandoLector,
                    TextColor = Ui.Ink,
                    FontSize = 15,
                    LineBreakMode = LineBreakMode.WordWrap
                }),
                leerQr,
                _lecturaQr,
                consultarQr
            }
        };
        var tomarFoto = Ui.Primario(PdaTexts.TomarFotoQr);
        tomarFoto.Clicked += async (_, _) => await LeerFotoTomadaAsync();
        var subirFoto = Ui.Secundario(PdaTexts.SubirFotoQr);
        subirFoto.Clicked += async (_, _) => await LeerFotoSubidaAsync();
        _flujoImagen = new VerticalStackLayout
        {
            Spacing = 12,
            IsVisible = false,
            Children =
            {
                Ui.Tarjeta(new Label
                {
                    Text = PdaTexts.ValidarConImagenAyuda,
                    TextColor = Ui.Ink,
                    FontSize = 15,
                    LineBreakMode = LineBreakMode.WordWrap
                }),
                tomarFoto,
                subirFoto
            }
        };
        _detalle = new VerticalStackLayout
        {
            Spacing = 12,
            IsVisible = false,
            Children =
            {
                otraForma,
                _flujoCodigo,
                _flujoQr,
                _flujoImagen,
                _recibo,
                _reportar,
                _aviso
            }
        };
        var formulario = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 18, 16, 28),
                Spacing = 12,
                Children = { _opciones, _detalle }
            }
        };
        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Star) },
            Children = { formulario, _cargando }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_modoQr)
        {
            EscucharLector();
        }
    }

    protected override void OnDisappearing()
    {
        if (!_esperandoEscaner)
        {
            SoltarLector();
        }

        base.OnDisappearing();
    }

    private void MostrarOpciones()
    {
        SoltarLector();
        _modoQr = false;
        LimpiarConsulta();
        _opciones.IsVisible = true;
        _detalle.IsVisible = false;
        _flujoCodigo.IsVisible = false;
        _flujoQr.IsVisible = false;
        _flujoImagen.IsVisible = false;
    }

    private void MostrarCodigo()
    {
        SoltarLector();
        _modoQr = false;
        LimpiarConsulta();
        _opciones.IsVisible = false;
        _detalle.IsVisible = true;
        _flujoCodigo.IsVisible = true;
        _flujoQr.IsVisible = false;
        _flujoImagen.IsVisible = false;
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), () => _codigo.Focus());
    }

    private void MostrarImagen()
    {
        SoltarLector();
        _modoQr = false;
        LimpiarConsulta();
        _opciones.IsVisible = false;
        _detalle.IsVisible = true;
        _flujoCodigo.IsVisible = false;
        _flujoQr.IsVisible = false;
        _flujoImagen.IsVisible = true;
    }

    private async Task IniciarQrAsync()
    {
        _modoQr = true;
        LimpiarConsulta();
        _opciones.IsVisible = false;
        _detalle.IsVisible = true;
        _flujoCodigo.IsVisible = false;
        _flujoQr.IsVisible = true;
        _flujoImagen.IsVisible = false;
        if (LecturaTicket.AbrirEscanerDispositivo)
        {
            _esperandoEscaner = true;
            try
            {
                await EscanearCamaraAsync();
            }
            finally
            {
                _esperandoEscaner = false;
            }
        }
    }

    private async Task EscanearCamaraAsync()
    {
        try
        {
            var leido = await _camaraQr.EscanearAsync();
            var codigo = LecturaTicket.CodigoParaPegar(leido, null);
            if (string.IsNullOrWhiteSpace(codigo))
            {
                await MostrarErrorAsync(PdaTexts.CodigoNoLeido, true);
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                PegarLectura(codigo);
                await ValidarAsync(codigo, desdeQr: true, errorEnPopup: true);
            });
        }
        catch (Exception)
        {
            await MostrarErrorAsync(PdaTexts.CodigoNoLeido, true);
        }
    }

    private async Task LeerFotoTomadaAsync()
    {
        FileResult? archivo;
        try
        {
            var permiso = await Permissions.RequestAsync<Permissions.Camera>();
            if (permiso != PermissionStatus.Granted)
            {
                await this.AvisoAsync(PdaTexts.ValidarConImagen, PdaTexts.CodigoNoLeido, PdaTexts.Cerrar);
                return;
            }

            archivo = await MediaPicker.Default.CapturePhotoAsync();
        }
        catch (Exception)
        {
            await this.AvisoAsync(PdaTexts.ValidarConImagen, PdaTexts.QrNoEncontradoEnFoto, PdaTexts.Cerrar);
            return;
        }

        await DecodificarArchivoAsync(archivo, subida: false);
    }

    private async Task LeerFotoSubidaAsync()
    {
        FileResult? archivo;
        try
        {
            archivo = await MediaPicker.Default.PickPhotoAsync();
        }
        catch (Exception)
        {
            await this.AvisoAsync(PdaTexts.ValidarConImagen, PdaTexts.QrNoEncontradoEnFoto, PdaTexts.Cerrar);
            return;
        }

        await DecodificarArchivoAsync(archivo, subida: true);
    }

    private async Task DecodificarArchivoAsync(FileResult? archivo, bool subida)
    {
        if (archivo is null)
        {
            return;
        }

        _cargando.Mostrar(PdaTexts.LeyendoQrFoto);
        await Task.Delay(200);
        string? codigo = null;
        try
        {
            await using var stream = await archivo.OpenReadAsync();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            var bytes = buffer.ToArray();
            codigo = subida
                ? await _fotos.LeerFotoSubidaAsync(bytes)
                : await _fotos.LeerAsync(bytes);
        }
        catch (Exception)
        {
            codigo = null;
        }
        finally
        {
            _cargando.Ocultar();
        }

        if (string.IsNullOrWhiteSpace(codigo))
        {
            await this.AvisoAsync(PdaTexts.ValidarConImagen, PdaTexts.QrNoEncontradoEnFoto, PdaTexts.Cerrar);
            return;
        }

        await ValidarAsync(codigo, desdeQr: true, errorEnPopup: true);
    }

    private void EscucharLector()
    {
        if (_escuchando)
        {
            return;
        }

        try
        {
            _lector.CodigoLeido += AlLeerCodigo;
            _lector.Activar();
            _escuchando = true;
        }
        catch (Exception)
        {
            _lector.CodigoLeido -= AlLeerCodigo;
            _escuchando = false;
        }
    }

    private void SoltarLector()
    {
        if (!_escuchando)
        {
            return;
        }

        _lector.CodigoLeido -= AlLeerCodigo;
        _lector.Desactivar();
        _escuchando = false;
    }

    private void LimpiarConsulta()
    {
        _ticketConsultado = string.Empty;
        _aviso.Text = string.Empty;
        _recibo.IsVisible = false;
        _reportar.IsVisible = false;
        _cuerpoRecibo.Children.Clear();
        _codigo.Text = string.Empty;
        PegarLectura(string.Empty);
    }

    private void PegarLectura(string texto)
    {
        _escribiendoLectura = true;
        try
        {
            _lecturaQr.Text = texto;
        }
        finally
        {
            _escribiendoLectura = false;
        }
    }

    private static Button BotonOpcion(string titulo, string ayuda, Action alElegir)
    {
        var boton = new Button
        {
            Text = $"{titulo}\n{ayuda}",
            BackgroundColor = Colors.White,
            TextColor = Ui.Ink,
            BorderColor = Ui.Line,
            BorderWidth = 1,
            CornerRadius = 12,
            HeightRequest = 72
        };
        boton.Clicked += (_, _) => alElegir();
        return boton;
    }

    private async void AlLeerCodigo(object? sender, string codigo)
    {
        if (!_modoQr)
        {
            return;
        }

        PegarLectura(codigo);
        await ValidarAsync(codigo, desdeQr: true, errorEnPopup: true);
    }

    private async Task ValidarAsync(string? codigo, bool desdeQr = false, bool errorEnPopup = false)
    {
        if (_validando)
        {
            return;
        }

        _validando = true;
        _reportar.IsVisible = false;
        _recibo.IsVisible = false;
        _cuerpoRecibo.Children.Clear();
        _aviso.Text = string.Empty;
        var ticket = codigo?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ticket))
        {
            await MostrarErrorAsync(
                desdeQr ? PdaTexts.CodigoNoLeido : PremioMessages.TicketRequerido,
                errorEnPopup || desdeQr);
            _validando = false;
            return;
        }

        if (desdeQr)
        {
            _codigo.Text = string.Empty;
            PegarLectura(ticket);
        }

        _cargando.Mostrar(desdeQr || errorEnPopup ? PdaTexts.ValidandoQr : PdaTexts.Cargando);

        try
        {
            var consulta = await _api.ConsultarTicketAsync(new ConsultaTicketRequest
            {
                TicketCode = ticket
            }, CancellationToken.None);
            if (!consulta.IsSuccess || consulta.Data is null)
            {
                _ticketConsultado = string.Empty;
                await MostrarErrorAsync(
                    string.IsNullOrWhiteSpace(consulta.Message) ? PremioMessages.TicketNoEncontrado : consulta.Message,
                    errorEnPopup || desdeQr);
                return;
            }

            _ticketConsultado = ticket;
            PintarRecibo(TicketConsultaVista.De(consulta.Data));
            _aviso.Text = string.Empty;
        }
        catch (Exception)
        {
            _ticketConsultado = string.Empty;
            await MostrarErrorAsync(PdaTexts.SinConexionServidor, errorEnPopup || desdeQr);
        }
        finally
        {
            _cargando.Ocultar();
            _validando = false;
        }
    }

    private async Task MostrarErrorAsync(string mensaje, bool popup)
    {
        if (popup)
        {
            await this.AvisoAsync(PdaTexts.ValidarTicket, mensaje, PdaTexts.Cerrar);
            return;
        }

        _aviso.Text = mensaje;
        _aviso.TextColor = Ui.Danger;
    }

    private async Task ReportarAsync()
    {
        try
        {
            var ticket = string.IsNullOrWhiteSpace(_ticketConsultado)
                ? _codigo.Text?.Trim() ?? string.Empty
                : _ticketConsultado;
            var resultado = await _api.ReportarPremioAsync(new ReportarCasoGanadorRequest
            {
                TicketCode = ticket
            }, CancellationToken.None);
            _aviso.Text = resultado.IsSuccess
                ? $"{resultado.Message} {resultado.Data?.Ticket}"
                : resultado.Message;
            _aviso.TextColor = resultado.IsSuccess ? Ui.Green : Ui.Danger;
            if (resultado.IsSuccess)
            {
                _reportar.IsVisible = false;
            }
        }
        catch (Exception)
        {
            _aviso.Text = PdaTexts.SinConexionServidor;
            _aviso.TextColor = Ui.Danger;
        }
    }

    private void PintarRecibo(TicketConsultaVista vista)
    {
        _recibo.StrokeThickness = 0;
        _recibo.Padding = 0;
        _recibo.BackgroundColor = Colors.Transparent;
        _recibo.Shadow = null;
        _cuerpoRecibo.Children.Add(ReciboConsultaVista.Crear(vista));
        _recibo.IsVisible = true;
        _reportar.IsVisible = vista.PuedeReportar;
    }
}
