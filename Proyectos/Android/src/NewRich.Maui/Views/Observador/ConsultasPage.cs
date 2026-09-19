using Microsoft.Maui.Controls.Shapes;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Resultados;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Application.Contracts.Ventas;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Maui.Views;

namespace NewRich.Maui.Views.Observador;

public sealed class ConsultasPage : ContentPage
{
    /// <summary>Una fila del listado de resultados, ya lista para pintar.</summary>
    private sealed record Fila(
        string Titulo,
        string Detalle,
        string? Monto,
        string? Estado,
        bool Ganador = false,
        BoletoListaResponse? Boleto = null,
        ResultadoResponse? Resultado = null);

    private static readonly IReadOnlyList<string> Tipos =
    [
        PdaTexts.ConsultaBoletos,
        PdaTexts.ConsultaVentas,
        PdaTexts.ConsultaVendedores,
        PdaTexts.ConsultaDispositivos,
        PdaTexts.ConsultaResultados,
        PdaTexts.ConsultaConfiguracion
    ];

    private readonly NewRichApiClient _api;
    private readonly Entry _codigo = Ui.Entrada(PdaTexts.TicketCode);
    private readonly Entry _numero = Ui.Entero(PdaTexts.NumeroApostado);
    private readonly Picker _estado = new();
    private readonly Picker _tipoApuesta = new();
    private readonly Picker _loteria = new();
    private readonly Picker _grupo = new();
    private readonly Picker _vendedor = new();
    private readonly Picker _conectado = new();
    private readonly DatePicker _fecha = new() { Date = DateTime.Today };
    private readonly DatePicker _fechaDesde = new() { Date = DateTime.Today.AddDays(-7) };
    private readonly DatePicker _fechaHasta = new() { Date = DateTime.Today };
    private readonly Picker _filas = new() { ItemsSource = Paginacion.OpcionesFilas.Cast<object>().ToList(), SelectedIndex = 1 };
    private readonly HorizontalStackLayout _fichas = new() { Spacing = 8 };
    private readonly VerticalStackLayout _lista = new() { Spacing = 10 };
    private readonly Grid _filtros = new() { ColumnSpacing = 10, RowSpacing = 10 };
    private readonly Border _resumenVentas;
    private readonly Label _resumenVentasTitulo = new()
    {
        Text = PdaTexts.TotalFiltroVentas,
        TextColor = Ui.Dark,
        FontSize = 12,
        FontAttributes = FontAttributes.Bold
    };
    private readonly Label _resumenVentasValor = new()
    {
        Text = FormatoDinero.Pesos(0m),
        TextColor = Ui.Dark,
        FontSize = 30,
        FontAttributes = FontAttributes.Bold
    };
    private readonly Label _resumen = new() { FontSize = 12, TextColor = Ui.Muted, VerticalOptions = LayoutOptions.Center };
    private readonly Label _ayudaGanador = new()
    {
        Text = PdaTexts.ConsultaGanadorAyuda,
        FontSize = 11,
        TextColor = Ui.Muted,
        IsVisible = false
    };
    private readonly Dictionary<string, Guid> _loteriasPorNombre = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Guid> _gruposPorNombre = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Guid> _vendedoresPorNombre = new(StringComparer.OrdinalIgnoreCase);
    private string _tipo = PdaTexts.ConsultaBoletos;
    private int _pagina = 1;
    private IReadOnlyList<Fila> _datos = [];
    private decimal _totalVentasSegunFiltro;

