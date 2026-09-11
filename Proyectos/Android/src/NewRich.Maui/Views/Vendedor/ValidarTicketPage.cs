using Microsoft.Maui.Controls.Shapes;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Premios;
using NewRich.Constants;
using NewRich.Maui.Services;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;

namespace NewRich.Maui.Views.Vendedor;

public sealed class ValidarTicketPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly ILectorCodigoBarrasServicio _lector;
    private readonly Entry _codigo;
    private readonly Label _avisoLector;
    private readonly Label _aviso;
    private readonly Border _recibo;
    private readonly VerticalStackLayout _cuerpoRecibo;
    private readonly Button _reportar;
    private bool _escuchando;

    public ValidarTicketPage(NewRichApiClient api, ILectorCodigoBarrasServicio lector)
    {
        _api = api;
        _lector = lector;
        Title = PdaTexts.ValidarTicket;
        BackgroundColor = Color.FromArgb("#eef4f0");
        _codigo = Ui.Entrada("AOL-0000001");
        _codigo.MaxLength = 400;
        _codigo.ReturnType = ReturnType.Go;
        _codigo.Completed += async (_, _) => await ValidarAsync();
        _avisoLector = new Label
        {
            Text = PdaTexts.EsperandoLector,
            TextColor = Ui.Muted,
            FontSize = 13,
            IsVisible = false
        };
        _aviso = new Label { FontSize = 13, TextColor = Ui.Ink };
        _cuerpoRecibo = new VerticalStackLayout { Spacing = 0 };
        _recibo = new Border
        {
            Stroke = Color.FromArgb("#c9a44a"),
            StrokeThickness = 1.5,
            BackgroundColor = Color.FromArgb("#fffaf0"),
            StrokeShape = new RoundRectangle { CornerRadius = 4 },
            Padding = 0,
            IsVisible = false,
            Content = _cuerpoRecibo,
            Shadow = new Shadow
            {
                Brush = Color.FromArgb("#1a123d35"),
                Offset = new Point(0, 6),
                Radius = 16,
                Opacity = 0.35f
            }
        };
        _reportar = Ui.Primario(PdaTexts.ReportarCaso);
        _reportar.IsVisible = false;
        _reportar.Clicked += async (_, _) => await ReportarAsync();
        var leer = Ui.Primario(PdaTexts.LeerCodigoBarras);
        leer.Clicked += (_, _) => DispararLector();
        var validar = Ui.Secundario(PdaTexts.ValidarTicket);
        validar.Clicked += async (_, _) => await ValidarAsync();
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 18, 16, 28),
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        Text = PdaTexts.ValidarTicketAyuda,
                        TextColor = Ui.Muted,
                        FontSize = 14,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    Ui.Campo(PdaTexts.TicketCode),
                    _codigo,
                    leer,
                    validar,
                    _avisoLector,
                    _recibo,
                    _reportar,
                    _aviso
                }
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_escuchando)
        {
            return;
        }

        _lector.CodigoLeido += AlLeerCodigo;
        _lector.Activar();
        _escuchando = true;
        _codigo.Focus();
    }

    protected override void OnDisappearing()
    {
        if (_escuchando)
        {
            _lector.CodigoLeido -= AlLeerCodigo;
            _lector.Desactivar();
            _escuchando = false;
        }

        base.OnDisappearing();
    }

    private void DispararLector()
    {
        _avisoLector.IsVisible = true;
        _aviso.Text = string.Empty;
        _codigo.Focus();
        _lector.Disparar();
    }

    private async void AlLeerCodigo(object? sender, string codigo)
    {
        _avisoLector.IsVisible = false;
        _codigo.Text = codigo;
        await ValidarAsync();
    }

    private async Task ValidarAsync()
    {
        _reportar.IsVisible = false;
        _recibo.IsVisible = false;
        _cuerpoRecibo.Children.Clear();
        var consulta = await _api.ConsultarTicketAsync(new ConsultaTicketRequest
        {
            TicketCode = _codigo.Text?.Trim() ?? string.Empty
        }, CancellationToken.None);
        if (!consulta.IsSuccess || consulta.Data is null)
        {
            _aviso.Text = string.IsNullOrWhiteSpace(consulta.Message) ? PdaTexts.CodigoNoLeido : consulta.Message;
            _aviso.TextColor = Ui.Danger;
            return;
        }

        PintarRecibo(TicketConsultaVista.De(consulta.Data));
        _aviso.Text = string.Empty;
    }

    private async Task ReportarAsync()
    {
        var resultado = await _api.ReportarPremioAsync(new ReportarCasoGanadorRequest
        {
            TicketCode = _codigo.Text?.Trim() ?? string.Empty
        }, CancellationToken.None);
        _aviso.Text = resultado.IsSuccess
            ? $"{resultado.Message} {resultado.Data?.Ticket}"
            : resultado.Message;
        _aviso.TextColor = resultado.IsSuccess ? Ui.Green : Ui.Danger;
        if (resultado.IsSuccess)
        {
            _reportar.IsVisible = false;
        }
    }

    private void PintarRecibo(TicketConsultaVista vista)
    {
        var (fondoEstado, letraEstado) = ColorEstado(vista.Tono);
        _cuerpoRecibo.Children.Add(CabeceraRecibo());
        _cuerpoRecibo.Children.Add(new BoxView { Color = Color.FromArgb("#c9a44a"), HeightRequest = 3 });
        _cuerpoRecibo.Children.Add(new VerticalStackLayout
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
                Fila(PdaTexts.Fecha, vista.Fecha == default ? "—" : vista.Fecha.ToLocalTime().ToString("dd/MM/yyyy HH:mm")),
                new BoxView { Color = Color.FromArgb("#e6d7a8"), HeightRequest = 1, Margin = new Thickness(0, 4) }
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
                    Celda(juego.Valor.ToString("N0"), 2, true)
                }
            });
        }

        if (vista.Juegos.Count == 0)
        {
            juegos.Children.Add(new Label { Text = PdaTexts.SinJuegos, TextColor = Ui.Muted, FontSize = 13 });
        }

        _cuerpoRecibo.Children.Add(juegos);
        _cuerpoRecibo.Children.Add(new BoxView { Color = Color.FromArgb("#e6d7a8"), HeightRequest = 1, Margin = new Thickness(18, 4) });
        _cuerpoRecibo.Children.Add(new Grid
        {
            Padding = new Thickness(18, 12, 18, 18),
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            Children =
            {
                Colocar(new Label
                {
                    Text = PdaTexts.TotalApostado,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 14,
                    TextColor = Ui.Dark
                }, 0),
                Colocar(new Label
                {
                    Text = vista.Total.ToString("N0"),
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 20,
                    TextColor = Ui.Dark,
                    HorizontalTextAlignment = TextAlignment.End
                }, 1)
            }
        });

        _recibo.IsVisible = true;
        _reportar.IsVisible = vista.PuedeReportar;
    }

    private static Grid CabeceraRecibo()
    {
        var marca = new Label
        {
            Text = PdaTexts.Marca,
            TextColor = Color.FromArgb("#f0d078"),
            FontSize = 11,
            CharacterSpacing = 3,
            FontAttributes = FontAttributes.Bold
        };
        var titulo = new Label
        {
            Text = PdaTexts.ReciboDeVenta,
            TextColor = Colors.White,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold
        };
        Grid.SetRow(titulo, 1);
        return new Grid
        {
            BackgroundColor = Ui.Dark,
            Padding = new Thickness(18, 16),
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
            RowSpacing = 6,
            Children = { marca, titulo }
        };
    }

    private static View Colocar(View vista, int columna)
    {
        Grid.SetColumn(vista, columna);
        return vista;
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

    private static Grid Fila(string etiqueta, string valor)
    {
        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }
        };
        grid.Add(new Label { Text = etiqueta, FontSize = 12, TextColor = Ui.Muted }, 0);
        grid.Add(new Label
        {
            Text = string.IsNullOrWhiteSpace(valor) ? "—" : valor,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Ui.Ink,
            HorizontalTextAlignment = TextAlignment.End
        }, 1);
        return grid;
    }

    private static (Color Fondo, Color Letra) ColorEstado(string tono) => tono switch
    {
        TicketConsultaTono.Ganador or TicketConsultaTono.Entregado => (Ui.Mint, Ui.Green),
        TicketConsultaTono.NoGanador or TicketConsultaTono.Vencido => (Ui.DangerBg, Ui.Danger),
        TicketConsultaTono.Pendiente => (Ui.WarnBg, Ui.Warn),
        TicketConsultaTono.Pagado => (Ui.InfoBg, Ui.Info),
        _ => (Ui.DangerBg, Ui.Danger)
    };
}
