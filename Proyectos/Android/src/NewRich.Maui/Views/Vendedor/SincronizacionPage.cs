using Microsoft.Maui.Controls.Shapes;
using NewRich.Domain.Enums;
using NewRich.Maui.Data;
using NewRich.Maui.Services;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;

namespace NewRich.Maui.Views.Vendedor;

public sealed class SincronizacionPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly LocalDatabase _offline;
    private readonly SincronizacionOfflineServicio _sincronizacion;
    private readonly SesionPda _sesion;
    private readonly Border _estadoPastilla;
    private readonly Label _estadoTexto;
    private readonly Label _ventasValor;
    private readonly Label _codigosValor;
    private readonly Label _porcentaje;
    private readonly ProgressBar _barra;
    private readonly Label _estadoVentas;
    private readonly Label _estadoReposicion;
    private readonly Label _resultado;
    private readonly Button _boton;
    private readonly ActivityIndicator _spinner;
    private bool _trabajando;
    private bool _autoIntentado;

    public SincronizacionPage(
        NewRichApiClient api,
        LocalDatabase offline,
        SincronizacionOfflineServicio sincronizacion,
        SesionPda sesion)
    {
        _api = api;
        _offline = offline;
        _sincronizacion = sincronizacion;
        _sesion = sesion;
        Title = PdaTexts.SincronizacionTitulo;
        BackgroundColor = Ui.Paper;

        _estadoTexto = new Label
        {
            Text = PdaTexts.SyncPendiente,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Ui.Warn,
            VerticalTextAlignment = TextAlignment.Center
        };
        _spinner = new ActivityIndicator { Color = Ui.Info, IsVisible = false, IsRunning = false, HeightRequest = 16, WidthRequest = 16 };
        _estadoPastilla = new Border
        {
            BackgroundColor = Ui.WarnBg,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 999 },
            Padding = new Thickness(12, 7),
            HorizontalOptions = LayoutOptions.Start,
            Content = new HorizontalStackLayout
            {
                Spacing = 8,
                Children = { _spinner, _estadoTexto }
            }
        };

        _ventasValor = Metrica();
        _codigosValor = Metrica();
        _porcentaje = new Label
        {
            Text = string.Format(PdaTexts.SyncProgresoFormato, 0),
            FontSize = 26,
            FontAttributes = FontAttributes.Bold,
            TextColor = Ui.Dark
        };
        _barra = new ProgressBar { Progress = 0, ProgressColor = Ui.Gold, HeightRequest = 8 };
        _estadoVentas = Valor(PdaTexts.SyncEsperando);
        _estadoReposicion = Valor(PdaTexts.SyncEsperando);
        _resultado = new Label
        {
            Text = PdaTexts.SyncEsperando,
            FontSize = 13,
            TextColor = Ui.Ink,
            LineBreakMode = LineBreakMode.WordWrap
        };

        _boton = Ui.Primario(PdaTexts.SyncSincronizarTodo);
        _boton.Clicked += async (_, _) => await SincronizarAsync(manual: true);

        Content = new ScrollView
        {
            BackgroundColor = Ui.Paper,
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 18, 16, 28),
                Spacing = 14,
                Children =
                {
                    Encabezado(),
                    _estadoPastilla,
                    Contadores(),
                    Progreso(),
                    Detalle(),
                    _boton,
                    new Label
                    {
                        Text = PdaTexts.SyncAyudaAutomatica,
                        FontSize = 11,
                        TextColor = Ui.Muted,
                        HorizontalTextAlignment = TextAlignment.Center
                    }
                }
            }
        };
    }

    private static View Encabezado() => new VerticalStackLayout
    {
        Spacing = 4,
        Children =
        {
            Ui.Marca(PdaTexts.SincronizacionTitulo.ToUpperInvariant()),
            new Label
            {
                Text = PdaTexts.SincronizacionSubtitulo,
                FontSize = 13,
                TextColor = Ui.Muted
            }
        }
    };

    private static Label Metrica() => new()
    {
        Text = "0",
        FontSize = 30,
        FontAttributes = FontAttributes.Bold,
        TextColor = Ui.Dark
    };

    private static Label Valor(string texto) => new()
    {
        Text = texto,
        FontSize = 13,
        FontAttributes = FontAttributes.Bold,
        TextColor = Ui.Ink,
        HorizontalTextAlignment = TextAlignment.End
    };

    private static Label Etiqueta(string texto) => new()
    {
        Text = texto,
        FontSize = 11,
        TextColor = Ui.Muted,
        CharacterSpacing = 0.5
    };

    private static Border Tarjeta(View contenido) => new()
    {
        BackgroundColor = Colors.White,
        Stroke = Ui.Line,
        StrokeThickness = 1,
        StrokeShape = new RoundRectangle { CornerRadius = 16 },
        Padding = 16,
        Content = contenido
    };

    private View Contadores()
    {
        var grid = new Grid { ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        var ventas = Tarjeta(new VerticalStackLayout
        {
            Spacing = 2,
            Children = { _ventasValor, Etiqueta(PdaTexts.SyncVentasPendientesTitulo) }
        });
        var codigos = Tarjeta(new VerticalStackLayout
        {
            Spacing = 2,
            Children = { _codigosValor, Etiqueta(PdaTexts.SyncCodigosPendientesTitulo) }
        });

        Grid.SetColumn(ventas, 0);
        Grid.SetColumn(codigos, 1);
        grid.Add(ventas);
        grid.Add(codigos);
        return grid;
    }

    private View Progreso()
    {
        var fila = new Grid();
        fila.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        fila.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var titulo = Etiqueta(PdaTexts.SyncProgresoTitulo);
        titulo.VerticalTextAlignment = TextAlignment.Center;
        Grid.SetColumn(titulo, 0);
        Grid.SetColumn(_porcentaje, 1);
        fila.Add(titulo);
        fila.Add(_porcentaje);

        return Tarjeta(new VerticalStackLayout
        {
            Spacing = 10,
            Children = { fila, _barra }
        });
    }

    private View Detalle() => Tarjeta(new VerticalStackLayout
    {
        Spacing = 12,
        Children =
        {
            Renglon(PdaTexts.SyncEstadoVentas, _estadoVentas),
            Separador(),
            Renglon(PdaTexts.SyncEstadoReposicion, _estadoReposicion),
            Separador(),
            new VerticalStackLayout
            {
                Spacing = 4,
                Children = { Etiqueta(PdaTexts.SyncResultadoUltimo), _resultado }
            }
        }
    });

    private static View Renglon(string etiqueta, View valor)
    {
        var grid = new Grid { ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var texto = Etiqueta(etiqueta);
        texto.VerticalTextAlignment = TextAlignment.Center;
        Grid.SetColumn(texto, 0);
        Grid.SetColumn(valor, 1);
        grid.Add(texto);
        grid.Add(valor);
        return grid;
    }

    private static View Separador() => new BoxView
    {
        HeightRequest = 1,
        Color = Ui.Line,
        HorizontalOptions = LayoutOptions.Fill
    };

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Connectivity.ConnectivityChanged -= OnConnectivityChanged;
        Connectivity.ConnectivityChanged += OnConnectivityChanged;
        await RefrescarContadoresAsync();
        if (!_autoIntentado)
        {
            _autoIntentado = true;
            await SincronizarAsync(manual: false);
        }
    }

    protected override void OnDisappearing()
    {
        Connectivity.ConnectivityChanged -= OnConnectivityChanged;
        base.OnDisappearing();
    }

    private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (_trabajando || e.NetworkAccess != NetworkAccess.Internet)
        {
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(() => SincronizarAsync(manual: false));
    }

    private async Task RefrescarContadoresAsync(bool pintarEstado = true)
    {
        await _offline.AsegurarAsync();
        var ventas = await _offline.ContarVentasPendientesCantidadAsync();
        var codigos = await _offline.ObtenerGastadosPendientesAsync();
        _ventasValor.Text = ventas.ToString();
        _codigosValor.Text = codigos.ToString();
        if (!pintarEstado)
        {
            return;
        }

        var pendiente = ventas > 0 || codigos > 0;
        PintarEstado(pendiente ? PdaTexts.SyncPendiente : PdaTexts.SyncTodoAlDia, false);
        _estadoVentas.Text = ventas > 0 ? PdaTexts.SyncPendiente : PdaTexts.SyncNadaPendiente;
        _estadoReposicion.Text = codigos > 0 ? PdaTexts.SyncPendiente : PdaTexts.SyncNadaPendiente;
    }

    private async Task SincronizarAsync(bool manual)
    {
        if (_trabajando)
        {
            return;
        }

        _trabajando = true;
        _boton.IsEnabled = false;
        try
        {
            PintarEstado(PdaTexts.SyncSincronizando, true);
            var ping = await _api.ConectarAsync(
                PdaConexion.UrlsPara(DeviceInfo.Current.DeviceType == DeviceType.Virtual),
                CancellationToken.None);
            var conectado = ping.IsSuccess;
            if (!conectado && !manual)
            {
                PintarEstado(PdaTexts.SyncSinConexion, false);
                _resultado.Text = PdaTexts.SyncSinConexion;
                return;
            }

            var rol = _sesion.Usuario?.Rol ?? RolUsuario.Vendedor;
            var debe = _sesion.Usuario?.DebeCambiarPassword ?? false;
            var progreso = new Progress<AvanceSincronizacion>(a =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PintarEstado(a.Estado, true);
                    _porcentaje.Text = string.Format(PdaTexts.SyncProgresoFormato, a.Porcentaje);
                    _barra.ProgressTo(a.Porcentaje / 100.0, 220, Easing.CubicOut);
                    _ventasValor.Text = a.VentasPendientes.ToString();
                    _codigosValor.Text = a.CodigosPendientes.ToString();
                    ActualizarEstadosParciales(a.Estado, a.VentasPendientes, a.CodigosPendientes);
                });
            });

            var resultado = await _sincronizacion.SincronizarTodoAsync(conectado, rol, debe, progreso, CancellationToken.None);
            await RefrescarContadoresAsync(pintarEstado: false);

            PintarEstado(resultado.Exito ? PdaTexts.SyncCompletado : resultado.Mensaje, false);
            _resultado.Text = resultado.Mensaje;
            _estadoVentas.Text = resultado.VentasPendientes > 0
                ? PdaTexts.SyncPendiente
                : resultado.VentasSincronizadas > 0 ? PdaTexts.SyncCompletado : PdaTexts.SyncNadaPendiente;
            _estadoReposicion.Text = resultado.CodigosPendientes > 0
                ? resultado.Mensaje.Contains(PdaTexts.SyncReposicionYaRealizada, StringComparison.OrdinalIgnoreCase)
                    ? PdaTexts.SyncReposicionYaRealizada
                    : PdaTexts.SyncPendiente
                : resultado.CodigosRepuestos > 0 ? PdaTexts.SyncCompletado : PdaTexts.SyncNadaPendiente;
            if (resultado.Exito)
            {
                await _barra.ProgressTo(1, 220, Easing.CubicOut);
                _porcentaje.Text = string.Format(PdaTexts.SyncProgresoFormato, 100);
            }
        }
        finally
        {
            _trabajando = false;
            _boton.IsEnabled = true;
            _spinner.IsVisible = false;
            _spinner.IsRunning = false;
        }
    }

    private void PintarEstado(string estado, bool enProceso)
    {
        var (fondo, letra) = Paleta(estado, enProceso);
        _estadoPastilla.BackgroundColor = fondo;
        _estadoTexto.Text = estado;
        _estadoTexto.TextColor = letra;
        _spinner.Color = letra;
        _spinner.IsVisible = enProceso;
        _spinner.IsRunning = enProceso;
    }

    private static (Color Fondo, Color Letra) Paleta(string estado, bool enProceso)
    {
        if (enProceso)
        {
            return (Ui.InfoBg, Ui.Info);
        }

        if (Coincide(estado, PdaTexts.SyncError) || Coincide(estado, PdaTexts.SyncSinConexion))
        {
            return (Ui.DangerBg, Ui.Danger);
        }

        if (Coincide(estado, PdaTexts.SyncReposicionYaRealizada))
        {
            return (Ui.WarnBg, Ui.Warn);
        }

        if (Coincide(estado, PdaTexts.SyncCompletado)
            || Coincide(estado, PdaTexts.SyncTodoAlDia)
            || Coincide(estado, PdaTexts.SyncNadaPendiente))
        {
            return (Ui.Mint, Ui.Green);
        }

        return (Ui.WarnBg, Ui.Warn);
    }

    private static bool Coincide(string estado, string esperado) =>
        estado.Contains(esperado, StringComparison.OrdinalIgnoreCase);

    private void ActualizarEstadosParciales(string estado, int ventasPendientes, int codigosPendientes)
    {
        if (Coincide(estado, PdaTexts.SyncValidando) || Coincide(estado, PdaTexts.SyncSincronizando))
        {
            _estadoVentas.Text = estado;
        }
        else if (ventasPendientes == 0)
        {
            _estadoVentas.Text = PdaTexts.SyncCompletado;
        }

        if (Coincide(estado, PdaTexts.SyncReponiendo)
            || Coincide(estado, PdaTexts.SyncActualizandoConfig)
            || Coincide(estado, PdaTexts.SyncReposicionYaRealizada))
        {
            _estadoReposicion.Text = estado;
        }
        else if (codigosPendientes == 0)
        {
            _estadoReposicion.Text = PdaTexts.SyncNadaPendiente;
        }
    }
}
