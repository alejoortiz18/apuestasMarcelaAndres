using NewRich.Application.Contracts.Android;
using NewRich.Application.Contracts.Ventas;
using NewRich.Domain.Services;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;
using NewRich.Maui.Data;
using NewRich.Maui.Services;

namespace NewRich.Maui.Views.Vendedor;

public sealed class VendedorHomePage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly SesionPda _sesion;
    private readonly LocalDatabase _offline;
    private readonly SincronizacionOfflineServicio _sincronizacion;
    private readonly CodigosOfflineEnVivoServicio _enVivo;
    private readonly Label _total = new() { FontSize = 30, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink };
    private readonly Label _boletos = new() { FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink };
    private readonly Label _codigos = new() { Text = "0", FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink };
    private readonly Label _estado = new() { FontSize = 11, TextColor = Color.FromArgb("#bce9cc") };
    private readonly VerticalStackLayout _banners = new() { Spacing = 8 };
    private readonly IServiceProvider _services;
    private readonly Button _juegoNuevo;

    public VendedorHomePage(
        NewRichApiClient api,
        SesionPda sesion,
        LocalDatabase offline,
        SincronizacionOfflineServicio sincronizacion,
        CodigosOfflineEnVivoServicio enVivo,
        IServiceProvider services)
    {
        _api = api;
        _sesion = sesion;
        _offline = offline;
        _sincronizacion = sincronizacion;
        _enVivo = enVivo;
        _services = services;
        Title = PdaTexts.Inicio;
        _juegoNuevo = Ui.Primario("+ " + PdaTexts.BotonJuegoNuevo);
        _juegoNuevo.Clicked += (_, _) => _ = Shell.Current.GoToAsync("//vender");

        Content = new ScrollView
        {
            BackgroundColor = Ui.Paper,
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 14,
                Children =
                {
                    new Frame
                    {
                        BackgroundColor = Ui.Dark,
                        BorderColor = Ui.Dark,
                        CornerRadius = 0,
                        Padding = new Thickness(18, 18, 18, 22),
                        Content = new VerticalStackLayout
                        {
                            Children =
                            {
                                Ui.Titulo(_sesion.Usuario?.NombreCompleto ?? PdaTexts.Inicio),
                                new Label
                                {
                                    Text = string.IsNullOrWhiteSpace(_sesion.Usuario?.GrupoNombre) ? PdaTexts.GrupoNoConsultado : _sesion.Usuario!.GrupoNombre,
                                    TextColor = Color.FromArgb("#aed8c4"),
                                    FontSize = 13
                                },
                                _estado
                            }
                        }
                    },
                    _banners,
                    new Frame
                    {
                        BackgroundColor = Ui.Gold,
                        BorderColor = Ui.Gold,
                        CornerRadius = 15,
                        Padding = 20,
                        Content = new VerticalStackLayout
                        {
                            Spacing = 8,
                            Children =
                            {
                                new Label { Text = PdaTexts.VentasDelDia, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Ui.Ink },
                                _total,
                                _juegoNuevo
                            }
                        }
                    },
                    new Label { Text = PdaTexts.ResumenTurno, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink },
                    new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
                        RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Auto) },
                        ColumnSpacing = 10,
                        RowSpacing = 10,
                        Children =
                        {
                            Mini(PdaTexts.BoletosHoy, _boletos, 0, 0),
                            Mini(PdaTexts.CodigosOffline, _codigos, 1, 0),
                            Mini(PdaTexts.PdaAsociado, new Label { Text = _sesion.CodigoDispositivo, FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink }, 0, 1),
                            Mini(PdaTexts.Grupo, new Label { Text = string.IsNullOrWhiteSpace(_sesion.Usuario?.GrupoNombre) ? PdaTexts.GrupoNoConsultado : _sesion.Usuario!.GrupoNombre, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink }, 1, 1)
                        }
                    },
                    new Label { Text = PdaTexts.AccesosRapidos, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink },
                    Menu(PdaTexts.NumerosBloqueados, PdaTexts.NumerosBloqueadosAyuda, async () => await Navigation.PushAsync(_services.GetRequiredService<NumerosBloqueadosPage>())),
                    Menu(PdaTexts.ResultadosTitulo, PdaTexts.ResultadosAyuda, async () => await Navigation.PushAsync(_services.GetRequiredService<ResultadosPage>())),
                    Menu(PdaTexts.ValidarTicket, PdaTexts.ValidarTicketAyuda, async () => await Navigation.PushAsync(_services.GetRequiredService<ValidarTicketPage>()))
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await ActualizarAsync();
        }
        catch (Exception)
        {
        }
    }

    private async Task ActualizarAsync()
    {
        var ping = await _api.ConectarAsync(
            PdaConexion.UrlsPara(DeviceInfo.Current.DeviceType == DeviceType.Virtual),
            CancellationToken.None);
        var conectado = ping.IsSuccess;
        var rol = _sesion.Usuario?.Rol ?? default;
        await _sincronizacion.SincronizarEnSilencioAsync(conectado, rol, false, CancellationToken.None);
        if (conectado)
        {
            await _enVivo.AsegurarSesionAsync(CancellationToken.None);
            var operativa = await _api.OperativaAsync(CancellationToken.None);
            if (operativa.IsSuccess && operativa.Data is not null)
            {
                _sesion.Limites = operativa.Data;
                await _offline.GuardarNumerosRestringidosAsync(operativa.Data.NumerosRestringidos);
                await _offline.GuardarSesionAsync(new SesionLocal
                {
                    Usuario = _sesion.Usuario ?? new(),
                    Limites = operativa.Data,
                    CodigoDispositivo = _sesion.CodigoDispositivo
                });
            }

            var loterias = await _api.LoteriasAsync(CancellationToken.None);
            if (loterias.IsSuccess && loterias.Data is not null)
            {
                await _offline.GuardarLoteriasAsync(loterias.Data);
            }
        }
        var offline = await _offline.ContarDisponiblesAsync();
        _sesion.HorarioCerrado = HorarioPda.EstaFuera(
            _sesion.Limites,
            ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        _estado.Text = Ui.EstadoLinea(_sesion.CodigoDispositivo, conectado, _sesion.HorarioCerrado, offline);
        _codigos.Text = offline.ToString();
        _juegoNuevo.IsEnabled = HorarioPda.PuedeIniciarJuegoNuevo(_sesion.HorarioCerrado)
            && (conectado || offline > 0);
        _banners.Children.Clear();
        if (!conectado)
        {
            var critico = offline <= 0;
            _banners.Children.Add(Ui.Banner(
                PdaTexts.AvisoOperacionSinServidor(offline),
                critico ? Ui.DangerBg : Ui.WarnBg,
                critico ? Ui.Danger : Ui.Warn));
        }
        if (_sesion.HorarioCerrado)
        {
            _banners.Children.Add(Ui.Banner($"{PdaTexts.JuegosCerrados} {PdaTexts.JuegosCerradosDetalle}", Ui.DangerBg, Ui.Danger));
        }

        var hoy = DateTime.Today;
        var ventas = await _api.VentasAsync(new ConsultaVentasRequest
        {
            FechaInicial = hoy,
            FechaFinal = hoy.AddDays(1).AddTicks(-1)
        }, CancellationToken.None);

        if (ventas.IsSuccess && ventas.Data is not null)
        {
            _total.Text = FormatoDinero.Pesos(ventas.Data.Sum(v => v.Total));
            _boletos.Text = ventas.Data.Count.ToString();
        }
        else
        {
            _total.Text = FormatoDinero.Pesos(0);
            _boletos.Text = "0";
        }
    }

    private static Frame Mini(string titulo, View valor, int col, int row)
    {
        var frame = new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = Ui.Line,
            CornerRadius = 12,
            Padding = 14,
            Content = new VerticalStackLayout
            {
                Children =
                {
                    new Label { Text = titulo, FontSize = 11, TextColor = Ui.Muted },
                    valor
                }
            }
        };
        Grid.SetColumn(frame, col);
        Grid.SetRow(frame, row);
        return frame;
    }

    private static Button Menu(string titulo, string ayuda, Func<Task> accion)
    {
        var boton = new Button
        {
            Text = $"{titulo}\n{ayuda}",
            BackgroundColor = Colors.White,
            TextColor = Ui.Ink,
            BorderColor = Ui.Line,
            BorderWidth = 1,
            CornerRadius = 12,
            HeightRequest = 64
        };
        boton.Clicked += async (_, _) => await accion();
        return boton;
    }
}
