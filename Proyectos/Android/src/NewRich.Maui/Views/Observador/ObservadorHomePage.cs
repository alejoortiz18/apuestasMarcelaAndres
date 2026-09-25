using NewRich.Application.Contracts.Ventas;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;
using NewRich.Maui.Services;
using NewRich.Maui.Views;
using NewRich.Maui.Views.Vendedor;

namespace NewRich.Maui.Views.Observador;

public sealed class ObservadorHomePage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly SesionPda _sesion;
    private readonly IServiceProvider _services;
    private readonly LoteriasEnVivoServicio _loteriasVivo;
    private readonly Label _total = new() { FontSize = 30, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink };
    private readonly Label _boletos = new() { FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink };
    private readonly Label _numeros = new() { FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink };
    private readonly Label _casosPremios = new() { FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink };
    private readonly Label _estado = new() { FontSize = 11, TextColor = Color.FromArgb("#bce9cc") };
    private readonly VerticalStackLayout _banners = new() { Spacing = 8 };
    private readonly Button _validar;

    public ObservadorHomePage(
        NewRichApiClient api,
        SesionPda sesion,
        IServiceProvider services,
        LoteriasEnVivoServicio loteriasVivo)
    {
        _api = api;
        _sesion = sesion;
        _services = services;
        _loteriasVivo = loteriasVivo;
        Title = PdaTexts.Inicio;
        _validar = Ui.Primario(PdaTexts.ValidarTicket);
        _validar.Clicked += (_, _) => _ = Shell.Current.GoToAsync("//ovalidar");

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
                                    Text = PdaTexts.PdaObservador,
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
                                _validar
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
                            Mini(PdaTexts.NumerosHoy, _numeros, 1, 0),
                            MiniTocable(PdaTexts.CasosPremios, _casosPremios, 0, 1, async () =>
                                await Navigation.PushAsync(_services.GetRequiredService<CasosPage>())),
                            Mini(PdaTexts.RolObservador, new Label { Text = PdaTexts.ResultadosSoloLectura, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink }, 1, 1)
                        }
                    },
                    new Label { Text = PdaTexts.AccesosRapidos, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink },
                    Menu(PdaTexts.ResultadosTitulo, PdaTexts.ResultadosAyuda, async () => await Navigation.PushAsync(_services.GetRequiredService<ResultadosPage>())),
                    Menu(PdaTexts.ValidarTicket, PdaTexts.ValidarTicketAyuda, async () => await Shell.Current.GoToAsync("//ovalidar")),
                    Menu(PdaTexts.CasosPremios, PdaTexts.CasosPremiosAyuda, async () =>
                        await Navigation.PushAsync(_services.GetRequiredService<CasosPage>())),
                    Menu(PdaTexts.Consultas, PdaTexts.ConsultasAyuda, async () => await Shell.Current.GoToAsync("//consultas")),
                    Menu(PdaTexts.Kpi, PdaTexts.KpiAyuda, async () => await Navigation.PushAsync(_services.GetRequiredService<KpiPage>()))
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _loteriasVivo.AsegurarSesionAsync(CancellationToken.None);
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
        _estado.Text = $"PDA {_sesion.CodigoDispositivo} · {(conectado ? PdaTexts.Conectado : PdaTexts.SinConexion)} · {PdaTexts.HorarioAbierto}";
        _banners.Children.Clear();
        if (!conectado)
        {
            _banners.Children.Add(Ui.Banner(PdaTexts.SinConexionServidor, Ui.WarnBg, Ui.Warn));
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

        var resultados = await _api.ResultadosAsync(DateOnly.FromDateTime(hoy), null, CancellationToken.None);
        _numeros.Text = resultados.IsSuccess && resultados.Data is not null
            ? resultados.Data.Count.ToString()
            : "0";

        var casos = await _api.PremiosAsignadosAsync(CancellationToken.None);
        _casosPremios.Text = casos.IsSuccess && casos.Data is not null
            ? casos.Data.Count.ToString()
            : "0";
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

    private static Frame MiniTocable(string titulo, View valor, int col, int row, Func<Task> accion)
    {
        var frame = Mini(titulo, valor, col, row);
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await accion();
        frame.GestureRecognizers.Add(tap);
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
