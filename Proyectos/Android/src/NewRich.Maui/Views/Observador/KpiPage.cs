using NewRich.Application.Contracts.Kpi;
using NewRich.Domain.Enums;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Maui.Views;

namespace NewRich.Maui.Views.Observador;

public sealed class KpiPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly DatePicker _desde = new() { Date = DateTime.Today.AddDays(-7) };
    private readonly DatePicker _hasta = new() { Date = DateTime.Today };
    private readonly Picker _grupos = new();
    private readonly Picker _vendedores = new();
    private readonly VerticalStackLayout _tablero = new() { Spacing = 14 };
    private readonly CargandoOverlay _cargando = new();
    private IReadOnlyList<(string Nombre, Guid? Id)> _opcionesGrupo = [];
    private IReadOnlyList<(string Nombre, Guid? Id, Guid? GrupoId)> _opcionesVendedor = [];

    public KpiPage(NewRichApiClient api)
    {
        _api = api;
        Title = PdaTexts.Kpi;
        BackgroundColor = Ui.Paper;
        var buscar = Ui.Primario(PdaTexts.Buscar);
        buscar.Clicked += async (_, _) => await CargarAsync();
        var limpiar = Ui.Secundario(PdaTexts.Limpiar);
        limpiar.Clicked += async (_, _) =>
        {
            _desde.Date = DateTime.Today.AddDays(-7);
            _hasta.Date = DateTime.Today;
            _grupos.SelectedIndex = 0;
            _vendedores.SelectedIndex = 0;
            await CargarAsync();
        };

        var formulario = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 12,
                Children =
                {
                    new Label { Text = PdaTexts.KpiAyudaTablero, TextColor = Ui.Muted, FontSize = 13, LineBreakMode = LineBreakMode.WordWrap },
                    Ui.Campo(PdaTexts.Desde),
                    _desde,
                    Ui.Campo(PdaTexts.Hasta),
                    _hasta,
                    Ui.Campo(PdaTexts.Grupo),
                    _grupos,
                    Ui.Campo(PdaTexts.Vendedor),
                    _vendedores,
                    buscar,
                    limpiar,
                    _tablero
                }
            }
        };

        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Star) },
            Children = { formulario, _cargando }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await CargarFiltrosAsync();
            await CargarAsync();
        }
        catch (Exception)
        {
        }
    }

    private async Task CargarFiltrosAsync()
    {
        var grupos = await _api.GruposAsync(CancellationToken.None);
        var opcionesGrupo = new List<(string Nombre, Guid? Id)> { (PdaTexts.FiltroGeneral, null) };
        if (grupos.IsSuccess && grupos.Data is not null)
        {
            opcionesGrupo.AddRange(grupos.Data.Select(g => (g.Nombre, (Guid?)g.GrupoId)));
        }

        _opcionesGrupo = opcionesGrupo;
        _grupos.ItemsSource = opcionesGrupo.Select(x => x.Nombre).ToList();
        if (_grupos.SelectedIndex < 0)
        {
            _grupos.SelectedIndex = 0;
        }

        var usuarios = await _api.UsuariosAsync(CancellationToken.None);
        var opcionesVendedor = new List<(string Nombre, Guid? Id, Guid? GrupoId)> { (PdaTexts.FiltroGeneral, null, null) };
        if (usuarios.IsSuccess && usuarios.Data is not null)
        {
            opcionesVendedor.AddRange(usuarios.Data
                .Where(u => u.Rol == RolUsuario.Vendedor)
                .OrderBy(u => u.NombreCompleto)
                .Select(u => (u.NombreCompleto, (Guid?)u.UsuarioId, u.GrupoId)));
        }

        _opcionesVendedor = opcionesVendedor;
        _vendedores.ItemsSource = opcionesVendedor.Select(x => x.Nombre).ToList();
        if (_vendedores.SelectedIndex < 0)
        {
            _vendedores.SelectedIndex = 0;
        }
    }

    private async Task CargarAsync()
    {
        _tablero.Children.Clear();
        var desde = DateOnly.FromDateTime((_desde.Date ?? DateTime.Today.AddDays(-7)).Date);
        var hasta = DateOnly.FromDateTime((_hasta.Date ?? DateTime.Today).Date);
        Guid? grupoId = null;
        if (_grupos.SelectedIndex >= 0 && _grupos.SelectedIndex < _opcionesGrupo.Count)
        {
            grupoId = _opcionesGrupo[_grupos.SelectedIndex].Id;
        }

        Guid? vendedorId = null;
        if (_vendedores.SelectedIndex >= 0 && _vendedores.SelectedIndex < _opcionesVendedor.Count)
        {
            vendedorId = _opcionesVendedor[_vendedores.SelectedIndex].Id;
        }

        _cargando.Mostrar(PdaTexts.Cargando);
        try
        {
            var resultado = await _api.KpiAsync(new KpiRequest
            {
                GrupoId = grupoId,
                VendedorId = vendedorId,
                FechaInicial = desde.ToDateTime(TimeOnly.MinValue),
                FechaFinal = hasta.ToDateTime(TimeOnly.MaxValue),
                CompararAnterior = true
            }, CancellationToken.None);
            if (!resultado.IsSuccess || resultado.Data is null)
            {
                _tablero.Children.Add(new Label { Text = resultado.Message, TextColor = Ui.Danger });
                return;
            }

            Pintar(KpiTablero.De(resultado.Data, desde, hasta, vendedorId.HasValue));
        }
        finally
        {
            _cargando.Ocultar();
        }
    }

    private void Pintar(KpiTablero tablero)
    {
        var tonoVariacion = tablero.VariacionPositiva ? Ui.Green : Ui.Danger;
        _tablero.Children.Add(new Frame
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
                    new Label { Text = PdaTexts.KpiVentasDelPeriodo, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Ui.Ink },
                    new Label { Text = FormatoDinero.Pesos(tablero.Ingresos), FontSize = 30, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink },
                    new Label { Text = $"{tablero.VariacionTexto} {PdaTexts.KpiFrenteAnterior}", FontSize = 13, TextColor = tonoVariacion, FontAttributes = FontAttributes.Bold },
                    new Label { Text = tablero.Lectura, FontSize = 14, TextColor = Ui.Ink, LineBreakMode = LineBreakMode.WordWrap }
                }
            }
        });

        _tablero.Children.Add(new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) },
            ColumnSpacing = 8,
            Children =
            {
                Mini(PdaTexts.ConsultaVentas, tablero.Ventas.ToString(), 0),
                Mini(PdaTexts.KpiTicketPromedio, FormatoDinero.Pesos(tablero.TicketPromedio), 1),
                Mini(PdaTexts.KpiPicoDelPeriodo, tablero.Pico, 2)
            }
        });

        _tablero.Children.Add(Seccion(PdaTexts.KpiComoSeMovieron, Grafico(tablero.Serie)));
        if (_vendedores.SelectedIndex > 0)
        {
            _tablero.Children.Add(Seccion(PdaTexts.KpiMovimientos, ListaMovimientos(tablero.Serie)));
        }
        else
        {
            _tablero.Children.Add(Seccion(PdaTexts.KpiPorVendedor, Ranking(tablero.Ranking)));
            _tablero.Children.Add(Seccion(PdaTexts.KpiMovimientos, ListaMovimientos(tablero.Serie)));
        }
    }

    private static View Grafico(IReadOnlyList<KpiPuntoDia> serie)
    {
        if (serie.Count == 0)
        {
            return new Label { Text = PdaTexts.KpiSinMovimientos, TextColor = Ui.Muted, FontSize = 13 };
        }

        var fila = new HorizontalStackLayout { Spacing = 6, Padding = new Thickness(2, 6, 2, 0) };
        foreach (var punto in serie)
        {
            fila.Children.Add(new VerticalStackLayout
            {
                Spacing = 4,
                WidthRequest = 22,
                HeightRequest = 132,
                VerticalOptions = LayoutOptions.End,
                Children =
                {
                    new BoxView
                    {
                        Color = punto.Total > 0 ? Ui.Gold : Ui.Line,
                        HeightRequest = Math.Max(6, punto.Alto * 1.1),
                        CornerRadius = 3,
                        VerticalOptions = LayoutOptions.End
                    },
                    new Label
                    {
                        Text = punto.Fecha.Day.ToString(),
                        FontSize = 9,
                        TextColor = Ui.Muted,
                        HorizontalTextAlignment = TextAlignment.Center
                    }
                }
            });
        }

        return new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HeightRequest = 148,
            Content = fila
        };
    }

    private static View Ranking(IReadOnlyList<KpiBarraVendedor> ranking)
    {
        if (ranking.Count == 0)
        {
            return new Label { Text = PdaTexts.KpiSinMovimientos, TextColor = Ui.Muted, FontSize = 13 };
        }

        var lista = new VerticalStackLayout { Spacing = 10 };
        foreach (var fila in ranking)
        {
            lista.Children.Add(new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = $"{fila.Nombre} · {fila.Grupo}", FontSize = 13, TextColor = Ui.Ink, FontAttributes = FontAttributes.Bold },
                    new Label { Text = $"{fila.Ventas} {PdaTexts.ConsultaVentas.ToLowerInvariant()} · {FormatoDinero.Pesos(fila.Total)}", FontSize = 12, TextColor = Ui.Muted },
                    Barra(fila.Ancho)
                }
            });
        }

        return lista;
    }

    private static View ListaMovimientos(IReadOnlyList<KpiPuntoDia> serie)
    {
        var conVenta = serie.Where(x => x.Total > 0).ToList();
        if (conVenta.Count == 0)
        {
            return new Label { Text = PdaTexts.KpiSinMovimientos, TextColor = Ui.Muted, FontSize = 13 };
        }

        var lista = new VerticalStackLayout { Spacing = 8 };
        foreach (var dia in conVenta)
        {
            lista.Children.Add(new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                Children =
                {
                    new Label
                    {
                        Text = $"{dia.Fecha.Day:00}/{dia.Fecha.Month:00}/{dia.Fecha.Year} · {dia.Ventas} {PdaTexts.ConsultaVentas.ToLowerInvariant()}",
                        TextColor = Ui.Ink,
                        FontSize = 13
                    },
                    Columna(new Label
                    {
                        Text = FormatoDinero.Pesos(dia.Total),
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Ui.Ink,
                        FontSize = 13
                    }, 1)
                }
            });
        }

        return lista;
    }

    private static View Barra(int ancho)
    {
        var lleno = Math.Clamp(ancho, 0, 100);
        var vacio = Math.Max(1, 100 - lleno);
        var relleno = new BoxView { Color = Ui.Gold, HeightRequest = 10, CornerRadius = 5 };
        var pista = new BoxView { Color = Ui.Line, HeightRequest = 10, CornerRadius = 5 };
        var grilla = new Grid
        {
            ColumnSpacing = 0,
            HeightRequest = 10,
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(Math.Max(lleno, 1), GridUnitType.Star)),
                new ColumnDefinition(new GridLength(vacio, GridUnitType.Star))
            }
        };
        Grid.SetColumn(relleno, 0);
        Grid.SetColumn(pista, 1);
        if (lleno == 0)
        {
            grilla.Add(pista);
            return grilla;
        }

        grilla.Add(relleno);
        if (lleno < 100)
        {
            grilla.Add(pista);
        }

        return grilla;
    }

    private static Frame Mini(string titulo, string valor, int col)
    {
        var frame = new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = Ui.Line,
            CornerRadius = 12,
            Padding = 12,
            Content = new VerticalStackLayout
            {
                Children =
                {
                    new Label { Text = titulo, FontSize = 11, TextColor = Ui.Muted },
                    new Label { Text = valor, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink, LineBreakMode = LineBreakMode.TailTruncation }
                }
            }
        };
        Grid.SetColumn(frame, col);
        return frame;
    }

    private static Frame Seccion(string titulo, View contenido) =>
        new()
        {
            BackgroundColor = Colors.White,
            BorderColor = Ui.Line,
            CornerRadius = 12,
            Padding = 16,
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new Label { Text = titulo, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink, FontSize = 15 },
                    contenido
                }
            }
        };

    private static View Columna(View vista, int columna)
    {
        Grid.SetColumn(vista, columna);
        return vista;
    }
}
