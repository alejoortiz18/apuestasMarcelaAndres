using NewRich.Application.Contracts.Android;
using NewRich.Application.Contracts.Ventas;
using NewRich.Domain.Services;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;
using NewRich.Maui.Data;
using NewRich.Maui.Services;
using NewRich.Maui.Views;

namespace NewRich.Maui.Views.Vendedor;

public sealed class VendedorHomePage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly SesionPda _sesion;
    private readonly LocalDatabase _offline;
    private readonly SincronizacionOfflineServicio _sincronizacion;
    private readonly CodigosOfflineEnVivoServicio _enVivo;
    private readonly RecaudoEnVivoServicio _recaudoVivo;
    private readonly RecargaRecaudo _recarga = new();
    private readonly Label _total = new() { FontSize = 30, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink };
    private readonly Label _boletos = new() { FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink };
    private readonly Label _codigos = new() { Text = "0", FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink };
    private readonly Label _entregar = new() { FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink };
    private readonly Label _entregaDetalle = new() { FontSize = 12, TextColor = Ui.Muted, IsVisible = false };
    private readonly Label _atrasadoTitulo = new() { Text = PdaTexts.EntregaAlDia, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Ui.Green };
    private readonly Label _atrasado = new() { Text = "$0", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Ui.Green };
    private readonly Label _hoy = new() { Text = "$0", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink };
    private readonly Label _pagadoHoy = new() { FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Ui.Green, IsVisible = false };
    private readonly Frame _atrasadoCaja;
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
        RecaudoEnVivoServicio recaudoVivo,
        IServiceProvider services)
    {
        _api = api;
        _sesion = sesion;
        _offline = offline;
        _sincronizacion = sincronizacion;
        _enVivo = enVivo;
        _recaudoVivo = recaudoVivo;
        _recaudoVivo.CambioRecibido += OnRecaudoVivo;
        _services = services;
        Title = PdaTexts.Inicio;
        _juegoNuevo = Ui.Primario("+ " + PdaTexts.BotonJuegoNuevo);
        _juegoNuevo.Clicked += (_, _) => _ = Shell.Current.GoToAsync("//vender");
        _atrasadoCaja = new Frame
        {
            BackgroundColor = Ui.Mint,
            BorderColor = Ui.Green,
            CornerRadius = 10,
            Padding = new Thickness(12, 10),
            HasShadow = false,
            Content = FilaEntrega(_atrasadoTitulo, _atrasado)
        };

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
                                Ui.Titulo(string.IsNullOrWhiteSpace(_sesion.Usuario?.Alias) ? PdaTexts.Inicio : _sesion.Usuario!.Alias),
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
                    new Frame
                    {
                        BackgroundColor = Ui.Crema,
                        BorderColor = Ui.Gold,
                        CornerRadius = 15,
                        Padding = 18,
                        Content = new VerticalStackLayout
                        {
                            Spacing = 8,
                            Children =
                            {
                                new Label { Text = PdaTexts.DebeEntregarRecaudador, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Ui.Ink },
                                _atrasadoCaja,
                                new Frame
                                {
                                    BackgroundColor = Colors.White,
                                    BorderColor = Ui.Line,
                                    CornerRadius = 10,
                                    Padding = new Thickness(12, 10),
                                    HasShadow = false,
                                    Content = new VerticalStackLayout
                                    {
                                        Spacing = 2,
                                        Children =
                                        {
                                            FilaEntrega(new Label { Text = PdaTexts.EntregaDeHoy, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink }, _hoy),
                                            _entregaDetalle
                                        }
                                    }
                                },
                                new BoxView { HeightRequest = 1, Color = Ui.Gold, Margin = new Thickness(0, 2) },
                                FilaEntrega(new Label { Text = PdaTexts.EntregaTotal, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink }, _entregar),
                                _pagadoHoy
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
                    Menu(PdaTexts.ResultadosTitulo, PdaTexts.ResultadosAyuda, async () => await Navigation.PushAsync(_services.GetRequiredService<ResultadosPage>())),
                    Menu(PdaTexts.ValidarTicket, PdaTexts.ValidarTicketAyuda, async () => await Navigation.PushAsync(_services.GetRequiredService<ValidarTicketPage>())),
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

    private void OnRecaudoVivo() => _ = ActualizarAsync();

    private async Task ActualizarAsync()
    {
        if (!_recarga.Intentar())
        {
            return;
        }

        var serie = _recarga.Nueva();
        try
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
            await _recaudoVivo.AsegurarSesionAsync(CancellationToken.None);
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
        if (!_recarga.Vigente(serie))
        {
            return;
        }

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

        var hoy = ZonaHorariaColombia.ALocal(DateTime.UtcNow).Date;
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

        var entrega = conectado ? await _api.MiEntregaRecaudoAsync(CancellationToken.None) : null;
        var datos = entrega is { IsSuccess: true } ? entrega.Data : null;
        var atrasado = EntregaVendedorVista.Atrasado(datos);
        var conAtraso = atrasado > 0m;
        var alDia = EntregaVendedorVista.EstaAlDia(datos);
        _atrasadoTitulo.Text = EntregaVendedorVista.TituloAtrasado(datos);
        _atrasadoTitulo.TextColor = conAtraso ? Ui.Danger : alDia ? Ui.Green : Ui.Ink;
        _atrasado.Text = FormatoDinero.Pesos(atrasado);
        _atrasado.TextColor = conAtraso ? Ui.Danger : alDia ? Ui.Green : Ui.Ink;
        _atrasadoCaja.BackgroundColor = conAtraso ? Ui.DangerBg : alDia ? Ui.Mint : Colors.White;
        _atrasadoCaja.BorderColor = conAtraso ? Ui.Danger : alDia ? Ui.Green : Ui.Line;
        _hoy.Text = FormatoDinero.Pesos(EntregaVendedorVista.Hoy(datos));
        _entregar.Text = FormatoDinero.Pesos(EntregaVendedorVista.Total(datos));
        _entregaDetalle.Text = EntregaVendedorVista.Detalle(datos);
        _entregaDetalle.IsVisible = !string.IsNullOrEmpty(_entregaDetalle.Text);
        _pagadoHoy.Text = EntregaVendedorVista.PagadoHoy(datos);
        _pagadoHoy.IsVisible = !string.IsNullOrEmpty(_pagadoHoy.Text);
        }
        finally
        {
            if (_recarga.Terminar())
            {
                _ = ActualizarAsync();
            }
        }
    }

    private static Grid FilaEntrega(Label titulo, Label valor)
    {
        titulo.VerticalOptions = LayoutOptions.Center;
        valor.HorizontalOptions = LayoutOptions.End;
        valor.VerticalOptions = LayoutOptions.Center;
        var fila = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 8
        };
        fila.Add(titulo, 0, 0);
        fila.Add(valor, 1, 0);
        return fila;
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
