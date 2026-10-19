using System.Globalization;
using NewRich.Application.Contracts.Loterias;
using NewRich.Constants.Messages;
using NewRich.Domain.Services;
using NewRich.Maui.Data;
using NewRich.Maui.Services;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;
using NewRich.Pda.Core.Ventas;

namespace NewRich.Maui.Views.Vendedor;

/// <summary>
/// Formulario de «Juego números»: loterías del día, valor total y líneas de números.
/// Las líneas se repintan solo al agregar o quitar, para no perder el foco mientras se escribe.
/// </summary>
public sealed class JuegoNumerosManualPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly SesionPda _sesion;
    private readonly LocalDatabase _offline;
    private readonly JuegoNumerosManualServicio _juego;
    private readonly IServiceProvider _services;
    private readonly CargandoOverlay _cargando = new();
    private readonly ContentView _cuerpo = new();
    private readonly VerticalStackLayout _loteriasBox = new() { Spacing = 4 };
    private readonly VerticalStackLayout _lineasBox = new() { Spacing = 8 };
    private readonly Label _totalApostado = Valor();
    private readonly Label _cantidadNumeros = Valor();
    private readonly Label _cantidadLoterias = Valor();
    private readonly Label _cantidadApuestas = Valor();
    private readonly Label _valorPorApuesta = Valor();
    private readonly Label _sobrante = new() { FontSize = 12, TextColor = Ui.Gold, IsVisible = false };
    private readonly List<Entry> _entradas = [];
    private readonly List<Label> _errores = [];
    private IReadOnlyList<LoteriaResponse> _loterias = [];
    private IReadOnlyList<string> _restringidos = [];
    private CampoValorGanado? _valor;

    public JuegoNumerosManualPage(
        NewRichApiClient api,
        SesionPda sesion,
        LocalDatabase offline,
        JuegoNumerosManualServicio juego,
        IServiceProvider services)
    {
        _api = api;
        _sesion = sesion;
        _offline = offline;
        _juego = juego;
        _services = services;
        Title = JuegoNumerosManualTexts.Titulo;
        BackgroundColor = Ui.Paper;
        Content = new Grid { Children = { _cuerpo, _cargando } };
    }

    private JuegoNumerosManualBorrador Borrador => _juego.Borrador ??= new JuegoNumerosManualBorrador();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        BarraMenuVendedor.Asegurar(this, "vender");
        if (HorarioCerrado())
        {
            _cuerpo.Content = new VerticalStackLayout
            {
                Padding = 16,
                Children = { Ui.Banner($"{PdaTexts.JuegosCerrados} {PdaTexts.JuegosCerradosVenta}", Ui.DangerBg, Ui.Danger) }
            };
            return;
        }

        if (_cuerpo.Content is null || _valor is null)
        {
            Construir();
        }

        await CargarRestringidosAsync();
        await CargarLoteriasGuardadasAsync();
        _ = RefrescarLoteriasAsync();
    }

    private bool HorarioCerrado()
    {
        _sesion.HorarioCerrado = HorarioPda.EstaFuera(_sesion.Limites, DateTime.Now);
        return !HorarioPda.PuedeIniciarJuegoNuevo(_sesion.HorarioCerrado);
    }

    private void Construir()
    {
        _valor = Ui.Pesos(JuegoNumerosManualTexts.ValorJugadoPlaceholder);
        _valor.HorizontalOptions = LayoutOptions.Fill;
        if (Borrador.ValorJugado > 0)
        {
            _valor.Interno.Text = Borrador.ValorJugado.ToString(CultureInfo.InvariantCulture);
        }

        _valor.TextoCambiado += (_, _) =>
        {
            decimal.TryParse(EntradaEntera.SoloDigitos(_valor.Texto), NumberStyles.None, CultureInfo.InvariantCulture, out var monto);
            Borrador.CambiarValor(monto);
            ActualizarCalculo();
        };
        SemanticProperties.SetDescription(_valor.Interno, JuegoNumerosManualTexts.ValorJugado);

        var valorFila = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 6
        };
        valorFila.Add(new Label { Text = "$", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Ui.Dark, VerticalOptions = LayoutOptions.Center }, 0);
        valorFila.Add(_valor, 1);

        var verResumen = Ui.Primario(JuegoNumerosManualTexts.VerResumen);
        verResumen.Clicked += async (_, _) => await VerResumenAsync();

        var cancelar = Ui.Secundario(JuegoNumerosManualTexts.Cancelar);
        cancelar.Clicked += async (_, _) => await CancelarAsync();

        RenderLineas();
        ActualizarCalculo();

        _cuerpo.Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 16, 16, 24),
                Spacing = 14,
                Children =
                {
                    Ui.Banner(JuegoNumerosManualTexts.RequiereConexion, Ui.InfoBg, Ui.Info),
                    Ui.Tarjeta(new VerticalStackLayout
                    {
                        Spacing = 6,
                        Children =
                        {
                            Ui.Campo(JuegoNumerosManualTexts.Loterias),
                            new Label { Text = JuegoNumerosManualTexts.LoteriasAyuda, FontSize = 12, TextColor = Ui.Muted },
                            _loteriasBox
                        }
                    }),
                    Ui.Tarjeta(new VerticalStackLayout
                    {
                        Spacing = 6,
                        Children =
                        {
                            Ui.Campo(JuegoNumerosManualTexts.ValorJugado),
                            valorFila,
                            new Label { Text = JuegoNumerosManualTexts.ValorJugadoAyuda, FontSize = 12, TextColor = Ui.Muted }
                        }
                    }),
                    Ui.Tarjeta(new VerticalStackLayout
                    {
                        Spacing = 8,
                        Children = { Ui.Campo(JuegoNumerosManualTexts.NumeroAJugar), _lineasBox }
                    }),
                    TarjetaCalculo(),
                    verResumen,
                    cancelar
                }
            }
        };
    }

    private View TarjetaCalculo()
    {
        var filas = new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label { Text = JuegoNumerosManualTexts.Calculo, FontAttributes = FontAttributes.Bold, TextColor = Ui.Gold, FontSize = 13 },
                FilaCalculo(JuegoNumerosManualTexts.TotalApostado, _totalApostado),
                FilaCalculo(JuegoNumerosManualTexts.CantidadNumeros, _cantidadNumeros),
                FilaCalculo(JuegoNumerosManualTexts.CantidadLoterias, _cantidadLoterias),
                FilaCalculo(JuegoNumerosManualTexts.CantidadApuestas, _cantidadApuestas),
                FilaCalculo(JuegoNumerosManualTexts.ValorPorApuesta, _valorPorApuesta),
                _sobrante
            }
        };
        return new Frame
        {
            BackgroundColor = Ui.Dark,
            BorderColor = Ui.Dark,
            CornerRadius = 12,
            Padding = 14,
            Content = filas
        };
    }

    private static Grid FilaCalculo(string texto, Label valor)
    {
        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 8
        };
        grid.Add(new Label { Text = texto, TextColor = Colors.White, FontSize = 13, VerticalOptions = LayoutOptions.Center }, 0);
        grid.Add(valor, 1);
        return grid;
    }

    private static Label Valor() => new()
    {
        TextColor = Colors.White,
        FontAttributes = FontAttributes.Bold,
        FontSize = 15,
        HorizontalTextAlignment = TextAlignment.End,
        VerticalOptions = LayoutOptions.Center
    };

    private void ActualizarCalculo()
    {
        var calculo = Borrador.Calcular();
        _totalApostado.Text = FormatoDinero.Pesos(Borrador.ValorJugado);
        _cantidadNumeros.Text = calculo.CantidadNumeros.ToString(CultureInfo.InvariantCulture);
        _cantidadLoterias.Text = calculo.CantidadLoterias.ToString(CultureInfo.InvariantCulture);
        _cantidadApuestas.Text = calculo.CantidadApuestas.ToString(CultureInfo.InvariantCulture);
        _valorPorApuesta.Text = FormatoDinero.Pesos(calculo.ValorPorApuesta);
        _sobrante.IsVisible = calculo.ApuestasConSobrante > 0;
        _sobrante.Text = calculo.ApuestasConSobrante > 0 ? JuegoNumerosManualTexts.Sobrante(calculo.ApuestasConSobrante) : string.Empty;
    }

    private void RenderLineas(int? enfocar = null)
    {
        _lineasBox.Children.Clear();
        _entradas.Clear();
        _errores.Clear();
        for (var i = 0; i < Borrador.Lineas.Count; i++)
        {
            _lineasBox.Children.Add(Linea(i));
        }

        MarcarErrores(false);
        if (enfocar is int indice && indice < _entradas.Count)
        {
            Dispatcher.Dispatch(() => _entradas[indice].Focus());
        }
    }

    private View Linea(int indice)
    {
        var entrada = Ui.Entero(JuegoNumerosManualTexts.NumeroPlaceholder, 4);
        entrada.Text = Borrador.Lineas[indice];
        entrada.FontSize = 18;
        entrada.HeightRequest = 48;
        SemanticProperties.SetDescription(entrada, $"{JuegoNumerosManualTexts.NumeroAJugar} {indice + 1}");
        entrada.TextChanged += (_, args) =>
        {
            Borrador.CambiarNumero(indice, args.NewTextValue);
            MarcarErrores(false);
            ActualizarCalculo();
        };
        entrada.Unfocused += (_, _) => MarcarErrores(true);
        _entradas.Add(entrada);

        var quitar = new Button
        {
            Text = "-",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = Colors.White,
            TextColor = Ui.Danger,
            BorderColor = Ui.Danger,
            BorderWidth = 1,
            CornerRadius = 10,
            WidthRequest = 48,
            HeightRequest = 48,
            Padding = 0
        };
        SemanticProperties.SetDescription(quitar, $"{JuegoNumerosManualTexts.QuitarLineaAccesible} {indice + 1}");
        quitar.Clicked += (_, _) =>
        {
            Borrador.QuitarLinea(indice);
            RenderLineas();
            ActualizarCalculo();
        };

        var fila = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(28)),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8
        };
        fila.Add(new Label { Text = $"{indice + 1}.", TextColor = Ui.Muted, FontSize = 14, VerticalOptions = LayoutOptions.Center }, 0);
        fila.Add(entrada, 1);
        fila.Add(quitar, 2);
        if (Borrador.EsUltimaLinea(indice))
        {
            var agregar = new Button
            {
                Text = "+",
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                BackgroundColor = Ui.Dark,
                TextColor = Colors.White,
                CornerRadius = 10,
                WidthRequest = 48,
                HeightRequest = 48,
                Padding = 0
            };
            SemanticProperties.SetDescription(agregar, JuegoNumerosManualTexts.AgregarLineaAccesible);
            agregar.Clicked += (_, _) =>
            {
                Borrador.AgregarLinea();
                RenderLineas(Borrador.Lineas.Count - 1);
                ActualizarCalculo();
            };
            fila.Add(agregar, 3);
        }

        var error = new Label { FontSize = 12, TextColor = Ui.Danger, IsVisible = false, Margin = new Thickness(36, 0, 0, 0) };
        _errores.Add(error);
        return new VerticalStackLayout { Spacing = 2, Children = { fila, error } };
    }

    /// <summary>Repetidos y restringidos se avisan al escribir; el formato incompleto, al salir del campo.</summary>
    private void MarcarErrores(bool incluirFormato)
    {
        var vistos = new HashSet<string>();
        for (var i = 0; i < _errores.Count && i < Borrador.Lineas.Count; i++)
        {
            var numero = Borrador.Lineas[i];
            string? mensaje = null;
            if (numero.Length > 0)
            {
                if (!vistos.Add(numero))
                {
                    mensaje = string.Format(JuegoNumerosManualMessages.NumeroRepetido, numero);
                }
                else if (NumerosRestringidos.EstaBloqueado(numero, _restringidos))
                {
                    mensaje = string.Format(JuegoNumerosManualMessages.NumeroRestringido, numero);
                }
                else if (incluirFormato && !NumeroApuesta.EsValido(numero))
                {
                    mensaje = string.Format(JuegoNumerosManualMessages.NumeroFormato, numero);
                }
            }

            if (mensaje is null && !incluirFormato && _errores[i].IsVisible && !NumeroApuesta.EsValido(numero) && numero.Length > 0)
            {
                continue;
            }

            _errores[i].Text = mensaje ?? string.Empty;
            _errores[i].IsVisible = mensaje is not null;
        }
    }

    private void RenderLoterias()
    {
        Borrador.ConservarLoterias(_loterias.Select(l => l.LoteriaId));
        _loteriasBox.Children.Clear();
        if (_loterias.Count == 0)
        {
            _loteriasBox.Children.Add(new Label { Text = JuegoNumerosManualTexts.SinLoterias, TextColor = Ui.Danger, FontSize = 13 });
            ActualizarCalculo();
            return;
        }

        foreach (var loteria in _loterias)
        {
            var actual = loteria;
            var check = new CheckBox { Color = Ui.Dark, IsChecked = Borrador.LoteriaIds.Contains(actual.LoteriaId) };
            SemanticProperties.SetDescription(check, actual.Nombre);
            check.CheckedChanged += (_, args) =>
            {
                Borrador.Seleccionar(actual.LoteriaId, actual.Nombre, args.Value);
                ActualizarCalculo();
            };
            var cierre = TimeSpan.TryParse(actual.HoraFin, out var fin) ? $"{JuegoNumerosManualTexts.Cierra} {fin:hh\\:mm}" : string.Empty;
            var fila = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                ColumnSpacing = 6,
                MinimumHeightRequest = 44
            };
            fila.Add(check, 0);
            fila.Add(new Label { Text = actual.Nombre, TextColor = Ui.Ink, FontSize = 15, VerticalOptions = LayoutOptions.Center }, 1);
            fila.Add(new Label { Text = cierre, TextColor = Ui.Muted, FontSize = 12, VerticalOptions = LayoutOptions.Center }, 2);
            var toque = new TapGestureRecognizer();
            toque.Tapped += (_, _) => check.IsChecked = !check.IsChecked;
            fila.GestureRecognizers.Add(toque);
            _loteriasBox.Children.Add(fila);
        }

        ActualizarCalculo();
    }

    private IReadOnlyList<LoteriaResponse> DisponiblesAhora(IEnumerable<LoteriaResponse> loterias) =>
        LoteriasDelDia.Filtrar(loterias, DateTime.Now);

    private async Task CargarLoteriasGuardadasAsync()
    {
        try
        {
            _loterias = DisponiblesAhora(await _offline.LoteriasAsync());
        }
        catch (Exception)
        {
            _loterias = [];
        }

        RenderLoterias();
    }

    private async Task RefrescarLoteriasAsync()
    {
        try
        {
            using var corte = new CancellationTokenSource(CargaLoterias.MsEspera);
            var loterias = await _api.LoteriasAsync(corte.Token);
            if (!loterias.IsSuccess || loterias.Data is null)
            {
                return;
            }

            await _offline.GuardarLoteriasAsync(loterias.Data);
            var frescas = DisponiblesAhora(loterias.Data);
            if (CargaLoterias.SonIguales(frescas, _loterias))
            {
                return;
            }

            _loterias = frescas;
            RenderLoterias();
        }
        catch (Exception)
        {
        }
    }

    private async Task CargarRestringidosAsync()
    {
        try
        {
            var guardados = await _offline.NumerosRestringidosAsync();
            _restringidos = NumerosRestringidosPda.Vigentes(guardados.Count > 0 ? guardados : null, _sesion.Limites.NumerosRestringidos);
        }
        catch (Exception)
        {
            _restringidos = NumerosRestringidosPda.Vigentes(_sesion.Limites.NumerosRestringidos, null);
        }
    }

    private async Task VerResumenAsync()
    {
        if (HorarioCerrado())
        {
            await this.AvisoAsync(JuegoNumerosManualTexts.Titulo, $"{PdaTexts.JuegosCerrados} {PdaTexts.JuegosCerradosVenta}", PdaTexts.Entendido);
            return;
        }

        MarcarErrores(true);
        var error = Borrador.Validar(_restringidos);
        if (error is null)
        {
            var abiertas = DisponiblesAhora(_loterias).Select(l => l.LoteriaId).ToHashSet();
            var cerrada = _loterias.FirstOrDefault(l => Borrador.LoteriaIds.Contains(l.LoteriaId) && !abiertas.Contains(l.LoteriaId));
            if (cerrada is not null)
            {
                error = string.Format(JuegoNumerosManualMessages.LoteriaCerrada, cerrada.Nombre);
                _loterias = DisponiblesAhora(_loterias);
                RenderLoterias();
            }
        }

        if (error is not null)
        {
            await this.AvisoAsync(JuegoNumerosManualTexts.Titulo, error, PdaTexts.Entendido);
            return;
        }

        await Navigation.PushAsync(_services.GetRequiredService<ResumenJuegoNumerosManualPage>());
    }

    private async Task CancelarAsync()
    {
        if (!await this.ConfirmarAsync(JuegoNumerosManualTexts.Cancelar, JuegoNumerosManualTexts.CancelarConfirma, PdaTexts.SiCancelar, PdaTexts.No))
        {
            return;
        }

        _juego.Borrador = null;
        await Shell.Current.GoToAsync("//inicio");
    }
}
