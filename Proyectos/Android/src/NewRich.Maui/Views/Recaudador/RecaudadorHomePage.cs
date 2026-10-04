using NewRich.Application.Contracts.Recaudo;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;
using NewRich.Maui.Data;
using NewRich.Maui.Views;

namespace NewRich.Maui.Views.Recaudador;

public sealed class RecaudadorHomePage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly SesionPda _sesion;
    private readonly LocalDatabase _local;
    private readonly IServiceProvider _services;
    private readonly Label _estado = new() { FontSize = 11, TextColor = Color.FromArgb("#bce9cc") };
    private readonly Label _fecha = new()
    {
        Text = RecaudoListas.FechaDelDia(DateTime.UtcNow),
        FontAttributes = FontAttributes.Bold,
        FontSize = 14,
        TextColor = Ui.Ink,
        HorizontalTextAlignment = TextAlignment.Center
    };
    private readonly Label _total = new()
    {
        FontSize = 22,
        FontAttributes = FontAttributes.Bold,
        TextColor = Ui.Ink,
        Text = RecaudoPagoVista.Miles(0),
        HorizontalTextAlignment = TextAlignment.Center
    };
    private readonly Label _recaudado = new()
    {
        FontSize = 22,
        FontAttributes = FontAttributes.Bold,
        TextColor = Ui.Ink,
        Text = RecaudoPagoVista.Miles(0),
        HorizontalTextAlignment = TextAlignment.Center
    };
    private readonly Label _grupos = new() { FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink, Text = "0" };
    private readonly Label _vendedores = new() { FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink, Text = "0" };
    private readonly Button _recaudar;

    public RecaudadorHomePage(NewRichApiClient api, SesionPda sesion, LocalDatabase local, IServiceProvider services)
    {
        _api = api;
        _sesion = sesion;
        _local = local;
        _services = services;
        Title = PdaTexts.Inicio;
        _recaudar = Ui.Primario(PdaTexts.Recaudar);
        _recaudar.Clicked += async (_, _) =>
            await Navigation.PushAsync(_services.GetRequiredService<RecaudadorCobroPage>());

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
                                new Label { Text = PdaTexts.PdaRecaudador, TextColor = Color.FromArgb("#aed8c4"), FontSize = 13 },
                                _estado
                            }
                        }
                    },
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
                                _fecha,
                                new Grid
                                {
                                    ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
                                    ColumnSpacing = 12,
                                    Children =
                                    {
                                        Columna(PdaTexts.TotalRecaudar, _total, 0),
                                        Columna(PdaTexts.TotalRecaudado, _recaudado, 1)
                                    }
                                },
                                _recaudar
                            }
                        }
                    },
                    new Label { Text = PdaTexts.ResumenTurno, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink },
                    new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
                        ColumnSpacing = 10,
                        Children =
                        {
                            Mini(PdaTexts.GruposAsignados, _grupos, 0),
                            Mini(PdaTexts.VendedoresAsignados, _vendedores, 1)
                        }
                    },
                    new Label { Text = PdaTexts.AccesosRapidos, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink },
                    Menu(PdaTexts.Recaudar, PdaTexts.RecaudarAyuda, async () =>
                        await Navigation.PushAsync(_services.GetRequiredService<RecaudadorCobroPage>())),
                    Menu(PdaTexts.HistorialRecaudo, PdaTexts.HistorialRecaudoAyuda, async () =>
                        await Shell.Current.GoToAsync("//rhistorial")),
                    Menu(PdaTexts.MetricasRecaudo, PdaTexts.MetricasRecaudoAyuda, async () =>
                        await Shell.Current.GoToAsync("//rmetricas")),
                    VersionInstaladaPie.Crear()
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
        _estado.Text = $"PDA {_sesion.CodigoDispositivo} · {(conectado ? PdaTexts.Conectado : PdaTexts.SinConexion)}";

        IReadOnlyList<ObligacionRecaudoResponse> filas;
        if (conectado)
        {
            var remoto = await _api.ObligacionesRecaudoAsync(null, CancellationToken.None);
            if (remoto.IsSuccess && remoto.Data is not null)
            {
                filas = RecaudoListas.ConPendientes(remoto.Data, await _local.PagosRecaudoPendientesAsync(), RecaudoListas.HoyEnColombia());
                await _local.GuardarObligacionesRecaudoAsync(filas);
            }
            else
            {
                filas = await _local.ObligacionesRecaudoLocalAsync();
            }
        }
        else
        {
            filas = await _local.ObligacionesRecaudoLocalAsync();
        }

        var resumen = RecaudoListas.Resumen(filas);
        _fecha.Text = RecaudoListas.FechaDelDia(DateTime.UtcNow);
        _total.Text = RecaudoPagoVista.Miles(resumen.TotalPorRecaudar);
        _recaudado.Text = RecaudoPagoVista.Miles(resumen.TotalRecaudado);
        _grupos.Text = resumen.Grupos.ToString();
        _vendedores.Text = resumen.Vendedores.ToString();
    }

    private static VerticalStackLayout Columna(string titulo, View valor, int col)
    {
        var columna = new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label
                {
                    Text = titulo,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 12,
                    TextColor = Ui.Ink,
                    HorizontalTextAlignment = TextAlignment.Center
                },
                valor
            }
        };
        Grid.SetColumn(columna, col);
        return columna;
    }

    private static Frame Mini(string titulo, View valor, int col)
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
