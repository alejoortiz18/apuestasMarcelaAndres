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
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children =
                        {
                            new Label
                            {
                                Text = vista.Estado,
                                FontAttributes = FontAttributes.Bold,
                                FontSize = 15,
                                TextColor = letraEstado,
                                HorizontalTextAlignment = TextAlignment.Center
                            },
                            new Label
                            {
                                Text = vista.Mensaje,
                                FontSize = 13,
                                TextColor = letraEstado,
                                HorizontalTextAlignment = TextAlignment.Center
                            }
                        }
                    }
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
            juegos.Children.Add(new Label
            {
                Text = $"{juego.Numero} · {juego.Loterias} · {FormatoDinero.Pesos(juego.Valor)}",
                TextColor = Ui.Ink,
                FontSize = 13
            });
        }

        if (vista.Juegos.Count == 0)
        {
            juegos.Children.Add(new Label { Text = PdaTexts.SinJuegos, TextColor = Ui.Muted, FontSize = 13 });
        }

        cuerpo.Children.Add(juegos);
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
