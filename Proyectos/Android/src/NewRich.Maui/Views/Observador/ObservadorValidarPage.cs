using NewRich.Application.Contracts.Boletos;
using NewRich.Maui.Services;
using NewRich.Maui.Views;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Shared.Results;

namespace NewRich.Maui.Views.Observador;

public sealed class ObservadorValidarPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly IEscanerQrObservador _camara;
    private readonly ILectorQrFotoObservador _fotos;
    private readonly VerticalStackLayout _recibo = new() { Spacing = 12 };
    private readonly CargandoOverlay _cargando = new();
    private bool _ocupado;

    public ObservadorValidarPage(
        NewRichApiClient api,
        IEscanerQrObservador camara,
        ILectorQrFotoObservador fotos)
    {
        _api = api;
        _camara = camara;
        _fotos = fotos;
        Title = PdaTexts.ObservadorValidarTitulo;
        BackgroundColor = Ui.Paper;

        var leer = Ui.Primario(PdaTexts.ObservadorLeerQrCamara);
        leer.Clicked += async (_, _) => await LeerCamaraAsync();
        var tomar = Ui.Primario(PdaTexts.ObservadorTomarFotoQr);
        tomar.Clicked += async (_, _) => await LeerFotoAsync();
        var subir = Ui.Secundario(PdaTexts.ObservadorSubirFotoQr);
        subir.Clicked += async (_, _) => await LeerFotoSubidaAsync();

        var formulario = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 18, 16, 28),
                Spacing = 12,
                Children =
                {
                    Ui.Tarjeta(new Label
                    {
                        Text = PdaTexts.ObservadorValidarAyuda,
                        TextColor = Ui.Ink,
                        FontSize = 15,
                        LineBreakMode = LineBreakMode.WordWrap
                    }),
                    leer,
                    tomar,
                    subir,
                    _recibo
                }
            }
        };
        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Star) },
            Children = { formulario, _cargando }
        };
    }

    private Result<ConsultaTicketResponse>? _consultaPrevia;
    private string? _errorConsultaPrevia;

    private async Task LeerCamaraAsync()
    {
        if (_ocupado)
        {
            return;
        }

        _ocupado = true;
        _consultaPrevia = null;
        _errorConsultaPrevia = null;
        string? ticketConsultado = null;

        try
        {
            var codigo = await _camara.EscanearAsync(async codigoLeido =>
            {
                var limpio = codigoLeido.Trim();
                if (ObservadorConsultaTicket.ListoParaConsultar(limpio))
                {
                    ticketConsultado = limpio;
                    try
                    {
                        var respuesta = await _api.ConsultarTicketObservadorAsync(new ConsultaTicketRequest
                        {
                            TicketCode = limpio
                        }, CancellationToken.None);
                        _consultaPrevia = respuesta;
                    }
                    catch (Exception)
                    {
                        _errorConsultaPrevia = PdaTexts.SinConexionServidor;
                    }
                }
            });

            if (codigo is null)
            {
                await this.AvisoAsync(
                    PdaTexts.ObservadorValidarTitulo,
                    PdaTexts.ObservadorCamaraNoDisponible,
                    PdaTexts.Cerrar);
                return;
            }

            var texto = codigo.Trim();
            if (texto.Length == 0)
            {
                return;
            }

            if (texto == ticketConsultado && (_consultaPrevia is not null || _errorConsultaPrevia is not null))
            {
                await AplicarConsultaPreviaAsync(texto);
            }
            else
            {
                await ConsultarAsync(texto);
            }
        }
        catch (Exception)
        {
            await this.AvisoAsync(
                PdaTexts.ObservadorValidarTitulo,
                PdaTexts.ObservadorCamaraNoDisponible,
                PdaTexts.Cerrar);
        }
        finally
        {
            _ocupado = false;
        }
    }

    private async Task AplicarConsultaPreviaAsync(string codigo)
    {
        _recibo.Children.Clear();
        if (!string.IsNullOrWhiteSpace(_errorConsultaPrevia))
        {
            await this.AvisoAsync(
                PdaTexts.ObservadorValidarTitulo,
                _errorConsultaPrevia,
                PdaTexts.Cerrar);
            return;
        }

        var consulta = _consultaPrevia;
        if (consulta is null || !consulta.IsSuccess || consulta.Data is null)
        {
            await this.AvisoAsync(
                PdaTexts.ObservadorValidarTitulo,
                string.IsNullOrWhiteSpace(consulta?.Message) ? PdaTexts.ObservadorQrNoLeido : consulta?.Message ?? PdaTexts.ObservadorQrNoLeido,
                PdaTexts.Cerrar);
            return;
        }

        _recibo.Children.Add(ReciboConsultaVista.Crear(TicketConsultaVista.De(consulta.Data)));
    }

    private async Task LeerFotoAsync()
    {
        if (_ocupado)
        {
            return;
        }

        FileResult? archivo;
        try
        {
            var permiso = await Permissions.RequestAsync<Permissions.Camera>();
            if (permiso != PermissionStatus.Granted)
            {
                await this.AvisoAsync(
                    PdaTexts.ObservadorValidarTitulo,
                    PdaTexts.ObservadorCamaraNoDisponible,
                    PdaTexts.Cerrar);
                return;
            }

            archivo = await MediaPicker.Default.CapturePhotoAsync();
        }
        catch (Exception)
        {
            await this.AvisoAsync(
                PdaTexts.ObservadorValidarTitulo,
                PdaTexts.ObservadorCamaraNoDisponible,
                PdaTexts.Cerrar);
            return;
        }

        await DecodificarArchivoAsync(archivo, subida: false);
    }

    private async Task LeerFotoSubidaAsync()
    {
        if (_ocupado)
        {
            return;
        }

        FileResult? archivo;
        try
        {
            await Permissions.RequestAsync<Permissions.Photos>();
            archivo = await MediaPicker.Default.PickPhotoAsync();
        }
        catch (Exception)
        {
            await this.AvisoAsync(
                PdaTexts.ObservadorValidarTitulo,
                PdaTexts.ObservadorQrNoLeido,
                PdaTexts.Cerrar);
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

        _ocupado = true;
        _cargando.Mostrar(PdaTexts.ObservadorLeyendoQr);
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
            _ocupado = false;
        }

        if (string.IsNullOrWhiteSpace(codigo))
        {
            await this.AvisoAsync(
                PdaTexts.ObservadorValidarTitulo,
                PdaTexts.ObservadorQrNoLeido,
                PdaTexts.Cerrar);
            return;
        }

        await ConsultarAsync(codigo);
    }

    private async Task ConsultarAsync(string codigo)
    {
        if (!ObservadorConsultaTicket.ListoParaConsultar(codigo))
        {
            await this.AvisoAsync(
                PdaTexts.ObservadorValidarTitulo,
                PdaTexts.ObservadorQrNoLeido,
                PdaTexts.Cerrar);
            return;
        }

        _cargando.Mostrar(PdaTexts.ObservadorConsultandoTicket);
        try
        {
            var consulta = await _api.ConsultarTicketObservadorAsync(new ConsultaTicketRequest
            {
                TicketCode = codigo
            }, CancellationToken.None);
            _recibo.Children.Clear();
            if (!consulta.IsSuccess || consulta.Data is null)
            {
                await this.AvisoAsync(
                    PdaTexts.ObservadorValidarTitulo,
                    string.IsNullOrWhiteSpace(consulta.Message) ? PdaTexts.ObservadorQrNoLeido : consulta.Message,
                    PdaTexts.Cerrar);
                return;
            }

            _recibo.Children.Add(ReciboConsultaVista.Crear(TicketConsultaVista.De(consulta.Data)));
        }
        catch (Exception)
        {
            await this.AvisoAsync(
                PdaTexts.ObservadorValidarTitulo,
                PdaTexts.SinConexionServidor,
                PdaTexts.Cerrar);
        }
        finally
        {
            _cargando.Ocultar();
        }
    }
}
