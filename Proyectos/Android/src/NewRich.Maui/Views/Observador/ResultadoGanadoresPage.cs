using Microsoft.Maui.Controls.Shapes;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Resultados;
using NewRich.Domain.Services;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;

namespace NewRich.Maui.Views.Observador;

/// <summary>Lista de boletos ganadores de un número publicado.</summary>
public sealed class ResultadoGanadoresPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly ResultadoResponse _resultado;
    private readonly VerticalStackLayout _lista = new() { Spacing = 10 };
    private bool _cargado;

    public ResultadoGanadoresPage(NewRichApiClient api, ResultadoResponse resultado)
    {
        _api = api;
        _resultado = resultado;
        Title = PdaTexts.ResultadoGanadoresTitulo;
        BackgroundColor = Ui.Paper;

        var volver = Ui.Secundario(PdaTexts.Cerrar);
        volver.Clicked += async (_, _) => await Navigation.PopAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 14,
                Children =
                {
                    Encabezado(),
                    new Label
                    {
                        Text = PdaTexts.ResultadoGanadoresAyuda,
                        FontSize = 12,
                        TextColor = Ui.Muted
                    },
                    _lista,
                    volver
                }
            }
        };

        _lista.Children.Add(new Label
        {
            Text = PdaTexts.GanadorCargando,
            TextColor = Ui.Muted,
            FontSize = 12
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_cargado)
        {
            return;
        }

        _cargado = true;
        try
        {
            await CargarAsync();
        }
        catch (Exception)
        {
            MostrarVacio(PdaTexts.SinConexionServidor);
        }
    }

    private View Encabezado() => new Border
    {
        BackgroundColor = Color.FromArgb(ConsultaObservadorResultados.ColorGanador),
        Stroke = Colors.Transparent,
        StrokeShape = new RoundRectangle { CornerRadius = 16 },
        Padding = new Thickness(16, 14),
        Content = new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label
                {
                    Text = _resultado.Loteria,
                    TextColor = Ui.Dark,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold
                },
                new Label
                {
                    Text = _resultado.Numero,
                    TextColor = Ui.Dark,
                    FontSize = 28,
                    FontAttributes = FontAttributes.Bold
                },
                new Label
                {
                    Text = $"{_resultado.FechaJuego:dd/MM/yyyy} · {PdaTexts.CantidadGanadores(_resultado.CantidadGanadores)}",
                    TextColor = Ui.Dark,
                    FontSize = 12
                }
            }
        }
    };

    private async Task CargarAsync()
    {
        var respuesta = await _api.GanadoresResultadoAsync(_resultado.NumeroGanadorId, CancellationToken.None);
        if (!respuesta.IsSuccess)
        {
            MostrarVacio(respuesta.Message);
            return;
        }

        var ganadores = respuesta.Data ?? [];
        if (ganadores.Count == 0)
        {
            MostrarVacio(PdaTexts.ResultadoSinGanadores);
            return;
        }

        _lista.Children.Clear();
        foreach (var boleto in ganadores)
        {
            _lista.Children.Add(Tarjeta(boleto));
        }
    }

    private View Tarjeta(BoletoListaResponse boleto)
    {
        var color = ConsultaObservadorResultados.ColorResaltado(boleto.Estado)
                    ?? ConsultaObservadorResultados.ColorGanador;

        var izquierda = new VerticalStackLayout
        {
            Spacing = 3,
            Children =
            {
                new Label
                {
                    Text = CodigoPublicoGenerator.FormatoImpreso(boleto.CodigoPublico),
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Ui.Ink
                },
                new Label
                {
                    Text = $"{boleto.Vendedor} · {boleto.Fecha:dd/MM/yyyy HH:mm}",
                    FontSize = 12,
                    TextColor = Ui.Dark
                }
            }
        };

        var derecha = new VerticalStackLayout
        {
            Spacing = 4,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = FormatoDinero.Pesos(boleto.Total),
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Ui.Ink,
                    HorizontalTextAlignment = TextAlignment.End
                },
                new Label
                {
                    Text = boleto.Estado,
                    FontSize = 10,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Ui.Dark,
                    HorizontalTextAlignment = TextAlignment.End
                }
            }
        };

        var contenido = new Grid { ColumnSpacing = 10 };
        contenido.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        contenido.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        contenido.Add(izquierda, 0, 0);
        contenido.Add(derecha, 1, 0);

        var tarjeta = new Border
        {
            BackgroundColor = Color.FromArgb(color),
            Stroke = Color.FromArgb(color),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Padding = new Thickness(16, 14),
            Content = contenido
        };

        var toque = new TapGestureRecognizer();
        toque.Tapped += async (_, _) =>
        {
            try
            {
                await Navigation.PushAsync(new BoletoGanadorPage(_api, boleto));
            }
            catch (Exception)
            {
            }
        };
        tarjeta.GestureRecognizers.Add(toque);
        return tarjeta;
    }

    private void MostrarVacio(string mensaje)
    {
        _lista.Children.Clear();
        _lista.Children.Add(new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Ui.Line,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Padding = 18,
            Content = new Label
            {
                Text = mensaje,
                TextColor = Ui.Muted,
                FontSize = 12,
                HorizontalTextAlignment = TextAlignment.Center
            }
        });
    }
}
