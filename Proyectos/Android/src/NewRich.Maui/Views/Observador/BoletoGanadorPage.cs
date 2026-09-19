using Microsoft.Maui.Controls.Shapes;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Premios;
using NewRich.Domain.Services;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;

namespace NewRich.Maui.Views.Observador;

/// <summary>Detalle completo de un boleto ganador elegido desde Consultas.</summary>
public sealed class BoletoGanadorPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly BoletoListaResponse _boleto;
    private readonly VerticalStackLayout _cuerpo = new() { Spacing = 14 };
    private bool _cargado;

    public BoletoGanadorPage(NewRichApiClient api, BoletoListaResponse boleto)
    {
        _api = api;
        _boleto = boleto;
        Title = PdaTexts.GanadorDetalleTitulo;
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
                    _cuerpo,
                    volver
                }
            }
        };

        _cuerpo.Children.Add(new Label
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
            Mostrar(new Label { Text = PdaTexts.SinConexionServidor, TextColor = Ui.Muted, FontSize = 12 });
        }
    }

    private View Encabezado()
    {
        var color = ConsultaObservadorResultados.ColorResaltado(_boleto.Estado)
                    ?? ConsultaObservadorResultados.ColorGanador;
        return new Border
        {
            BackgroundColor = Color.FromArgb(color),
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
                        Text = _boleto.Estado,
                        TextColor = Ui.Dark,
                        FontSize = 12,
                        FontAttributes = FontAttributes.Bold,
                        CharacterSpacing = 1
                    },
                    new Label
                    {
                        Text = CodigoPublicoGenerator.FormatoImpreso(_boleto.CodigoPublico),
                        TextColor = Ui.Dark,
                        FontSize = 26,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = $"{_boleto.Vendedor} · {_boleto.Fecha:dd/MM/yyyy HH:mm}",
                        TextColor = Ui.Dark,
                        FontSize = 12
                    }
                }
            }
        };
    }

    private async Task CargarAsync()
    {
        var consulta = await _api.ConsultarTicketObservadorAsync(
            new ConsultaTicketRequest { TicketCode = _boleto.CodigoPublico },
            CancellationToken.None);

        var secciones = new List<View> { Boleta(consulta.Data) };

        if (consulta.Data?.Resultados.Count > 0)
        {
            secciones.Add(Sorteo(consulta.Data.Resultados));
        }

        var caso = await BuscarCasoAsync();
        secciones.Add(Premio(caso));

        if (caso is not null && caso.Evidencias.Count > 0)
        {
            foreach (var vista in await EvidenciasAsync(caso))
            {
                secciones.Add(vista);
            }
        }

        Mostrar([.. secciones]);
    }

    private async Task<CasoGanadorResponse?> BuscarCasoAsync()
    {
        var premios = await _api.PremiosAsync(CancellationToken.None);
        return premios.IsSuccess
            ? (premios.Data ?? []).FirstOrDefault(c => c.BoletoId == _boleto.BoletoId)
            : null;
    }

    private async Task<IReadOnlyList<View>> EvidenciasAsync(CasoGanadorResponse caso)
    {
        var vistas = new List<View>();
        foreach (var evidencia in caso.Evidencias)
        {
            var descarga = await _api.DescargarEvidenciaPremioAsync(caso.CasoId, evidencia.EvidenciaId, CancellationToken.None);
            if (!descarga.IsSuccess || descarga.Data is null)
            {
                continue;
            }

            var bytes = descarga.Data.Bytes;
            vistas.Add(Ui.Tarjeta(new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label
                    {
                        Text = $"{evidencia.Tipo} · {evidencia.FechaCaptura:dd/MM/yyyy HH:mm}",
                        FontSize = 11,
                        TextColor = Ui.Muted
                    },
                    new Image
                    {
                        Source = ImageSource.FromStream(() => new MemoryStream(bytes)),
                        Aspect = Aspect.AspectFit,
                        HeightRequest = 200
                    }
                }
            }));
        }

        return vistas;
    }

    private View Boleta(ConsultaTicketResponse? consulta)
    {
        var bloque = new VerticalStackLayout { Spacing = 10 };
        bloque.Children.Add(Seccion(PdaTexts.GanadorDatosBoleta));
        bloque.Children.Add(Dato(PdaTexts.TicketCode, CodigoPublicoGenerator.FormatoImpreso(_boleto.CodigoPublico)));
        bloque.Children.Add(Dato(PdaTexts.Vendedor, _boleto.Vendedor));
        bloque.Children.Add(Dato(PdaTexts.FechaVenta, _boleto.Fecha.ToString("dd/MM/yyyy HH:mm")));
        bloque.Children.Add(Dato(PdaTexts.ValorApostado, FormatoDinero.Pesos(_boleto.Total)));

        var tirilla = consulta?.Tirilla;
        if (tirilla is not null)
        {
            bloque.Children.Add(Dato(PdaTexts.TipoApuestaFiltro, ConsultaObservadorResultados.TipoApuestaTexto(tirilla.TipoApuesta)));
            bloque.Children.Add(Seccion(PdaTexts.GanadorNumerosJugados));
            foreach (var juego in tirilla.Juegos)
            {
                bloque.Children.Add(Dato(
                    $"{juego.Numero} · {string.Join(", ", juego.Loterias)}",
                    $"{FormatoDinero.Pesos(juego.Valor)} → {FormatoDinero.Pesos(juego.Total)}"));
            }
        }

        return Ui.Tarjeta(bloque);
    }

    private static View Sorteo(IReadOnlyList<ResultadoLoteriaResponse> resultados)
    {
        var bloque = new VerticalStackLayout { Spacing = 10 };
        bloque.Children.Add(Seccion(PdaTexts.GanadorResultadoSorteo));
        foreach (var resultado in resultados)
        {
            var linea = new Label
            {
                Text = $"{resultado.Numero} vs {resultado.NumeroGanador ?? "-"}",
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = resultado.Gano ? Ui.Green : Ui.Ink
            };

            bloque.Children.Add(new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    new Label { Text = resultado.Loteria, FontSize = 11, TextColor = Ui.Muted },
                    linea
                }
            });
        }

        return Ui.Tarjeta(bloque);
    }

    private static View Premio(CasoGanadorResponse? caso)
    {
        var bloque = new VerticalStackLayout { Spacing = 10 };
        bloque.Children.Add(Seccion(PdaTexts.GanadorEstadoPremio));

        if (caso is null)
        {
            bloque.Children.Add(new Label { Text = PdaTexts.GanadorSinCaso, FontSize = 12, TextColor = Ui.Muted });
            return Ui.Tarjeta(bloque);
        }

        bloque.Children.Add(Dato(PdaTexts.CasosPremiosEstado, caso.Estado));
        bloque.Children.Add(Dato(PdaTexts.ComprobanteGanador, $"{caso.NombreGanador} {caso.ApellidoGanador}".Trim()));
        bloque.Children.Add(Dato(PdaTexts.NumeroContacto, caso.NumeroContacto ?? "-"));
        bloque.Children.Add(Dato(PdaTexts.LugarGano, caso.LugarGano ?? "-"));
        bloque.Children.Add(Dato(PdaTexts.ValorTotalGanado, FormatoDinero.Pesos(caso.ValorTotalGanado ?? 0m)));
        bloque.Children.Add(Dato(
            PdaTexts.ComprobanteFecha,
            caso.FechaEntrega?.ToString("dd/MM/yyyy HH:mm") ?? "-"));

        if (caso.Evidencias.Count == 0)
        {
            bloque.Children.Add(new Label { Text = PdaTexts.GanadorSinEvidencias, FontSize = 12, TextColor = Ui.Muted });
        }

        return Ui.Tarjeta(bloque);
    }

    private static Label Seccion(string texto) => new()
    {
        Text = texto,
        FontSize = 13,
        FontAttributes = FontAttributes.Bold,
        TextColor = Ui.Dark
    };

    private static View Dato(string titulo, string valor) => new VerticalStackLayout
    {
        Spacing = 2,
        Children =
        {
            new Label { Text = titulo, FontSize = 11, TextColor = Ui.Muted },
            new Label
            {
                Text = string.IsNullOrWhiteSpace(valor) ? "-" : valor,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Ui.Ink
            }
        }
    };

    private void Mostrar(params View[] vistas)
    {
        _cuerpo.Children.Clear();
        foreach (var vista in vistas)
        {
            _cuerpo.Children.Add(vista);
        }
    }
}
