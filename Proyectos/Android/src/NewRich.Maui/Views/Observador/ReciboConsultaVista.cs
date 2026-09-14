using Microsoft.Maui.Controls.Shapes;
using NewRich.Constants;
using NewRich.Pda.Core;

namespace NewRich.Maui.Views;

public static class ReciboConsultaVista
{
    public static View Crear(TicketConsultaVista vista)
    {
        var cuerpo = new VerticalStackLayout { Spacing = 0 };
        var (fondoEstado, letraEstado) = ColorEstado(vista.Tono);
        cuerpo.Children.Add(Cabecera());
        cuerpo.Children.Add(new BoxView { Color = Color.FromArgb("#c9a44a"), HeightRequest = 3 });
        cuerpo.Children.Add(new VerticalStackLayout
        {
            Padding = new Thickness(18, 16),
            Spacing = 10,
            Children =
            {
                new Label
                {
                    Text = vista.Codigo,
                    FontFamily = "OpenSansSemibold",
                    FontSize = 26,
                    TextColor = Ui.Dark,
                    HorizontalTextAlignment = TextAlignment.Center,
                    CharacterSpacing = 1.2
                },
                new Border
                {
                    StrokeThickness = 0,
                    BackgroundColor = fondoEstado,
                    StrokeShape = new RoundRectangle { CornerRadius = 8 },
                    Padding = new Thickness(12, 10),
                    Content = InsigniaEstado(vista.Estado, vista.Mensaje, letraEstado)
                },
                Fila(PdaTexts.Vendedor, vista.Vendedor),
                Fila(PdaTexts.Fecha, vista.Fecha == default ? "—" : vista.Fecha.ToLocalTime().ToString("dd/MM/yyyy HH:mm"))
            }
        });
        var juegos = new VerticalStackLayout { Padding = new Thickness(18, 0, 18, 8), Spacing = 8 };
        juegos.Children.Add(new Label
        {
            Text = PdaTexts.JuegosDelBoleto,
            FontAttributes = FontAttributes.Bold,
            FontSize = 12,
            TextColor = Ui.Muted
        });
        foreach (var juego in vista.Juegos)
        {
            juegos.Children.Add(new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 10,
                Children =
                {
                    Celda(juego.Numero, 0, true),
                    Celda(juego.Loterias, 1, false),
                    Celda(FormatoDinero.Pesos(juego.Valor), 2, true)
                }
            });
        }

        if (vista.Juegos.Count == 0)
        {
            juegos.Children.Add(new Label { Text = PdaTexts.SinJuegos, TextColor = Ui.Muted, FontSize = 13 });
        }

        cuerpo.Children.Add(juegos);
        var resultados = CrearResultados(vista);
        if (resultados is not null)
        {
            cuerpo.Children.Add(resultados);
        }
        cuerpo.Children.Add(new Grid
        {
            Padding = new Thickness(18, 12, 18, 18),
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            Children =
            {
                new Label
                {
                    Text = PdaTexts.TotalApostado,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 14,
                    TextColor = Ui.Dark
                },
                Columna(new Label
                {
                    Text = FormatoDinero.Pesos(vista.Total),
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 16,
                    TextColor = Ui.Dark
                }, 1)
            }
        });