    public ConsultasPage(NewRichApiClient api)
    {
        _api = api;
        Title = PdaTexts.Consultas;
        BackgroundColor = Ui.Paper;

        _estado.ItemsSource = new List<string>
        {
            PdaTexts.FiltroTodos,
            "por jugar",
            "jugados",
            "ganadores",
            "no ganadores",
            "vencidos",
            "pagados"
        };
        _estado.SelectedIndex = 0;

        _tipoApuesta.ItemsSource = new List<string>
        {
            PdaTexts.FiltroTodos,
            PdaTexts.TipoCombinada,
            PdaTexts.TipoIndividual
        };
        _tipoApuesta.SelectedIndex = 0;

        _conectado.ItemsSource = new List<string>
        {
            PdaTexts.FiltroTodos,
            PdaTexts.Conectado,
            PdaTexts.SinConexion
        };
        _conectado.SelectedIndex = 0;

        _filas.SelectedIndexChanged += (_, _) => { _pagina = 1; Pintar(); };

        _resumenVentas = new Border
        {
            BackgroundColor = Ui.Mint,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Padding = new Thickness(18, 16),
            IsVisible = false,
            HorizontalOptions = LayoutOptions.Fill,
            Content = new VerticalStackLayout
            {
                Spacing = 2,
                Children = { _resumenVentasTitulo, _resumenVentasValor }
            }
        };

        ConstruirFichas();
        ActualizarFiltrosVisuales();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 14, 16, 24),
                Spacing = 14,
                Children =
                {
                    new Label { Text = PdaTexts.ConsultasAyuda, TextColor = Ui.Muted, FontSize = 12 },
                    new ScrollView
                    {
                        Orientation = ScrollOrientation.Horizontal,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
                        Content = _fichas
                    },
                    PanelFiltros(),
                    _resumenVentas,
                    CabeceraResultados(),
                    _ayudaGanador,
                    _lista,
                    Paginador()
                }
            }
        };
    }

    private void ConstruirFichas()
    {
        _fichas.Children.Clear();
        foreach (var tipo in Tipos)
        {
            _fichas.Children.Add(Ficha(tipo));
        }
    }

    private View Ficha(string tipo)
    {
        var activa = tipo == _tipo;
        var ficha = new Border
        {
            BackgroundColor = activa ? Ui.Dark : Colors.White,
            Stroke = activa ? Ui.Dark : Ui.Line,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Padding = new Thickness(16, 9),
            Content = new Label
            {
                Text = tipo,
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = activa ? Colors.White : Ui.Ink
            }
        };

        var toque = new TapGestureRecognizer();
        toque.Tapped += async (_, _) =>
        {
            if (_tipo == tipo)
            {
                return;
            }

            _tipo = tipo;
            _pagina = 1;
            ConstruirFichas();
            await CargarAsync();
        };
        ficha.GestureRecognizers.Add(toque);
        return ficha;
    }

    private View PanelFiltros()
    {
        var buscar = Ui.Primario(PdaTexts.Buscar);
        buscar.Clicked += async (_, _) => { _pagina = 1; await CargarAsync(); };

        var limpiar = Ui.Secundario(PdaTexts.Limpiar);
        limpiar.Clicked += async (_, _) =>
        {
            _codigo.Text = string.Empty;
            _numero.Text = string.Empty;
            _estado.SelectedIndex = 0;
            _tipoApuesta.SelectedIndex = 0;
            _conectado.SelectedIndex = 0;
            _loteria.SelectedIndex = 0;
            _grupo.SelectedIndex = 0;
            _vendedor.SelectedIndex = 0;
            _fecha.Date = DateTime.Today;
            _fechaDesde.Date = DateTime.Today.AddDays(-7);
            _fechaHasta.Date = DateTime.Today;
            _pagina = 1;
            await CargarAsync();
        };

        var acciones = new Grid { ColumnSpacing = 10 };
        acciones.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        acciones.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        acciones.Add(buscar, 0, 0);
        acciones.Add(limpiar, 1, 0);

        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Ui.Line,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Padding = 16,
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        Text = PdaTexts.ConsultaFiltros,
                        FontSize = 13,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Ui.Dark
                    },
                    _filtros,
                    acciones
                }
            }
        };
    }

    private View CabeceraResultados()
    {
        var cabecera = new Grid { ColumnSpacing = 10 };
        cabecera.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        cabecera.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        cabecera.Add(new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label
                {
                    Text = PdaTexts.ConsultaResultadosTitulo,
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Ui.Ink
                },
                _resumen
            }
        }, 0, 0);

        cabecera.Add(new VerticalStackLayout
        {
            Spacing = 2,
            WidthRequest = 110,
            Children =
            {
                new Label { Text = PdaTexts.FilasPorPagina, FontSize = 10, TextColor = Ui.Muted },
                _filas
            }
        }, 1, 0);

        return cabecera;
    }

    /// <summary>Los campos con nombres largos ocupan la fila entera; el resto va en dos columnas.</summary>
    private static bool OcupaFilaCompleta(string etiqueta) =>
        etiqueta == PdaTexts.Loteria
        || etiqueta == PdaTexts.EstadoBoleto
        || etiqueta == PdaTexts.Vendedor
        || etiqueta == PdaTexts.Grupo
        || etiqueta == PdaTexts.TicketCode;

    private View? ControlParaEtiqueta(string etiqueta) => etiqueta switch
    {
        var e when e == PdaTexts.TicketCode => _codigo,
        var e when e == PdaTexts.NumeroApostado => _numero,
        var e when e == PdaTexts.EstadoBoleto => _estado,
        var e when e == PdaTexts.TipoApuestaFiltro => _tipoApuesta,
        var e when e == PdaTexts.Fecha => _fecha,
        var e when e == PdaTexts.FechaResultado => _fecha,
        var e when e == PdaTexts.Desde => _fechaDesde,
        var e when e == PdaTexts.Hasta => _fechaHasta,
        var e when e == PdaTexts.Loteria => _loteria,
        var e when e == PdaTexts.Vendedor => _vendedor,
        var e when e == PdaTexts.Grupo => _grupo,
        var e when e == PdaTexts.CodigoPda => _codigo,
        var e when e == PdaTexts.Conectado => _conectado,
        _ => null
    };

    private void ActualizarFiltrosVisuales()
    {
        var campos = ConsultaObservadorCampos.Obtener(_tipo);
        _filtros.Children.Clear();
        _filtros.RowDefinitions.Clear();
        _filtros.ColumnDefinitions.Clear();
        _filtros.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        _filtros.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        if (campos.Count == 0)
        {
            _filtros.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var vacio = new Label
            {
                Text = PdaTexts.SinFiltrosAdicionales,
                TextColor = Ui.Muted,
                FontSize = 12
            };
            _filtros.Add(vacio, 0, 0);
            Grid.SetColumnSpan(vacio, 2);
            return;
        }

        var fila = 0;
        var columna = 0;
        _filtros.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        foreach (var campo in campos)
        {
            var control = ControlParaEtiqueta(campo.Etiqueta);
            if (control is null)
            {
                continue;
            }

            var completo = OcupaFilaCompleta(campo.Etiqueta);
            if (completo && columna == 1)
            {
                fila++;
                columna = 0;
                _filtros.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }

            var bloque = Campo(campo.Etiqueta, control);
            _filtros.Add(bloque, columna, fila);
            if (completo)
            {
                Grid.SetColumnSpan(bloque, 2);
                fila++;
                columna = 0;
                _filtros.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                continue;
            }

            if (columna == 1)
            {
                fila++;
                columna = 0;
                _filtros.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }
            else
            {
                columna = 1;
            }
        }
    }

    private static View Campo(string etiqueta, View control)
    {
        if (control.Parent is Layout padre)
        {
            padre.Children.Remove(control);
        }

        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = etiqueta, FontSize = 11, TextColor = Ui.Muted },
                control
            }
        };
    }

    private View Paginador()
    {
        Button Boton(string texto, Action accion)
        {
            var b = Ui.Secundario(texto);
            b.HeightRequest = 40;
            b.FontSize = 12;
            b.Clicked += (_, _) => accion();
            return b;
        }

        var barra = new Grid { ColumnSpacing = 6 };
        for (var i = 0; i < 4; i++)
        {
            barra.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        barra.Add(Boton(PdaTexts.PaginaInicio, () => { _pagina = 1; Pintar(); }), 0, 0);
        barra.Add(Boton(PdaTexts.PaginaAnterior, () => { _pagina = Math.Max(1, _pagina - 1); Pintar(); }), 1, 0);
        barra.Add(Boton(PdaTexts.PaginaSiguiente, () =>
        {
            _pagina = Math.Min(Paginacion.TotalPaginas(_datos.Count, Filas()), _pagina + 1);
            Pintar();
        }), 2, 0);
        barra.Add(Boton(PdaTexts.PaginaUltimo, () =>
        {
            _pagina = Paginacion.TotalPaginas(_datos.Count, Filas());
            Pintar();
        }), 3, 0);

        return barra;
    }

    private int Filas() => _filas.SelectedItem is int n ? n : 10;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await CargarAsync();
        }
        catch (Exception)
        {
        }
    }

    private async Task CargarAsync()
    {
        await CargarOpcionesAsync();
        ActualizarFiltrosVisuales();

        IReadOnlyList<Fila> filas = [];
        var mensaje = string.Empty;
        var ok = true;
        var fechaDesde = (_fechaDesde.Date ?? DateTime.Today.AddDays(-7)).Date;
        var fechaHasta = (_fechaHasta.Date ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

        if (_tipo == PdaTexts.ConsultaBoletos)
        {
            var estado = _estado.SelectedItem as string;
            var resultado = await _api.BoletosAsync(new FiltroBoletosRequest
            {
                CodigoPublico = TextoOpcional(_codigo.Text),
                Numero = TextoOpcional(_numero.Text),
                Estado = estado == PdaTexts.FiltroTodos ? null : estado
            }, CancellationToken.None);
            ok = resultado.IsSuccess;
            mensaje = resultado.Message;
            var boletos = resultado.Data ?? [];
            _totalVentasSegunFiltro = boletos.Sum(x => x.Total);
            filas = boletos.Select(x => new Fila(
                CodigoPublicoGenerator.FormatoImpreso(x.CodigoPublico),
                $"{x.Vendedor} · {x.Fecha:dd/MM/yyyy HH:mm}",
                FormatoDinero.Pesos(x.Total),
                x.Estado,
                ConsultaObservadorResultados.EsGanador(x.Estado),
                x)).ToList();
        }
        else if (_tipo == PdaTexts.ConsultaVentas)
        {
            var resultado = await _api.VentasAsync(new ConsultaVentasRequest
            {
                Numero = TextoOpcional(_numero.Text),
                LoteriaId = OpcionSeleccionada(_loteria, _loteriasPorNombre),
                TipoApuesta = TipoApuestaSeleccionado(),
                FechaInicial = fechaDesde,
                FechaFinal = fechaHasta
            }, CancellationToken.None);
            ok = resultado.IsSuccess;
            mensaje = resultado.Message;
            var ventas = resultado.Data ?? [];
            _totalVentasSegunFiltro = ventas.Sum(x => x.Total);
            filas = ventas.Select(x =>
            {
                var boleto = ConsultaObservadorResultados.ComoBoleto(x);
                return new Fila(
                    x.CodigoImpreso,
                    $"{ConsultaObservadorResultados.TipoApuestaTexto(x.TipoApuesta)} · {PdaTexts.NumeroApostado}: {ConsultaObservadorResultados.NumerosApostados(x)}\n{x.Vendedor} · {x.FechaVenta:dd/MM/yyyy HH:mm}",
                    FormatoDinero.Pesos(x.Total),
                    x.EstadoBoleto,
                    ConsultaObservadorResultados.EsGanador(x.EstadoBoleto),
                    boleto);
            }).ToList();
        }
        else if (_tipo == PdaTexts.ConsultaVendedores)
        {
            var usuarios = await _api.UsuariosAsync(CancellationToken.None);
            ok = usuarios.IsSuccess;
            mensaje = usuarios.Message;
            var vendedorId = OpcionSeleccionada(_vendedor, _vendedoresPorNombre);
            var grupo = OpcionSeleccionadaTexto(_grupo);
            var seleccionados = (usuarios.Data ?? [])
                .Where(u => u.Rol == RolUsuario.Vendedor)
                .Where(u => vendedorId is null || u.UsuarioId == vendedorId)
                .Where(u => grupo is null || string.Equals(u.GrupoNombre, grupo, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var ventas = await _api.VentasAsync(new ConsultaVentasRequest
            {
                VendedorId = vendedorId,
                FechaInicial = fechaDesde,
                FechaFinal = fechaHasta
            }, CancellationToken.None);

            filas = ConsultaObservadorResultados
                .TotalesPorVendedor(seleccionados, ventas.Data ?? [])
                .Select(x => new Fila(
                    x.Vendedor,
                    $"{x.Grupo} · {x.Ventas} {PdaTexts.ConsultaVentasPor.ToLowerInvariant()}",
                    FormatoDinero.Pesos(x.Total),
                    null))
                .ToList();
            _totalVentasSegunFiltro = 0m;
        }
        else if (_tipo == PdaTexts.ConsultaDispositivos)
        {
            var resultado = await _api.DispositivosAsync(CancellationToken.None);
            ok = resultado.IsSuccess;
            mensaje = resultado.Message;
            var conectado = _conectado.SelectedItem as string;
            filas = (resultado.Data ?? [])
                .Where(d => conectado == PdaTexts.FiltroTodos || (conectado == PdaTexts.Conectado && d.Conectado) || (conectado == PdaTexts.SinConexion && !d.Conectado))
                .Select(d => new Fila(
                    d.CodigoDispositivo,
                    $"{d.Tipo} · {d.UsuarioAsociado ?? "-"}",
                    null,
                    d.Conectado ? PdaTexts.Conectado : PdaTexts.SinConexion))
                .ToList();
            _totalVentasSegunFiltro = 0m;
        }
        else if (_tipo == PdaTexts.ConsultaResultados)
        {
            var resultado = await _api.ResultadosAsync(
                DateOnly.FromDateTime(_fecha.Date ?? DateTime.Today),
                OpcionSeleccionada(_loteria, _loteriasPorNombre),
                CancellationToken.None);
            ok = resultado.IsSuccess;
            mensaje = resultado.Message;
            filas = (resultado.Data ?? []).Select(r => new Fila(
                r.Numero,
                $"{r.Loteria} · {r.FechaJuego:dd/MM/yyyy}",
                null,
                PdaTexts.CantidadGanadores(r.CantidadGanadores),
                r.TieneGanadores,
                null,
                r)).ToList();
            _totalVentasSegunFiltro = 0m;
        }
        else
        {
            var resultado = await _api.OperativaAsync(CancellationToken.None);
            ok = resultado.IsSuccess;
            mensaje = resultado.Message;
            _totalVentasSegunFiltro = 0m;
            if (resultado.Data is not null)
            {
                var c = resultado.Data;
                filas =
                [
                    new Fila(PdaTexts.HorarioAbierto, PdaTexts.ConsultaConfiguracion, c.HoraCierre, null),
                    new Fila(PdaTexts.VigenciaPremios, PdaTexts.ConsultaConfiguracion, $"{c.VigenciaPremiosDias} días", null),
                    new Fila($"Máximo {PdaTexts.TipoCombinada}", PdaTexts.ConsultaConfiguracion, c.MaxJuegosCombinado.ToString(), null),
                    new Fila($"Máximo {PdaTexts.TipoIndividual}", PdaTexts.ConsultaConfiguracion, c.MaxLineasIndividual.ToString(), null)
                ];
            }
        }

        _datos = filas;
        if (!ok)
        {
            await this.AvisoAsync(PdaTexts.Consultas, mensaje, PdaTexts.Cerrar);
        }

        Pintar();
    }

    private async Task CargarOpcionesAsync()
    {
        try
        {
            var loterias = await _api.LoteriasAsync(CancellationToken.None);
            if (loterias.IsSuccess && loterias.Data is not null)
            {
                _loteriasPorNombre.Clear();
                var items = new List<string> { PdaTexts.FiltroTodos };
                foreach (var item in loterias.Data.Where(x => !string.IsNullOrWhiteSpace(x.Nombre)))
                {
                    _loteriasPorNombre[item.Nombre] = item.LoteriaId;
                    items.Add(item.Nombre);
                }

                Reemplazar(_loteria, items);
            }
        }
        catch (Exception)
        {
        }

        try
        {
            var grupos = await _api.GruposAsync(CancellationToken.None);
            if (grupos.IsSuccess && grupos.Data is not null)
            {
                _gruposPorNombre.Clear();
                var items = new List<string> { PdaTexts.FiltroTodos };
                foreach (var grupo in grupos.Data.Where(x => !string.IsNullOrWhiteSpace(x.Nombre)))
                {
                    _gruposPorNombre[grupo.Nombre] = grupo.GrupoId;
                    items.Add(grupo.Nombre);
                }

                Reemplazar(_grupo, items);
            }
        }
        catch (Exception)
        {
        }

        try
        {
            var usuarios = await _api.UsuariosAsync(CancellationToken.None);
            if (usuarios.IsSuccess && usuarios.Data is not null)
            {
                _vendedoresPorNombre.Clear();
                var items = new List<string> { PdaTexts.FiltroTodos };
                foreach (var vendedor in usuarios.Data.Where(u => u.Rol == RolUsuario.Vendedor))
                {
                    var nombre = NombreVendedor(vendedor);
                    if (string.IsNullOrWhiteSpace(nombre) || _vendedoresPorNombre.ContainsKey(nombre))
                    {
                        continue;
                    }

                    _vendedoresPorNombre[nombre] = vendedor.UsuarioId;
                    items.Add(nombre);
                }

                Reemplazar(_vendedor, items);
            }
        }
        catch (Exception)
        {
        }
    }

    private static string NombreVendedor(UsuarioResponse vendedor) =>
        string.IsNullOrWhiteSpace(vendedor.NombreCompleto) ? vendedor.Usuario : vendedor.NombreCompleto;

    private static void Reemplazar(Picker picker, List<string> items)
    {
        if (picker.ItemsSource is List<string> actual && actual.SequenceEqual(items))
        {
            return;
        }

        var seleccionado = picker.SelectedItem as string;
        picker.ItemsSource = items;
        var indice = seleccionado is null ? 0 : items.IndexOf(seleccionado);
        picker.SelectedIndex = indice < 0 ? 0 : indice;
    }

    private TipoApuesta? TipoApuestaSeleccionado() => (_tipoApuesta.SelectedItem as string) switch
    {
        var t when t == PdaTexts.TipoCombinada => TipoApuesta.COMBINADO,
        var t when t == PdaTexts.TipoIndividual => TipoApuesta.INDIVIDUAL,
        _ => null
    };

    private static string? OpcionSeleccionadaTexto(Picker picker)
    {
        var seleccionado = picker.SelectedItem as string;
        return string.IsNullOrWhiteSpace(seleccionado) || seleccionado == PdaTexts.FiltroTodos ? null : seleccionado;
    }

    private static Guid? OpcionSeleccionada(Picker picker, IReadOnlyDictionary<string, Guid> opciones)
    {
        var seleccionado = OpcionSeleccionadaTexto(picker);
        return seleccionado is not null && opciones.TryGetValue(seleccionado, out var id) ? id : null;
    }

    private void Pintar()
    {
        var filas = Filas();
        var tituloTotal = ConsultaObservadorCampos.TituloTotalFiltro(_tipo);
        _resumenVentas.IsVisible = tituloTotal is not null;
        _resumenVentasTitulo.Text = tituloTotal ?? PdaTexts.TotalFiltroVentas;
        _resumenVentasValor.Text = FormatoDinero.Pesos(_totalVentasSegunFiltro);
        _resumen.Text = Paginacion.Resumen(_pagina, filas, _datos.Count);
        _ayudaGanador.Text = _tipo == PdaTexts.ConsultaResultados
            ? PdaTexts.ResultadoSeleccionarAyuda
            : PdaTexts.ConsultaGanadorAyuda;
        _ayudaGanador.IsVisible = _datos.Any(x => x.Ganador);
        _lista.Children.Clear();

        if (_datos.Count == 0)
        {
            _lista.Children.Add(new Border
            {
                BackgroundColor = Colors.White,
                Stroke = Ui.Line,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Padding = 18,
                Content = new Label
                {
                    Text = PdaTexts.SinVentas,
                    TextColor = Ui.Muted,
                    FontSize = 12,
                    HorizontalTextAlignment = TextAlignment.Center
                }
            });
            return;
        }

        foreach (var item in Paginacion.Pagina(_datos, _pagina, filas))
        {
            _lista.Children.Add(TarjetaFila(item));
        }
    }

    private View TarjetaFila(Fila fila)
    {
        var colorResaltado = fila.Resultado is not null
            ? (fila.Ganador ? ConsultaObservadorResultados.ColorGanador : null)
            : ConsultaObservadorResultados.ColorResaltado(fila.Estado);
        var resaltada = colorResaltado is not null;
        var entregado = ConsultaObservadorResultados.EsPremioEntregado(fila.Estado);

        var izquierda = new VerticalStackLayout { Spacing = 3 };
        izquierda.Children.Add(new Label
        {
            Text = fila.Titulo,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Ui.Ink
        });
        izquierda.Children.Add(new Label
        {
            Text = fila.Detalle,
            FontSize = 12,
            TextColor = resaltada ? Ui.Dark : Ui.Muted
        });

        var derecha = new VerticalStackLayout
        {
            Spacing = 4,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center
        };

        if (fila.Monto is not null)
        {
            derecha.Children.Add(new Label
            {
                Text = fila.Monto,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Ui.Ink,
                HorizontalTextAlignment = TextAlignment.End
            });
        }

        if (fila.Estado is not null)
        {
            derecha.Children.Add(Etiqueta(fila.Estado, resaltada, entregado));
        }

        var contenido = new Grid { ColumnSpacing = 10 };
        contenido.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        contenido.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        contenido.Add(izquierda, 0, 0);
        contenido.Add(derecha, 1, 0);

        var tarjeta = new Border
        {
            BackgroundColor = resaltada ? Color.FromArgb(colorResaltado!) : Colors.White,
            Stroke = resaltada ? Color.FromArgb(colorResaltado!) : Ui.Line,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Padding = new Thickness(16, 14),
            Content = contenido
        };

        if (fila.Ganador && fila.Boleto is not null)
        {
            var toque = new TapGestureRecognizer();
            toque.Tapped += async (_, _) => await AbrirGanadorAsync(fila.Boleto);
            tarjeta.GestureRecognizers.Add(toque);
        }
        else if (fila.Ganador && fila.Resultado is not null)
        {
            var toque = new TapGestureRecognizer();
            toque.Tapped += async (_, _) => await AbrirResultadoGanadoresAsync(fila.Resultado);
            tarjeta.GestureRecognizers.Add(toque);
        }

        return tarjeta;
    }

    private static View Etiqueta(string texto, bool resaltada, bool entregado) => new Border
    {
        BackgroundColor = resaltada ? Colors.White : Ui.Paper,
        Stroke = entregado ? Color.FromArgb("#7a7c99") : resaltada ? Ui.Green : Ui.Line,
        StrokeThickness = 1,
        StrokeShape = new RoundRectangle { CornerRadius = 10 },
        Padding = new Thickness(10, 4),
        HorizontalOptions = LayoutOptions.End,
        Content = new Label
        {
            Text = texto,
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            TextColor = entregado ? Color.FromArgb("#4e5068") : resaltada ? Ui.Green : Ui.Muted
        }
    };

    private async Task AbrirGanadorAsync(BoletoListaResponse boleto)
    {
        try
        {
            await Navigation.PushAsync(new BoletoGanadorPage(_api, boleto));
        }
        catch (Exception)
        {
        }
    }

    private async Task AbrirResultadoGanadoresAsync(ResultadoResponse resultado)
    {
        try
        {
            await Navigation.PushAsync(new ResultadoGanadoresPage(_api, resultado));
        }
        catch (Exception)
        {
        }
    }

    private static string? TextoOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
