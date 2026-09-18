using System.Collections.ObjectModel;
using NewRich.Application.Contracts.Chat;
using NewRich.Maui.Data;
using NewRich.Maui.Services;
using NewRich.Maui.Views;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;

namespace NewRich.Maui.Views.Vendedor;

public sealed class SoporteTecnicoPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly SesionPda _sesion;
    private readonly ApiOpciones _opciones;
    private readonly ITokenStore _tokens;
    private readonly LocalDatabase _offline;
    private readonly SincronizacionOfflineServicio _sincronizacion;
    private readonly ChatEnVivoServicio _vivo;
    private readonly ObservableCollection<string> _lineas = [];
    private readonly CollectionView _lista;
    private Guid? _conversacionId;
    private readonly HashSet<Guid> _vistos = [];

    public SoporteTecnicoPage(
        NewRichApiClient api,
        SesionPda sesion,
        ApiOpciones opciones,
        ITokenStore tokens,
        LocalDatabase offline,
        SincronizacionOfflineServicio sincronizacion,
        ChatEnVivoServicio vivo)
    {
        _api = api;
        _sesion = sesion;
        _opciones = opciones;
        _tokens = tokens;
        _offline = offline;
        _sincronizacion = sincronizacion;
        _vivo = vivo;
        Title = PdaTexts.SoporteTecnico;
        BackgroundColor = Ui.Paper;

        _lista = new CollectionView
        {
            ItemsSource = _lineas,
            ItemTemplate = new DataTemplate(() =>
            {
                var texto = new Label
                {
                    FontSize = 14,
                    TextColor = Ui.Ink,
                    LineBreakMode = LineBreakMode.WordWrap,
                    Padding = new Thickness(12),
                    BackgroundColor = Colors.White
                };
                texto.SetBinding(Label.TextProperty, ".");
                return new Border
                {
                    Stroke = Ui.Line,
                    StrokeThickness = 1,
                    Padding = 0,
                    Margin = new Thickness(0, 0, 0, 8),
                    Content = texto
                };
            })
        };

        var ayuda = new Label
        {
            Text = PdaTexts.SoporteTecnicoAyuda,
            TextColor = Ui.Muted,
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 8)
        };
        var soloLectura = new Label
        {
            Text = PdaTexts.SoporteTecnicoAyuda,
            TextColor = Ui.Muted,
            FontSize = 12,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var raiz = new Grid
        {
            Padding = 16,
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };
        raiz.Add(ayuda, 0, 0);
        raiz.Add(_lista, 0, 1);
        raiz.Add(soloLectura, 0, 2);
        Content = raiz;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _vivo.Recibido -= EnVivo;
        _vivo.Recibido += EnVivo;
        var token = await _tokens.ObtenerAsync();
        if (!string.IsNullOrWhiteSpace(token))
        {
            await _vivo.AsegurarConectadoAsync(_opciones.BaseUrl, token, CancellationToken.None);
        }

        await CargarAsync();
    }

    protected override void OnDisappearing()
    {
        _vivo.Recibido -= EnVivo;
        base.OnDisappearing();
    }

    private async Task CargarAsync()
    {
        _lineas.Clear();
        _vistos.Clear();
        await _offline.AsegurarAsync();

        if (_sesion.Usuario is not null)
        {
            await _sincronizacion.SincronizarEnSilencioAsync(
                true,
                _sesion.Usuario.Rol,
                _sesion.Usuario.DebeCambiarPassword,
                CancellationToken.None);
        }

        var pendientes = await _offline.ReportesTecnicosPendientesAsync();
        foreach (var pendiente in pendientes)
        {
            _lineas.Add($"{pendiente.Estado}\nTicket: {pendiente.CodigoTicket}\n{pendiente.Observacion}");
        }

        try
        {
            var lista = await _api.ChatsTecnicosAsync(CancellationToken.None);
            if (!lista.IsSuccess || lista.Data is null || lista.Data.Count == 0)
            {
                if (_lineas.Count == 0)
                {
                    _lineas.Add(PdaTexts.SoporteTecnicoVacio);
                }

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
                AgregarMensaje(mensaje);
            }
        }
        catch (Exception)
        {
            if (_lineas.Count == 0)
            {
                _lineas.Add(PdaTexts.SinConexionServidor);
            }
        }
    }

    private void EnVivo(MensajeChatEnVivoResponse aviso)
    {
        if (_conversacionId is Guid id && aviso.ConversacionId == id)
        {
            AgregarMensaje(aviso.Mensaje);
        }
    }

    private void AgregarMensaje(MensajeResponse mensaje)
    {
        if (!_vistos.Add(mensaje.MensajeId))
        {
            return;
        }

        var texto = mensaje.Texto;
        if (!string.IsNullOrWhiteSpace(mensaje.NombreArchivo))
        {
            texto = string.IsNullOrWhiteSpace(texto)
                ? mensaje.NombreArchivo
                : $"{texto}\n[{mensaje.NombreArchivo}]";
        }

        _lineas.Add(texto);
    }
}