        return new Border
        {
            Stroke = Color.FromArgb("#c9a44a"),
            StrokeThickness = 1.5,
            BackgroundColor = Color.FromArgb("#fffaf0"),
            StrokeShape = new RoundRectangle { CornerRadius = 4 },
            Padding = 0,
            Content = cuerpo
        };
    }

    public static View? CrearResultados(TicketConsultaVista vista)
    {
        if (vista.Resultados.Count == 0 && string.IsNullOrWhiteSpace(vista.AvisoResultados))
        {
            return null;
        }

        var bloque = new VerticalStackLayout
        {
            Padding = new Thickness(18, 0, 18, 8),
            Spacing = 8
        };
        bloque.Children.Add(new Label
        {
            Text = PdaTexts.ResultadoPorLoteria,
            FontAttributes = FontAttributes.Bold,
            FontSize = 12,
            TextColor = Ui.Muted
        });
        foreach (var resultado in vista.Resultados)
        {
            var (fondo, letra) = ColorEstado(resultado.Tono);
            bloque.Children.Add(new Border
            {
                StrokeThickness = 0,
                BackgroundColor = fondo,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Padding = new Thickness(12, 8),
                Content = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Auto)
                    },
                    Children =
                    {
                        new VerticalStackLayout
                        {
                            Spacing = 2,
                            Children =
                            {
                                new Label
                                {
                                    Text = $"{resultado.Loteria} · {resultado.NumeroApostado}",
                                    FontAttributes = FontAttributes.Bold,
                                    FontSize = 13,
                                    TextColor = letra
                                },
                                new Label
                                {
                                    Text = resultado.NumeroGanador == "—"
                                        ? PdaTexts.SinPublicar
                                        : $"{PdaTexts.NumeroGanador}: {resultado.NumeroGanador}",
                                    FontSize = 12,
                                    TextColor = letra
                                }
                            }
                        },
                        Columna(new Label
                        {
                            Text = resultado.Veredicto,
                            FontAttributes = FontAttributes.Bold,
                            FontSize = 13,
                            TextColor = letra,
                            VerticalTextAlignment = TextAlignment.Center
                        }, 1)
                    }
                }
            });
        }

        if (!string.IsNullOrWhiteSpace(vista.AvisoResultados))
        {
            bloque.Children.Add(new Label
            {
                Text = vista.AvisoResultados,
                FontSize = 12,
                TextColor = Ui.Warn
            });
        }

        return bloque;
    }

    public static View InsigniaEstado(string estado, string mensaje, Color letra)
    {
        var textos = new VerticalStackLayout { Spacing = 4 };
        textos.Children.Add(new Label
        {
            Text = estado,
            FontAttributes = FontAttributes.Bold,
            FontSize = 15,
            TextColor = letra,
            HorizontalTextAlignment = TextAlignment.Center
        });
        if (!string.IsNullOrWhiteSpace(mensaje))
        {
            textos.Children.Add(new Label
            {
                Text = mensaje,
                FontSize = 13,
                TextColor = letra,
                HorizontalTextAlignment = TextAlignment.Center
            });
        }

        return textos;
    }

    private static View Celda(string texto, int columna, bool negrita)
    {
        var etiqueta = new Label
        {
            Text = texto,
            FontSize = 14,
            FontAttributes = negrita ? FontAttributes.Bold : FontAttributes.None,
            TextColor = Ui.Ink,
            VerticalTextAlignment = TextAlignment.Center
        };
        Grid.SetColumn(etiqueta, columna);
        return etiqueta;
    }

    private static View Cabecera()
    {
        return new VerticalStackLayout
        {
            BackgroundColor = Ui.Dark,
            Padding = new Thickness(18, 16),
            Spacing = 6,
            Children =
            {
                new Label
                {
                    Text = PdaTexts.Marca,
                    TextColor = Color.FromArgb("#f0d078"),
                    FontSize = 11,
                    CharacterSpacing = 3,
                    FontAttributes = FontAttributes.Bold
                },
                new Label
                {
                    Text = PdaTexts.ReciboDeVenta,
                    TextColor = Colors.White,
                    FontSize = 20,
                    FontAttributes = FontAttributes.Bold
                }
            }
        };
    }

    private static View Fila(string etiqueta, string valor) =>
        new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            Children =
            {
                new Label { Text = etiqueta, TextColor = Ui.Muted, FontSize = 12 },
                Columna(new Label { Text = string.IsNullOrWhiteSpace(valor) ? "—" : valor, TextColor = Ui.Ink, FontSize = 13, HorizontalTextAlignment = TextAlignment.End }, 1)
            }
        };

    private static View Columna(View vista, int columna)
    {
        Grid.SetColumn(vista, columna);
        return vista;
    }

    private static (Color Fondo, Color Letra) ColorEstado(string tono) => tono switch
    {
        TicketConsultaTono.Ganador or TicketConsultaTono.Entregado => (Ui.Mint, Ui.Green),
        TicketConsultaTono.NoGanador or TicketConsultaTono.Vencido => (Ui.DangerBg, Ui.Danger),
        TicketConsultaTono.Pendiente => (Ui.WarnBg, Ui.Warn),
        TicketConsultaTono.Pagado => (Ui.InfoBg, Ui.Info),
        _ => (Ui.Line, Ui.Ink)
    };
}
