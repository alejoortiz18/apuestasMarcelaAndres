using System.Globalization;
using NewRich.Application.Contracts.Cierre;
using NewRich.Application.Contracts.Informes;
using NewRich.Application.Informes;
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
    private readonly VerticalStackLayout _raiz = new() { Padding = 16, Spacing = 16 };
    private readonly VerticalStackLayout _tablero = new() { Spacing = 14 };
    private readonly CargandoOverlay _cargando = new();
    private readonly HashSet<string> _elegidas = new(StringComparer.Ordinal);
    private KpiResponse? _kpi;
    private bool _filtroVendedor;
    private DateOnly _desdeFecha = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
    private DateOnly _hastaFecha = DateOnly.FromDateTime(DateTime.Today);
    private IReadOnlyList<CierreResumenResponse> _cierres = [];
    private InformeEgresosObservadorResponse? _egresos;
    private string? _errorCierres;
    private string? _errorKpi;
    private string? _errorEgresos;
    private int _paginaPremios = 1;
    private int _paginaCubiertos = 1;
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");
    private static readonly Color BarraPremio = Color.FromArgb("#1f4e45");
    private static readonly Color BarraGasto = Color.FromArgb("#8d5a2b");
    private View? _filtros;
    private IReadOnlyList<(string Nombre, Guid? Id)> _opcionesGrupo = [];
    private IReadOnlyList<(string Nombre, Guid? Id, Guid? GrupoId)> _vendedoresTodos = [];
    private IReadOnlyList<(string Nombre, Guid? Id, Guid? GrupoId)> _opcionesVendedor = [];

    public KpiPage(NewRichApiClient api)
    {
        _api = api;
        Title = PdaTexts.InformeObservador;
        BackgroundColor = Ui.Paper;
        _grupos.Title = PdaTexts.Grupo;
        _grupos.TextColor = Ui.Ink;
        _grupos.TitleColor = Ui.Muted;
        _grupos.BackgroundColor = Colors.White;
        _grupos.FontSize = 16;
        _vendedores.Title = PdaTexts.Vendedor;
        _vendedores.TextColor = Ui.Ink;
        _vendedores.TitleColor = Ui.Muted;
        _vendedores.BackgroundColor = Colors.White;
        _vendedores.FontSize = 16;
        _grupos.SelectedIndexChanged += (_, _) => AplicarVendedoresDelGrupo();
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

        var fechas = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10,
            Children =
            {
                Columna(CampoFecha(PdaTexts.Desde, _desde), 0),
                Columna(CampoFecha(PdaTexts.Hasta, _hasta), 1)
            }
        };
        var acciones = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10,
            Children = { Columna(buscar, 0), Columna(limpiar, 1) }
        };
        _filtros = new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Ui.Line,
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            Padding = 14,
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new Label { Text = PdaTexts.KpiFiltros, FontAttributes = FontAttributes.Bold, FontSize = 18, TextColor = Ui.Ink },
                    new Label { Text = PdaTexts.KpiFiltrosAyuda, TextColor = Ui.Muted, FontSize = 13, LineBreakMode = LineBreakMode.WordWrap },
                    fechas,
                    Ui.Campo(PdaTexts.Grupo),
                    _grupos,
                    Ui.Campo(PdaTexts.Vendedor),
                    _vendedores,
                    acciones
                }
            }
        };

        var formulario = new ScrollView { Content = _raiz };

        Dibujar();

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
        catch (Exception ex)
        {
            _tablero.Children.Add(new Label { Text = ex.Message, TextColor = Ui.Danger, LineBreakMode = LineBreakMode.WordWrap });
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

        _vendedoresTodos = opcionesVendedor;
        AplicarVendedoresDelGrupo();
    }

    private void AplicarVendedoresDelGrupo()
    {
        Guid? grupoId = null;
        if (_grupos.SelectedIndex >= 0 && _grupos.SelectedIndex < _opcionesGrupo.Count)
        {
            grupoId = _opcionesGrupo[_grupos.SelectedIndex].Id;
        }

        var visibles = grupoId is null
            ? _vendedoresTodos
            : _vendedoresTodos.Where(v => v.Id is null || v.GrupoId == grupoId).ToList();
        Guid? elegido = null;
        if (_vendedores.SelectedIndex >= 0 && _vendedores.SelectedIndex < _opcionesVendedor.Count)
        {
            elegido = _opcionesVendedor[_vendedores.SelectedIndex].Id;
        }

        _opcionesVendedor = visibles;
        _vendedores.ItemsSource = visibles.Select(v => v.Nombre).ToList();
        var indice = elegido is null ? 0 : visibles.ToList().FindIndex(v => v.Id == elegido);
        _vendedores.SelectedIndex = indice >= 0 ? indice : 0;
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
            _desdeFecha = desde;
            _hastaFecha = hasta;
            _filtroVendedor = vendedorId.HasValue;
            _paginaPremios = 1;
            _paginaCubiertos = 1;
            var cierres = await _api.CierresAsync(CancellationToken.None);
            if (!cierres.IsSuccess || cierres.Data is null)
            {
                _cierres = [];
                _errorCierres = cierres.Message;
            }
            else
            {
                _cierres = cierres.Data;
                _errorCierres = null;
            }

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
                _kpi = null;
                _errorKpi = resultado.Message;
            }
            else
            {
                _kpi = resultado.Data;
                _errorKpi = null;
            }

            var egresos = await _api.InformeEgresosAsync(desde, hasta, grupoId, vendedorId, CancellationToken.None);
            if (!egresos.IsSuccess || egresos.Data is null)
            {
                _egresos = null;
                _errorEgresos = egresos.Message;
            }
            else
            {
                _egresos = egresos.Data;
                _errorEgresos = null;
            }

            Dibujar();
        }
        finally
        {
            _cargando.Ocultar();
        }
    }

    private void Dibujar()
    {
        _raiz.Children.Clear();
        _raiz.Children.Add(new Label
        {
            Text = PdaTexts.InformeObservadorAyuda,
            TextColor = Ui.Muted,
            FontSize = 13,
            LineBreakMode = LineBreakMode.WordWrap
        });
        _raiz.Children.Add(new Label
        {
            Text = PdaTexts.InformeOpcionesTitulo,
            FontAttributes = FontAttributes.Bold,
            FontSize = 20,
            TextColor = Ui.Ink
        });
        _raiz.Children.Add(new Label
        {
            Text = PdaTexts.InformeOpcionesAyuda,
            TextColor = Ui.Muted,
            FontSize = 13,
            LineBreakMode = LineBreakMode.WordWrap
        });
        _raiz.Children.Add(GrillaOpciones());
        if (_elegidas.Count == 0)
        {
            _raiz.Children.Add(new Label
            {
                Text = PdaTexts.InformeFiltrosVacio,
                TextColor = Ui.Muted,
                FontSize = 14,
                LineBreakMode = LineBreakMode.WordWrap
            });
            return;
        }

        if (_elegidas.Contains("personas") || _elegidas.Contains("ventas") || _elegidas.Contains("egresos"))
        {
            if (_filtros is not null)
            {
                _raiz.Children.Add(_filtros);
            }
        }

        if (_errorKpi is not null && (_elegidas.Contains("personas") || _elegidas.Contains("ventas") || _elegidas.Contains("egresos")))
        {
            _raiz.Children.Add(new Label { Text = _errorKpi, TextColor = Ui.Danger, LineBreakMode = LineBreakMode.WordWrap });
        }
        else if (_kpi is not null)
        {
            if (_elegidas.Contains("personas"))
            {
                _raiz.Children.Add(TituloCategoria(PdaTexts.InformeObservadorPersonas));
                _raiz.Children.Add(BloquePersonas(_kpi));
            }

            if (_elegidas.Contains("ventas"))
            {
                _raiz.Children.Add(TituloCategoria(PdaTexts.InformeObservadorVentas));
                _tablero.Children.Clear();
                Pintar(_kpi, KpiTablero.De(_kpi, _desdeFecha, _hastaFecha, _filtroVendedor));
                foreach (var vista in _tablero.Children.ToList())
                {
                    _tablero.Children.Remove(vista);
                    _raiz.Children.Add(vista);
                }
            }

            if (_elegidas.Contains("egresos"))
            {
                _raiz.Children.Add(TituloCategoria(PdaTexts.InformeCategoriaEgresos));
                if (_errorEgresos is not null)
                {
                    _raiz.Children.Add(new Label { Text = _errorEgresos, TextColor = Ui.Danger, LineBreakMode = LineBreakMode.WordWrap });
                }
                else if (_egresos is not null)
                {
                    foreach (var vista in InformeEgresos(_egresos))
                    {
                        _raiz.Children.Add(vista);
                    }
                }
            }
        }

        if (_elegidas.Contains("recaudo"))
        {
            _raiz.Children.Add(TituloCategoria(PdaTexts.InformeCategoriaRecaudo));
            _raiz.Children.Add(new Label
            {
                Text = PdaTexts.InformeCategoriaRecaudoAyuda,
                TextColor = Ui.Muted,
                FontSize = 13
            });
        }

        if (_elegidas.Contains("cierres"))
        {
            _raiz.Children.Add(TituloCategoria(PdaTexts.KpiCierres));
            _raiz.Children.Add(BloqueCierres());
        }
    }

    private View GrillaOpciones()
    {
        var grilla = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12,
            RowSpacing = 12
        };
        (string Clave, string Nombre, string Ayuda)[] opciones =
        [
            ("personas", PdaTexts.InformeObservadorPersonas, PdaTexts.InformeCategoriaPersonasAyuda),
            ("ventas", PdaTexts.InformeObservadorVentas, PdaTexts.InformeCategoriaVentasAyuda),
            ("egresos", PdaTexts.InformeCategoriaEgresos, PdaTexts.InformeCategoriaEgresosAyuda),
            ("recaudo", PdaTexts.InformeCategoriaRecaudo, PdaTexts.InformeCategoriaRecaudoAyuda),
            ("cierres", PdaTexts.KpiCierres, PdaTexts.InformeCategoriaCierresAyuda)
        ];
        for (var i = 0; i < opciones.Length; i++)
        {
            var opcion = opciones[i];
            var tarjeta = TarjetaOpcion(opcion.Nombre, opcion.Ayuda, _elegidas.Contains(opcion.Clave), () =>
            {
                if (!_elegidas.Add(opcion.Clave))
                {
                    _elegidas.Remove(opcion.Clave);
                }

                Dibujar();
            });
            Grid.SetColumn(tarjeta, i % 2);
            Grid.SetRow(tarjeta, i / 2);
            grilla.Children.Add(tarjeta);
        }

        return grilla;
    }

    private static View TarjetaOpcion(string nombre, string ayuda, bool elegida, Action alPulsar)
    {
        var fondo = elegida ? Color.FromArgb("#fff3d6") : Color.FromArgb("#fffdf8");
        var borde = elegida ? Ui.Gold : Ui.Line;
        var toque = new TapGestureRecognizer();
        toque.Tapped += (_, _) => alPulsar();
        var cuerpo = new VerticalStackLayout
        {
            Spacing = 4,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Border
                {
                    BackgroundColor = elegida ? Colors.White : Color.FromArgb("#f7f1e2"),
                    StrokeThickness = 0,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
                    WidthRequest = 36,
                    HeightRequest = 36,
                    HorizontalOptions = LayoutOptions.Center
                },
                new Label
                {
                    Text = nombre,
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Ui.Ink,
                    HorizontalTextAlignment = TextAlignment.Center
                },
                new Label
                {
                    Text = ayuda,
                    FontSize = 12,
                    TextColor = Ui.Muted,
                    HorizontalTextAlignment = TextAlignment.Center,
                    LineBreakMode = LineBreakMode.WordWrap
                }
            }
        };
        if (elegida)
        {
            cuerpo.Children.Insert(0, new Label
            {
                Text = PdaTexts.InformeSeleccionada,
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#8c6b1f"),
                HorizontalTextAlignment = TextAlignment.Center
            });
        }

        var tarjeta = new Border
        {
            BackgroundColor = fondo,
            Stroke = borde,
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(12, 16),
            MinimumHeightRequest = 112,
            Content = cuerpo
        };
        tarjeta.GestureRecognizers.Add(toque);
        return tarjeta;
    }

    private static Label TituloCategoria(string texto) => new()
    {
        Text = texto.ToUpperInvariant(),
        FontSize = 22,
        FontAttributes = FontAttributes.Bold,
        TextColor = Ui.Ink,
        HorizontalTextAlignment = TextAlignment.Center
    };

    private static View BloquePersonas(KpiResponse origen)
    {
        var personas = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
            ColumnSpacing = 8,
            RowSpacing = 8
        };
        personas.Add(Celda(PdaTexts.InformeObservadorPersonas, origen.PersonasTotales.ToString(), 0, 0));
        personas.Add(Celda(PdaTexts.Vendedor, origen.Vendedores.ToString(), 1, 0));
        personas.Add(Celda(PdaTexts.Grupo, origen.GruposConVendedores.ToString(), 0, 1));
        personas.Add(Celda(PdaTexts.InformeObservadorActivos, origen.UsuariosActivos.ToString(), 1, 1));
        return Seccion(PdaTexts.InformeObservadorPersonas, personas);
    }

    private View BloqueCierres()
    {
        var contenido = new VerticalStackLayout { Spacing = 10 };
        contenido.Children.Add(new Label
        {
            Text = PdaTexts.KpiCierresAyuda,
            FontSize = 12,
            TextColor = Ui.Muted,
            LineBreakMode = LineBreakMode.WordWrap
        });
        if (_errorCierres is not null)
        {
            contenido.Children.Add(new Label { Text = _errorCierres, TextColor = Ui.Danger, FontSize = 13 });
            return Seccion(PdaTexts.KpiCierres, contenido);
        }

        var enRango = CierresObservador.EnRango(_cierres, _desdeFecha, _hastaFecha);
        var cierres = enRango.Count > 0 ? enRango : _cierres.OrderByDescending(c => c.FechaFin).ToList();
        if (enRango.Count == 0 && cierres.Count > 0)
        {
            contenido.Children.Add(new Label { Text = PdaTexts.InformeObservadorFueraDeFechas, TextColor = Ui.Ink, FontSize = 13, LineBreakMode = LineBreakMode.WordWrap });
        }
        if (cierres.Count == 0)
        {
            contenido.Children.Add(new Label { Text = PdaTexts.KpiCierresVacio, TextColor = Ui.Muted, FontSize = 14 });
        }
        else
        {
            var cronologicos = cierres.OrderBy(c => c.FechaFin).ToList();
            contenido.Children.Add(GraficoCierres(PdaTexts.KpiCierreGraficoGanancias, cronologicos, c => c.TotalGanancia, true));
            contenido.Children.Add(GraficoCierres(PdaTexts.KpiCierreGraficoGastos, cronologicos, c => c.TotalGastos, false));
            foreach (var cierre in cierres)
            {
                contenido.Children.Add(TarjetaCierre(cierre));
            }
        }

        return Seccion(PdaTexts.KpiCierres, contenido);
    }

    private static View GraficoCierres(string titulo, IReadOnlyList<CierreResumenResponse> cierres, Func<CierreResumenResponse, decimal> valor, bool ganancia)
    {
        var mayor = cierres.Count == 0 ? 0m : cierres.Max(c => Math.Abs(valor(c)));
        var lista = new VerticalStackLayout { Spacing = 12 };
        lista.Children.Add(new Label { Text = titulo, FontAttributes = FontAttributes.Bold, FontSize = 14, TextColor = Ui.Ink });
        foreach (var cierre in cierres)
        {
            var monto = valor(cierre);
            var color = ganancia && monto < 0 ? Ui.Danger : ganancia ? Ui.Green : Ui.Warn;
            var variacion = ganancia ? cierre.VariacionGanancia : cierre.VariacionGastos;
            lista.Children.Add(new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = Periodo(cierre), FontSize = 12, TextColor = Ui.Ink, LineBreakMode = LineBreakMode.WordWrap },
                    new Label { Text = FormatoDinero.Pesos(monto), FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = color },
                    Barra(mayor <= 0 ? 0 : (int)Math.Clamp(Math.Abs(monto) / mayor * 100m, 0m, 100m), color),
                    new Label
                    {
                        Text = variacion is null ? PdaTexts.KpiCierresSinAnterior : PdaTexts.KpiCierreVariacion(FormatoDinero.Pesos(variacion.Value)),
                        FontSize = 11,
                        TextColor = Ui.Muted,
                        LineBreakMode = LineBreakMode.WordWrap
                    }
                }
            });
        }

        return lista;
    }

    private static View TarjetaCierre(CierreResumenResponse cierre)
    {
        var ganancia = cierre.TotalGanancia < 0 ? Ui.Danger : Ui.Green;
        return new Border
        {
            BackgroundColor = Ui.Paper,
            Stroke = Ui.Line,
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Padding = 12,
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    FilaDato(PdaTexts.KpiCierreInicio, cierre.FechaInicio is null ? PdaTexts.KpiCierresSinAnterior : cierre.FechaInicio.Value.ToString("dd/MM/yyyy HH:mm:ss")),
                    FilaDato(PdaTexts.KpiCierreFin, cierre.FechaFin.ToString("dd/MM/yyyy HH:mm:ss")),
                    FilaDato(PdaTexts.KpiCierreDias, cierre.Dias.ToString()),
                    FilaDato(PdaTexts.KpiCierreVentas, FormatoDinero.Pesos(cierre.TotalVentas)),
                    FilaDato(PdaTexts.KpiCierreRecaudos, FormatoDinero.Pesos(cierre.TotalRecaudos)),
                    FilaDato(PdaTexts.KpiCierrePremios, FormatoDinero.Pesos(cierre.TotalPremios)),
                    FilaDato(PdaTexts.KpiCierreGastos, FormatoDinero.Pesos(cierre.TotalGastos)),
                    FilaDato(PdaTexts.KpiCierreGanancia, FormatoDinero.Pesos(cierre.TotalGanancia), ganancia)
                }
            }
        };
    }

    private static View FilaDato(string etiqueta, string valor, Color? color = null) =>
        new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(new GridLength(1.2, GridUnitType.Star)), new ColumnDefinition(new GridLength(1, GridUnitType.Star)) },
            ColumnSpacing = 8,
            Children =
            {
                new Label { Text = etiqueta, FontSize = 12, TextColor = Ui.Muted, LineBreakMode = LineBreakMode.WordWrap },
                Columna(new Label
                {
                    Text = valor,
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = color ?? Ui.Ink,
                    HorizontalTextAlignment = TextAlignment.End,
                    LineBreakMode = LineBreakMode.WordWrap
                }, 1)
            }
        };

    private static string Periodo(CierreResumenResponse cierre)
    {
        var inicio = cierre.FechaInicio is null
            ? PdaTexts.KpiCierresSinAnterior
            : cierre.FechaInicio.Value.ToString("dd/MM/yyyy HH:mm");
        return inicio + " — " + cierre.FechaFin.ToString("dd/MM/yyyy HH:mm");
    }

    private void Pintar(KpiResponse origen, KpiTablero tablero)
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
                Mini(PdaTexts.KpiValorMasAltoApostado, FormatoDinero.Pesos(tablero.ValorMasAltoApostado), 1),
                Mini(PdaTexts.KpiPicoDelPeriodo, tablero.Pico, 2)
            }
        });

        if (origen.IngresosPorGrupo.Count > 0)
        {
            _tablero.Children.Add(Seccion(PdaTexts.Grupo, BarrasGrupo(origen.IngresosPorGrupo)));
        }

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

    private static View Barra(int ancho, Color? color = null)
    {
        var lleno = Math.Clamp(ancho, 0, 100);
        var vacio = Math.Max(1, 100 - lleno);
        var relleno = new BoxView { Color = color ?? Ui.Gold, HeightRequest = 10, CornerRadius = 5 };
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

    private static View BarrasGrupo(IReadOnlyList<KpiBarraGrupoResponse> grupos)
    {
        var lista = new VerticalStackLayout { Spacing = 10 };
        foreach (var grupo in grupos.OrderByDescending(g => g.Total))
        {
            lista.Children.Add(new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = grupo.Nombre, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink },
                    new Label { Text = FormatoDinero.Pesos(grupo.Total), FontSize = 12, TextColor = Ui.Muted },
                    Barra((int)Math.Clamp(grupo.Ancho, 0m, 100m))
                }
            });
        }

        return lista;
    }

    private static View Celda(string titulo, string valor, int columna, int fila)
    {
        var vista = Mini(titulo, valor, columna);
        Grid.SetRow(vista, fila);
        return vista;
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

    private IEnumerable<View> InformeEgresos(InformeEgresosObservadorResponse informe)
    {
        yield return EncabezadoEgresos();
        yield return TablaPremios(informe.Premios);
        yield return GraficoBarras(PdaTexts.InformePremiosPorLoteria, PdaTexts.InformePremiosPorLoteriaAyuda, informe.PremiosPorLoteria, PdaTexts.InformeSinPremiosPorLoteria, PdaTexts.InformePremiosCantidad, BarraPremio);
        yield return GraficoBarras(PdaTexts.InformeCubiertoPorLoteria, PdaTexts.InformeCubiertoPorLoteriaAyuda, informe.CubiertoPorLoteria, PdaTexts.InformeSinCubiertos, PdaTexts.InformeCubiertosCantidad, BarraPremio);
        yield return GraficoBarras(PdaTexts.InformeGastosPorTipo, PdaTexts.InformeGastosPorTipoAyuda, informe.GastosPorTipo, PdaTexts.InformeSinGastos, null, BarraGasto);
        yield return ListaCubiertos(informe.Cubiertos);
        yield return Conceptos(informe);
    }

    private View EncabezadoEgresos()
    {
        var desde = _desdeFecha.ToString("dd/MM/yyyy", Cultura);
        var hasta = _hastaFecha.ToString("dd/MM/yyyy", Cultura);
        var periodo = _desdeFecha == _hastaFecha ? desde : string.Format(PdaTexts.InformeRangoTexto, desde, hasta);
        var grupo = PdaTexts.TodosLosGrupos;
        if (_grupos.SelectedIndex >= 0 && _grupos.SelectedIndex < _opcionesGrupo.Count && _opcionesGrupo[_grupos.SelectedIndex].Id is not null)
        {
            grupo = _opcionesGrupo[_grupos.SelectedIndex].Nombre;
        }

        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label
                {
                    Text = PdaTexts.InformeEgresosNombre.ToUpperInvariant(),
                    FontSize = 22,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Ui.Ink
                },
                new Label
                {
                    Text = string.Format(PdaTexts.InformeCorte, DateTime.Now.ToString("dd/MM/yyyy hh:mm tt", Cultura)),
                    TextColor = Ui.Muted,
                    FontSize = 13
                },
                Pastilla(string.Format(PdaTexts.InformePeriodoTexto, periodo)),
                Pastilla(string.Format(PdaTexts.InformeAlcanceTexto, grupo))
            }
        };
    }

    private View TablaPremios(IReadOnlyList<PremioInformeFilaResponse> premios)
    {
        var cuerpo = new VerticalStackLayout { Spacing = 8 };
        cuerpo.Children.Add(FilasPorPagina());
        cuerpo.Children.Add(Ayuda(PdaTexts.InformePremiosWebAyuda));
        if (premios.Count == 0)
        {
            cuerpo.Children.Add(Vacio(PdaTexts.InformeSinPremiosWeb));
            return Seccion(PdaTexts.InformePremiosWeb, cuerpo);
        }

        var pagina = PaginaActual(_paginaPremios, premios.Count);
        _paginaPremios = pagina;
        foreach (var premio in premios.Skip((pagina - 1) * 10).Take(10))
        {
            cuerpo.Children.Add(TarjetaDatos(
            [
                (PdaTexts.Fecha, premio.Fecha.ToString("dd/MM/yyyy", Cultura)),
                (PdaTexts.Vendedor, premio.Vendedor),
                (PdaTexts.InformeColumnaEntrega, premio.Entrega),
                (PdaTexts.Loteria, premio.Loteria),
                (PdaTexts.InformeColumnaNumero, premio.Numero),
                (PdaTexts.ValorApostado, Pesos(premio.ValorApostado)),
                (PdaTexts.InformeColumnaValorPremio, Pesos(premio.ValorPremio))
            ]));
        }

        cuerpo.Children.Add(Paginador(premios.Count, pagina, n =>
        {
            _paginaPremios = n;
            Dibujar();
        }));
        return Seccion(PdaTexts.InformePremiosWeb, cuerpo);
    }

    private View ListaCubiertos(IReadOnlyList<CubiertoInformeFilaResponse> cubiertos)
    {
        var cuerpo = new VerticalStackLayout { Spacing = 8 };
        cuerpo.Children.Add(FilasPorPagina());
        cuerpo.Children.Add(Ayuda(PdaTexts.InformeCubiertosAyuda));
        if (cubiertos.Count == 0)
        {
            cuerpo.Children.Add(Vacio(PdaTexts.InformeSinCubiertos));
            return Seccion(PdaTexts.InformeCubiertos, cuerpo);
        }

        var pagina = PaginaActual(_paginaCubiertos, cubiertos.Count);
        _paginaCubiertos = pagina;
        foreach (var cubierto in cubiertos.Skip((pagina - 1) * 10).Take(10))
        {
            cuerpo.Children.Add(TarjetaDatos(
            [
                (PdaTexts.Fecha, cubierto.Fecha.ToString("dd/MM/yyyy", Cultura)),
                (PdaTexts.Loteria, cubierto.Loteria),
                (PdaTexts.InformeColumnaNumero, cubierto.Numero),
                (PdaTexts.InformeColumnaQuienCubrio, cubierto.CubiertoPor),
                (PdaTexts.Subtotal, Pesos(cubierto.Subtotal)),
                (PdaTexts.Descuento, $"{Pesos(cubierto.Descuento)} ({Porcentaje(cubierto.DescuentoPorcentaje)} %)"),
                (PdaTexts.InformeColumnaValorPagado, Pesos(cubierto.Pagado))
            ]));
        }

        cuerpo.Children.Add(Paginador(cubiertos.Count, pagina, n =>
        {
            _paginaCubiertos = n;
            Dibujar();
        }));
        return Seccion(PdaTexts.InformeCubiertos, cuerpo);
    }

    private static View GraficoBarras(string titulo, string ayuda, IReadOnlyList<BarraInformeResponse> barras, string vacio, string? cantidadFormato, Color color)
    {
        var cuerpo = new VerticalStackLayout { Spacing = 12 };
        cuerpo.Children.Add(Ayuda(ayuda));
        if (barras.Count == 0)
        {
            cuerpo.Children.Add(Vacio(vacio));
            return Seccion(titulo, cuerpo);
        }

        foreach (var barra in barras)
        {
            var nombre = new FormattedString();
            nombre.Spans.Add(new Span { Text = barra.Texto, TextColor = Ui.Ink, FontSize = 14 });
            if (cantidadFormato is not null)
            {
                nombre.Spans.Add(new Span
                {
                    Text = "  " + string.Format(cantidadFormato, barra.Cantidad.ToString("N0", Cultura)),
                    TextColor = Ui.Muted,
                    FontSize = 12
                });
            }

            var valor = new FormattedString();
            valor.Spans.Add(new Span { Text = Pesos(barra.Valor), TextColor = Ui.Ink, FontSize = 14, FontAttributes = FontAttributes.Bold });
            valor.Spans.Add(new Span { Text = "  " + Porcentaje(barra.Porcentaje) + " %", TextColor = Ui.Muted, FontSize = 12 });
            cuerpo.Children.Add(new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Grid
                    {
                        ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                        ColumnSpacing = 8,
                        Children =
                        {
                            new Label { FormattedText = nombre, LineBreakMode = LineBreakMode.WordWrap },
                            Columna(new Label { FormattedText = valor, HorizontalTextAlignment = TextAlignment.End }, 1)
                        }
                    },
                    Barra((int)Math.Clamp(Math.Round(barra.Ancho, MidpointRounding.AwayFromZero), 0m, 100m), color)
                }
            });
        }

        return Seccion(titulo, cuerpo);
    }

    private static View Conceptos(InformeEgresosObservadorResponse informe)
    {
        var partes = new (string Texto, decimal Valor, int Cantidad, Color Color)[]
        {
            (PdaTexts.InformeConceptoPremios, informe.PremiosPorLoteria.Sum(p => p.Valor), informe.PremiosPorLoteria.Sum(p => p.Cantidad), BarraPremio),
            (PdaTexts.InformeConceptoGastos, informe.GastosPorTipo.Sum(p => p.Valor), informe.GastosPorTipo.Sum(p => p.Cantidad), BarraGasto),
            (PdaTexts.InformeConceptoCubierto, informe.CubiertoPorLoteria.Sum(p => p.Valor), informe.CubiertoPorLoteria.Sum(p => p.Cantidad), Ui.Gold)
        };
        var conValor = partes.Where(p => p.Valor > 0).ToList();
        var cuerpo = new VerticalStackLayout { Spacing = 10 };
        cuerpo.Children.Add(Ayuda(PdaTexts.InformeEgresosPorConceptoAyuda));
        if (conValor.Count == 0)
        {
            cuerpo.Children.Add(Vacio(PdaTexts.InformeSinEgresos));
            return Seccion(PdaTexts.InformeEgresosPorConcepto, cuerpo);
        }

        var barras = InformeEgresosLectura.Barras(conValor.Select(p => (p.Texto, p.Valor, p.Cantidad)).ToList());
        foreach (var barra in barras)
        {
            var color = conValor.First(p => p.Texto == barra.Texto).Color;
            cuerpo.Children.Add(new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 8,
                Children =
                {
                    new BoxView { Color = color, WidthRequest = 12, HeightRequest = 12, CornerRadius = 6, VerticalOptions = LayoutOptions.Center },
                    Columna(new Label { Text = barra.Texto, TextColor = Ui.Ink, FontSize = 14, VerticalOptions = LayoutOptions.Center }, 1),
                    Columna(new Label
                    {
                        Text = $"{Pesos(barra.Valor)}  {Porcentaje(barra.Porcentaje)} %",
                        TextColor = Ui.Ink,
                        FontSize = 13,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.End
                    }, 2)
                }
            });
        }

        return Seccion(PdaTexts.InformeEgresosPorConcepto, cuerpo);
    }

    private static View FilasPorPagina() =>
        new Label
        {
            Text = $"{PdaTexts.FilasPorPagina}  10",
            TextColor = Ui.Muted,
            FontSize = 12,
            HorizontalTextAlignment = TextAlignment.End
        };

    private static View Pastilla(string texto) =>
        new Border
        {
            BackgroundColor = Ui.Crema,
            Stroke = Ui.Gold,
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            Padding = new Thickness(12, 6),
            HorizontalOptions = LayoutOptions.Start,
            Content = new Label { Text = texto, TextColor = Ui.Ink, FontSize = 12 }
        };

    private static Label Ayuda(string texto) =>
        new() { Text = texto, TextColor = Ui.Muted, FontSize = 12, LineBreakMode = LineBreakMode.WordWrap };

    private static Label Vacio(string texto) =>
        new() { Text = texto, TextColor = Ui.Muted, FontSize = 13, LineBreakMode = LineBreakMode.WordWrap };

    private static View TarjetaDatos(IReadOnlyList<(string Etiqueta, string Valor)> filas)
    {
        var lista = new VerticalStackLayout { Spacing = 4 };
        foreach (var (etiqueta, valor) in filas)
        {
            lista.Children.Add(new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(new GridLength(130)), new ColumnDefinition(GridLength.Star) },
                ColumnSpacing = 8,
                Children =
                {
                    new Label { Text = etiqueta, TextColor = Ui.Muted, FontSize = 12 },
                    Columna(new Label { Text = valor, TextColor = Ui.Ink, FontSize = 14, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap }, 1)
                }
            });
        }

        return new Border
        {
            BackgroundColor = Ui.Paper,
            Stroke = Ui.Line,
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
            Padding = 10,
            Content = lista
        };
    }

    private View Paginador(int total, int pagina, Action<int> ir)
    {
        var paginas = Math.Max(1, (int)Math.Ceiling(total / 10d));
        var inicio = ((pagina - 1) * 10) + 1;
        var fin = Math.Min(total, pagina * 10);
        var botones = new FlexLayout
        {
            Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap,
            Direction = Microsoft.Maui.Layouts.FlexDirection.Row,
            JustifyContent = Microsoft.Maui.Layouts.FlexJustify.End,
            AlignItems = Microsoft.Maui.Layouts.FlexAlignItems.Center
        };
        botones.Children.Add(BotonPagina(PdaTexts.Inicio, pagina > 1, () => ir(1)));
        botones.Children.Add(BotonPagina(PdaTexts.Anterior, pagina > 1, () => ir(pagina - 1)));
        botones.Children.Add(new Border
        {
            BackgroundColor = Ui.Dark,
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(12, 8),
            Margin = new Thickness(4, 4, 0, 0),
            Content = new Label { Text = pagina.ToString(Cultura), TextColor = Colors.White, FontSize = 12, FontAttributes = FontAttributes.Bold }
        });
        botones.Children.Add(BotonPagina(PdaTexts.Siguiente, pagina < paginas, () => ir(pagina + 1)));
        botones.Children.Add(BotonPagina(PdaTexts.Ultimo, pagina < paginas, () => ir(paginas)));
        return new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label
                {
                    Text = string.Format(PdaTexts.MostrandoFormato, inicio, fin, total),
                    TextColor = Ui.Muted,
                    FontSize = 12
                },
                botones
            }
        };
    }

    private static View BotonPagina(string texto, bool activo, Action pulsar)
    {
        var boton = new Button
        {
            Text = texto,
            FontSize = 12,
            TextColor = activo ? Ui.Ink : Ui.Muted,
            BackgroundColor = Colors.White,
            BorderColor = Ui.Line,
            BorderWidth = 1,
            CornerRadius = 8,
            Padding = new Thickness(10, 6),
            Margin = new Thickness(4, 4, 0, 0),
            IsEnabled = activo
        };
        if (activo)
        {
            boton.Clicked += (_, _) => MainThread.BeginInvokeOnMainThread(pulsar);
        }

        return boton;
    }

    private static int PaginaActual(int pagina, int total)
    {
        var paginas = Math.Max(1, (int)Math.Ceiling(total / 10d));
        return Math.Clamp(pagina, 1, paginas);
    }

    private static string Pesos(decimal valor) => valor.ToString("C0", Cultura);

    private static string Porcentaje(decimal valor) => valor.ToString("0.#", Cultura);

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

    private static View CampoFecha(string etiqueta, DatePicker fecha)
    {
        fecha.TextColor = Ui.Ink;
        fecha.FontSize = 16;
        fecha.BackgroundColor = Colors.White;
        return new VerticalStackLayout
        {
            Spacing = 6,
            Children = { Ui.Campo(etiqueta), fecha }
        };
    }

}
