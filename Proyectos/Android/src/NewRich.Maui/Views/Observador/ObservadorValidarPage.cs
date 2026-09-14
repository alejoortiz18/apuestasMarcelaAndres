using NewRich.Application.Contracts.Boletos;
using NewRich.Maui.Services;
using NewRich.Maui.Views;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;

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

    private async Task LeerCamaraAsync()
    {
        if (_ocupado)
        {
            return;
        }

        _ocupado = true;
        try
        {
            var codigo = await _camara.EscanearAsync();
            if (codigo is null)
            {
                await this.AvisoAsync(
                    PdaTexts.ObservadorValidarTitulo,
                    PdaTexts.ObservadorCamaraNoDisponible,
                    PdaTexts.Cerrar);
                return;
            }

            if (codigo.Trim().Length == 0)
            {
                return;
            }

            await ConsultarAsync(codigo.Trim());
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
