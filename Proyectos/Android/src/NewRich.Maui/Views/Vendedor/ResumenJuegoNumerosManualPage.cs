using System.Globalization;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Maui.Services;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;
using NewRich.Pda.Core.Ventas;

namespace NewRich.Maui.Views.Vendedor;

/// <summary>
/// Resumen de «Combo» antes de registrar. Solo registra en línea; si la red falla la jugada
/// sigue en pantalla con el mismo identificador para reintentar sin duplicarla.
/// </summary>
public sealed class ResumenJuegoNumerosManualPage : ContentPage
{
    private readonly JuegoNumerosManualApiClient _juegos;
    private readonly JuegoNumerosManualServicio _juego;
    private readonly SesionPda _sesion;
    private readonly IServiceProvider _services;
    private readonly CargandoOverlay _cargando = new();
    private readonly ContentView _cuerpo = new();
    private Button? _registrar;
    private bool _registrando;

    public ResumenJuegoNumerosManualPage(
        JuegoNumerosManualApiClient juegos,
        JuegoNumerosManualServicio juego,
        SesionPda sesion,
        IServiceProvider services)
    {
        _juegos = juegos;
        _juego = juego;
        _sesion = sesion;
        _services = services;
        Title = JuegoNumerosManualTexts.Resumen;
        BackgroundColor = Ui.Paper;
        Content = new Grid { Children = { _cuerpo, _cargando } };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BarraMenuVendedor.Asegurar(this, "vender");
        var borrador = _juego.Borrador;
        if (borrador is null)
        {
            _cuerpo.Content = new Label { Text = JuegoNumerosManualMessages.NumerosRequeridos, Padding = 16, TextColor = Ui.Ink };
            return;
        }

        Pintar(borrador);
    }

    private void Pintar(JuegoNumerosManualBorrador borrador)
    {
        var calculo = borrador.Calcular();
        var loterias = new VerticalStackLayout { Spacing = 2 };
        foreach (var nombre in borrador.LoteriaNombres)
        {
            loterias.Children.Add(new Label { Text = nombre, TextColor = Ui.Ink, FontSize = 15, FontAttributes = FontAttributes.Bold });
        }

        var datos = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                Ui.Campo(JuegoNumerosManualTexts.Loterias),
                loterias,
                Separador(),
                Fila(JuegoNumerosManualTexts.ValorJugado, FormatoDinero.Pesos(borrador.ValorJugado)),
                Separador(),
                Ui.Campo(JuegoNumerosManualTexts.NumerosAJugar),
                Numeros(borrador.NumerosValidos),
                Separador(),
                Fila(JuegoNumerosManualTexts.TotalApostado, FormatoDinero.Pesos(borrador.ValorJugado)),
                Fila(JuegoNumerosManualTexts.CantidadNumeros, calculo.CantidadNumeros.ToString(CultureInfo.InvariantCulture)),
                Fila(JuegoNumerosManualTexts.CantidadLoterias, calculo.CantidadLoterias.ToString(CultureInfo.InvariantCulture)),
                Fila(JuegoNumerosManualTexts.CantidadApuestas, calculo.CantidadApuestas.ToString(CultureInfo.InvariantCulture)),
                Fila(JuegoNumerosManualTexts.ValorPorApuesta, FormatoDinero.Pesos(calculo.ValorPorApuesta))
            }
        };
        if (calculo.ApuestasConSobrante > 0)
        {
            datos.Children.Add(new Label { Text = JuegoNumerosManualTexts.Sobrante(calculo.ApuestasConSobrante), FontSize = 12, TextColor = Ui.Warn });
        }

        datos.Children.Add(Fila(JuegoNumerosManualTexts.FechaHora, DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)));
        datos.Children.Add(new Label
        {
            Text = JuegoNumerosManualTexts.MuchaSuerte,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = Ui.Green,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0)
        });

        _registrar = Ui.Primario(JuegoNumerosManualTexts.Registrar);
        _registrar.Clicked += async (_, _) => await RegistrarAsync(borrador);
        var corregir = Ui.Secundario(JuegoNumerosManualTexts.Corregir);
        corregir.Clicked += async (_, _) =>
        {
            if (!_registrando)
            {
                await Navigation.PopAsync();
            }
        };

        _cuerpo.Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 16, 16, 24),
                Spacing = 14,
                Children =
                {
                    new Label { Text = JuegoNumerosManualTexts.Titulo, FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink },
                    new Label { Text = JuegoNumerosManualTexts.ResumenAyuda, FontSize = 13, TextColor = Ui.Muted },
                    Ui.Tarjeta(datos),
                    _registrar,
                    corregir
                }
            }
        };
    }

    /// <summary>Los números se reparten en tres columnas para que se lean de un vistazo en el PDA.</summary>
    private static View Numeros(IReadOnlyList<string> numeros)
    {
        const int columnas = 3;
        var grid = new Grid { ColumnSpacing = 8, RowSpacing = 6 };
        for (var c = 0; c < columnas; c++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        for (var i = 0; i < numeros.Count; i++)
        {
            if (i % columnas == 0)
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }

            grid.Add(new Border
            {
                Stroke = Ui.Line,
                BackgroundColor = Ui.Mint,
                Padding = new Thickness(6, 8),
                Content = new Label
                {
                    Text = numeros[i],
                    FontSize = 17,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Ui.Dark,
                    HorizontalTextAlignment = TextAlignment.Center
                }
            }, i % columnas, i / columnas);
        }

        return grid;
    }

    private static Grid Fila(string texto, string valor)
    {
        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 8
        };
        grid.Add(new Label { Text = texto, TextColor = Ui.Muted, FontSize = 13, VerticalOptions = LayoutOptions.Center }, 0);
        grid.Add(new Label { Text = valor, TextColor = Ui.Ink, FontSize = 15, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.End, VerticalOptions = LayoutOptions.Center }, 1);
        return grid;
    }

    private static BoxView Separador() => new() { HeightRequest = 1, Color = Ui.Line };

    private string Leyenda()
    {
        var vigencia = _sesion.Limites.VigenciaPremiosDias > 0 ? _sesion.Limites.VigenciaPremiosDias : 30;
        return TirillaCuerpo.Leyenda(vigencia, _sesion.Limites.LeyendaTirilla);
    }

    private async Task RegistrarAsync(JuegoNumerosManualBorrador borrador)
    {
        if (_registrando)
        {
            return;
        }

        _registrando = true;
        if (_registrar is not null)
        {
            _registrar.IsEnabled = false;
        }

        try
        {
            _sesion.HorarioCerrado = HorarioPda.EstaFuera(_sesion.Limites, DateTime.Now);
            if (!HorarioPda.PuedeIniciarJuegoNuevo(_sesion.HorarioCerrado))
            {
                await this.AvisoAsync(JuegoNumerosManualTexts.Titulo, $"{PdaTexts.JuegosCerrados} {PdaTexts.JuegosCerradosVenta}", PdaTexts.Entendido);
                return;
            }

            _cargando.Mostrar(JuegoNumerosManualTexts.Registrando);
            if (await _juego.ConectarAsync(CancellationToken.None))
            {
                var resultado = await _juegos.RegistrarAsync(borrador.ARequest(), CancellationToken.None);
                if (resultado.IsSuccess && resultado.Data is not null)
                {
                    await TerminarAsync(ReciboJuegoNumerosManual.DesdeRespuesta(resultado.Data, Leyenda()));
                    return;
                }

                if (!JuegoNumerosManualApiClient.EsFalloDeConexion(resultado.StatusCode))
                {
                    _cargando.Ocultar();
                    await this.AvisoAsync(JuegoNumerosManualTexts.Titulo, resultado.Message, PdaTexts.Entendido);
                    return;
                }
            }

            _cargando.Ocultar();
            await AvisarSinConexionAsync();
        }
        catch (Exception)
        {
            _cargando.Ocultar();
            await AvisarSinConexionAsync();
        }
        finally
        {
            _cargando.Ocultar();
            _registrando = false;
            if (_registrar is not null)
            {
                _registrar.IsEnabled = true;
            }
        }
    }

    private Task AvisarSinConexionAsync() =>
        this.AvisoAsync(JuegoNumerosManualTexts.ConexionNoDisponible, JuegoNumerosManualTexts.SinConexionRegistrar, PdaTexts.Entendido);

    private async Task TerminarAsync(ReciboJuegoNumerosManual recibo)
    {
        _cargando.Ocultar();
        _juego.Recibo = recibo;
        _juego.Borrador = null;
        await Navigation.PushAsync(_services.GetRequiredService<TirillaJuegoNumerosManualPage>());
    }
}
