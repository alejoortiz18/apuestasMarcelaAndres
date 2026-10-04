using NewRich.Application.Contracts.Recaudo;
using NewRich.Domain.Services;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;
using NewRich.Maui.Data;
using NewRich.Maui.Services;
using NewRich.Maui.Views;

namespace NewRich.Maui.Views.Recaudador;

public sealed class RecaudadorCobroPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly SesionPda _sesion;
    private readonly LocalDatabase _local;
    private readonly IPrinterService _impresora;
    private readonly VerticalStackLayout _lista = new() { Spacing = 10 };
    private readonly HorizontalStackLayout _grupos = new() { Spacing = 8 };
    private readonly Label _estado = new() { FontSize = 11, TextColor = Color.FromArgb("#bce9cc") };
    private readonly Label _porCobrar = new() { FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink };
    private readonly Label _recaudado = new() { FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink };
    private readonly Label _pendiente = new() { FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink };
    private readonly Entry _buscar = Ui.Entrada(PdaTexts.BuscarVendedorAyuda);
    private readonly Picker _orden = new() { Title = PdaTexts.OrdenarPor, ItemsSource = new[] { "vendedor", "vendido", "cobrar", "pendiente", "recibido", "estado" } };
    private readonly ActivityIndicator _spinner = new() { IsRunning = false, IsVisible = false, Color = Ui.Gold };
    private readonly Grid _modal = new() { IsVisible = false };
    private readonly List<Button> _botonesGrupo = [];
    private Button? _botonPendientes;
    private Button? _botonCobrados;
    private string _firmaGrupos = string.Empty;
    private string? _grupo;
    private string _consulta = string.Empty;
    private ListaCobro _filtroLista = ListaCobro.Pendientes;
    private IReadOnlyList<ObligacionRecaudoResponse> _filas = [];
    private bool _ocupado;

    public RecaudadorCobroPage(NewRichApiClient api, SesionPda sesion, LocalDatabase local, IPrinterService impresora)
    {
        _api = api;
        _sesion = sesion;
        _local = local;
        _impresora = impresora;
        Title = PdaTexts.Recaudar;
        _orden.SelectedIndex = 0;
        _orden.SelectedIndexChanged += (_, _) => Pintar();
        _buscar.Completed += (_, _) =>
        {
            _consulta = _buscar.Text ?? string.Empty;
            Pintar();
        };

        var contenido = new ScrollView
        {
            BackgroundColor = Ui.Paper,
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 12,
                Children =
                {
                    Encabezado(),
                    Grupos(),
                    Listas(),
                    Busqueda(),
                    Indicadores(),
                    _spinner,
                    _lista
                }
            }
        };

        ArmarModal();
        Content = new Grid { Children = { contenido, _modal } };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarAsync();
    }

    private View Encabezado() => new Frame
    {
        BackgroundColor = Ui.Dark,
        BorderColor = Ui.Dark,
        CornerRadius = 0,
        Padding = new Thickness(18, 18, 18, 22),
        Content = new VerticalStackLayout
        {
            Children =
            {
                Ui.Titulo(PdaTexts.Recaudar),
                new Label { Text = _sesion.Usuario?.NombreCompleto ?? PdaTexts.PdaRecaudador, TextColor = Color.FromArgb("#aed8c4"), FontSize = 13 },
                _estado
            }
        }
    };

    private View Grupos() => new ScrollView
    {
        Orientation = ScrollOrientation.Horizontal,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
        Content = _grupos
    };

    private View Listas()
    {
        _botonPendientes = BotonLista(PdaTexts.Pendientes, ListaCobro.Pendientes);
        _botonCobrados = BotonLista(PdaTexts.Cobrados, ListaCobro.Cobrados);
        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
            ColumnSpacing = 8,
            Children = { Celda(_botonPendientes, 0), Celda(_botonCobrados, 1) }
        };
    }

    private static View Celda(View vista, int columna)
    {
        Grid.SetColumn(vista, columna);
        return vista;
    }

    private Button BotonLista(string texto, ListaCobro lista)
    {
        var boton = new Button
        {
            Text = texto,
            CornerRadius = 10,
            HeightRequest = 44,
            FontAttributes = FontAttributes.Bold
        };
        boton.Clicked += (_, _) =>
        {
            _filtroLista = lista;
            Pintar();
        };
        return boton;
    }

    private View Busqueda()
    {
        var buscar = Ui.Primario(PdaTexts.Buscar);
        buscar.WidthRequest = 112;
        buscar.Clicked += (_, _) =>
        {
            _consulta = _buscar.Text ?? string.Empty;
            Pintar();
        };
        var fila = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) },
            ColumnSpacing = 8,
            Children = { _buscar, buscar }
        };
        Grid.SetColumn(buscar, 1);
        return new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                Ui.Campo(PdaTexts.BuscarVendedor),
                fila,
                _orden
            }
        };
    }

    private View Indicadores() => new Grid
    {
        ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) },
        ColumnSpacing = 8,
        Children =
        {
            Mini(PdaTexts.PorCobrarHoy, _porCobrar, 0),
            Mini(PdaTexts.RecaudadoHoy, _recaudado, 1),
            Mini(PdaTexts.PendienteHoy, _pendiente, 2)
        }
    };

    private static View Mini(string titulo, Label valor, int col)
    {
        var marco = new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = Ui.Line,
            CornerRadius = 12,
            Padding = 10,
            Content = new VerticalStackLayout
            {
                Children =
                {
                    new Label { Text = titulo, FontSize = 11, TextColor = Ui.Muted },
                    valor
                }
            }
        };
        Grid.SetColumn(marco, col);
        return marco;
    }

    private async Task CargarAsync()
    {
        if (_ocupado)
        {
            return;
        }

        _ocupado = true;
        _spinner.IsVisible = _spinner.IsRunning = true;
        try
        {
            var ping = await _api.ConectarAsync(
                PdaConexion.UrlsPara(DeviceInfo.Current.DeviceType == DeviceType.Virtual),
                CancellationToken.None);
            _estado.Text = $"PDA {_sesion.CodigoDispositivo} · {(ping.IsSuccess ? PdaTexts.Conectado : PdaTexts.SinConexion)}";
            if (ping.IsSuccess)
            {
                await SubirPendientesAsync();
                var remoto = await _api.ObligacionesRecaudoAsync(null, CancellationToken.None);
                if (remoto.IsSuccess && remoto.Data is not null)
                {
                    _filas = RecaudoListas.ConPendientes(remoto.Data, await _local.PagosRecaudoPendientesAsync());
                    await _local.GuardarObligacionesRecaudoAsync(_filas);
                }
                else
                {
                    _filas = await _local.ObligacionesRecaudoLocalAsync();
                }
            }
            else
            {
                _filas = await _local.ObligacionesRecaudoLocalAsync();
            }

            ArmarGrupos();
            Pintar();
        }
        finally
        {
            _spinner.IsVisible = _spinner.IsRunning = false;
            _ocupado = false;
        }
    }

    private void ArmarGrupos()
    {
        var nombres = RecaudoListas.Grupos(_filas);
        var firma = string.Join("|", nombres);
        if (firma == _firmaGrupos && _botonesGrupo.Count > 0)
        {
            return;
        }

        _firmaGrupos = firma;
        if (_grupo is not null && !nombres.Contains(_grupo, StringComparer.OrdinalIgnoreCase))
        {
            _grupo = null;
        }

        _grupos.Children.Clear();
        _botonesGrupo.Clear();
        _botonesGrupo.Add(BotonGrupo(PdaTexts.Todos, null));
        foreach (var nombre in nombres)
        {
            _botonesGrupo.Add(BotonGrupo(nombre, nombre));
        }
    }

    private Button BotonGrupo(string texto, string? grupo)
    {
        var boton = new Button
        {
            Text = texto,
            CornerRadius = 10,
            HeightRequest = 44,
            Padding = new Thickness(14, 0),
            FontAttributes = FontAttributes.Bold
        };
        boton.Clicked += (_, _) =>
        {
            _grupo = grupo;
            Pintar();
        };
        _grupos.Children.Add(boton);
        return boton;
    }

    private void Pintar()
    {
        Marcar(_botonPendientes, _filtroLista == ListaCobro.Pendientes);
        Marcar(_botonCobrados, _filtroLista == ListaCobro.Cobrados);
        for (var i = 0; i < _botonesGrupo.Count; i++)
        {
            var esTodos = i == 0;
            var activo = esTodos
                ? _grupo is null
                : string.Equals(_botonesGrupo[i].Text, _grupo, StringComparison.OrdinalIgnoreCase);
            Marcar(_botonesGrupo[i], activo);
        }

        var orden = _orden.SelectedItem as string ?? "vendedor";
        var visibles = RecaudoListas.De(_filas, _filtroLista, _consulta, orden, _grupo);
        _porCobrar.Text = RecaudoPagoVista.Miles(_filas.Sum(f => f.ValorACobrar));
        _recaudado.Text = RecaudoPagoVista.Miles(_filas.Sum(f => f.PagosHoy));
        _pendiente.Text = RecaudoPagoVista.Miles(_filas.Sum(f => f.TotalPendiente));
        _lista.Children.Clear();
        if (visibles.Count == 0)
        {
            _lista.Children.Add(new Label { Text = PdaTexts.SinAsignadosHoy, TextColor = Ui.Muted, Margin = 8 });
            return;
        }

        foreach (var grupo in RecaudoListas.Agrupar(visibles))
        {
            _lista.Children.Add(new Label
            {
                Text = grupo.Key,
                FontAttributes = FontAttributes.Bold,
                FontSize = 16,
                TextColor = Ui.Ink
            });
            foreach (var fila in grupo)
            {
                _lista.Children.Add(Tarjeta(fila));
            }
        }
    }

    private static void Marcar(Button? boton, bool activo)
    {
        if (boton is null)
        {
            return;
        }

        boton.BackgroundColor = activo ? Ui.Dark : Colors.White;
        boton.TextColor = activo ? Colors.White : Ui.Dark;
        boton.BorderColor = activo ? Ui.Dark : Ui.Line;
        boton.BorderWidth = 1;
    }

    private View Tarjeta(ObligacionRecaudoResponse fila)
    {
        var color = fila.Color == nameof(ColorCobro.Rojo) ? Ui.Danger
            : fila.Color == nameof(ColorCobro.Azul) ? Ui.Info
            : Ui.Green;
        var valor = Ui.Pesos(PdaTexts.ValorRecibido);
        var cobrar = Ui.Primario(PdaTexts.RegistrarCobro);
        cobrar.Clicked += async (_, _) => await PedirCobroAsync(fila, valor.Texto);
        var hijos = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                Encabezado(fila),
                new Label { Text = string.IsNullOrWhiteSpace(fila.Alias) ? fila.Estado : $"{fila.Alias} · {fila.Estado}", TextColor = color, FontAttributes = FontAttributes.Bold, FontSize = 13 },
                Cifras(fila, color),
                valor,
                cobrar
            }
        };
        if (string.Equals(fila.Lista, nameof(ListaCobro.Cobrados), StringComparison.OrdinalIgnoreCase))
        {
            hijos.Children.Insert(1, new Frame
            {
                Padding = new Thickness(10, 4),
                CornerRadius = 8,
                HasShadow = false,
                BackgroundColor = Ui.InfoBg,
                BorderColor = Ui.Info,
                HorizontalOptions = LayoutOptions.Start,
                Content = new Label
                {
                    Text = PdaTexts.YaCobrado,
                    TextColor = Ui.Info,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 12
                }
            });
        }

        if (fila.SenalSinGrupo)
        {
            var aviso = string.Equals(fila.Lista, nameof(ListaCobro.Cobrados), StringComparison.OrdinalIgnoreCase) ? 3 : 2;
            hijos.Children.Insert(aviso, new Label { Text = PdaTexts.DebeIngresarAGrupo, TextColor = Ui.Warn, FontSize = 12 });
        }

        return new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = color,
            CornerRadius = 14,
            Padding = 14,
            Content = hijos
        };
    }

    private static Grid Encabezado(ObligacionRecaudoResponse fila)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) },
            ColumnSpacing = 10
        };
        var nombre = new Label
        {
            Text = fila.NombreCompleto,
            FontAttributes = FontAttributes.Bold,
            FontSize = 17,
            TextColor = Ui.Ink,
            VerticalOptions = LayoutOptions.Center,
            LineBreakMode = LineBreakMode.TailTruncation
        };
        var total = new Frame
        {
            Padding = new Thickness(12, 6),
            CornerRadius = 10,
            HasShadow = false,
            BackgroundColor = Ui.Crema,
            BorderColor = Ui.Gold,
            HorizontalOptions = LayoutOptions.End,
            Content = new VerticalStackLayout
            {
                Spacing = 0,
                Children =
                {
                    new Label { Text = PdaTexts.TotalAPagar, FontSize = 11, TextColor = Ui.Muted, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.End },
                    new Label
                    {
                        Text = RecaudoPagoVista.Miles(RecaudoPagoVista.Saldos(fila).TotalAPagar),
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Ui.Ink,
                        HorizontalTextAlignment = TextAlignment.End
                    }
                }
            }
        };
        grid.Add(nombre, 0);
        grid.Add(total, 1);
        return grid;
    }

    private static Grid Cifras(ObligacionRecaudoResponse fila, Color acento)
    {
        var cifras = RecaudoPagoVista.CifrasTarjeta(fila);
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
            ColumnSpacing = 8,
            RowSpacing = 8
        };
        for (var i = 0; i < cifras.Count; i++)
        {
            var filaGrid = i / 2;
            var col = i % 2;
            if (grid.RowDefinitions.Count <= filaGrid)
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }

            var cifra = cifras[i];
            var pagado = cifra.Etiqueta == PdaTexts.TotalPagado;
            var pendiente = cifra.Etiqueta == PdaTexts.PendienteDelDia;
            var celda = new Frame
            {
                Padding = new Thickness(10, 8),
                CornerRadius = 10,
                HasShadow = false,
                BackgroundColor = pagado ? Ui.Mint : pendiente ? Ui.Crema : Color.FromArgb("#f7faf8"),
                BorderColor = pagado ? Ui.Green : pendiente ? Ui.Gold : Ui.Line,
                Content = new VerticalStackLayout
                {
                    Spacing = 2,
                    Children =
                    {
                        new Label { Text = cifra.Etiqueta, FontSize = 11, TextColor = Ui.Muted, FontAttributes = FontAttributes.Bold },
                        new Label
                        {
                            Text = RecaudoPagoVista.Miles(cifra.Valor),
                            FontSize = 16,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = pagado ? Ui.Green : pendiente ? Ui.Ink : acento
                        }
                    }
                }
            };
            Grid.SetRow(celda, filaGrid);
            Grid.SetColumn(celda, col);
            if (cifras.Count % 2 == 1 && i == cifras.Count - 1)
            {
                Grid.SetColumnSpan(celda, 2);
            }

            grid.Children.Add(celda);
        }

        return grid;
    }

    private void ArmarModal()
    {
        _modal.BackgroundColor = Color.FromArgb("#99000000");
        var caja = new Frame
        {
            BackgroundColor = Colors.White,
            CornerRadius = 16,
            Padding = 18,
            VerticalOptions = LayoutOptions.Center,
            Margin = 24,
            Content = new VerticalStackLayout { Spacing = 10, ClassId = "cuerpo-modal" }
        };
        _modal.Children.Add(caja);
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => _modal.IsVisible = false;
        _modal.GestureRecognizers.Add(tap);
    }

    private async Task PedirCobroAsync(ObligacionRecaudoResponse fila, string digitado)
    {
        if (_ocupado)
        {
            return;
        }

        if (!RecaudoPagoVista.TryParsePesos(digitado, out var valor))
        {
            await DisplayAlert(PdaTexts.RegistrarCobro, PdaTexts.PagoRecaudoInvalido, PdaTexts.Cerrar);
            return;
        }

        var saldos = RecaudoPagoVista.Saldos(fila);
        var rechazo = RecaudoPagoVista.Rechazo(valor, saldos.TotalAPagar);
        if (rechazo is not null)
        {
            await DisplayAlert(PdaTexts.RegistrarCobro, rechazo, PdaTexts.Cerrar);
            return;
        }

        var confirmacion = RecaudoPagoVista.Confirmar(fila.NombreCompleto, saldos.TotalAPagar, valor);
        var aceptar = await DisplayAlert(
            PdaTexts.ConfirmarCobro,
            $"{confirmacion.Vendedor}\n{PdaTexts.TotalAPagar}: {RecaudoPagoVista.Miles(confirmacion.Pendiente)}\n{PdaTexts.ValorRecibido}: {RecaudoPagoVista.Miles(confirmacion.Recibido)}\n{PdaTexts.SaldoQueQueda}: {RecaudoPagoVista.Miles(confirmacion.SaldoQueQueda)}",
            PdaTexts.ConfirmarCobro,
            PdaTexts.Cancelar);
        if (!aceptar)
        {
            return;
        }

        await RegistrarAsync(fila, valor);
    }

    private async Task RegistrarAsync(ObligacionRecaudoResponse fila, decimal valor)
    {
        _ocupado = true;
        _spinner.IsVisible = _spinner.IsRunning = true;
        var pago = new PagoPendienteRecaudo(fila.VendedorId, valor, Guid.NewGuid().ToString("N"), DateTime.UtcNow);
        var saldoQueQueda = RecaudoPagoVista.Saldos(fila).TotalAPagar - valor;
        var aplicado = false;
        try
        {
            var ping = await _api.ConectarAsync(
                PdaConexion.UrlsPara(DeviceInfo.Current.DeviceType == DeviceType.Virtual),
                CancellationToken.None);
            if (ping.IsSuccess)
            {
                var resultado = await _api.RegistrarPagoRecaudoAsync(RecaudoColaPagos.Solicitud(pago), CancellationToken.None);
                if (resultado.IsSuccess && resultado.Data is not null)
                {
                    aplicado = true;
                    await _impresora.ImprimirAsync(TirillaCobroTexto.De(
                        resultado.Data.RecaudadorNombre,
                        resultado.Data.VendedorNombre,
                        resultado.Data.FechaHora,
                        valor,
                        resultado.Data.SaldoRestante), null);
                    return;
                }

                if (!RecaudoPagoVista.EsFallaDeRed(resultado.Message))
                {
                    await DisplayAlert(PdaTexts.RegistrarCobro, resultado.Message, PdaTexts.Cerrar);
                    return;
                }
            }

            var cola = RecaudoColaPagos.Encolar(pago, await _local.PagosRecaudoPendientesAsync());
            await _local.GuardarPagosRecaudoAsync(cola);
            aplicado = true;
            await _impresora.ImprimirAsync(TirillaCobroTexto.De(
                _sesion.Usuario?.NombreCompleto ?? PdaTexts.PdaRecaudador,
                fila.NombreCompleto,
                ZonaHorariaColombia.ALocal(pago.FechaLocal),
                valor,
                saldoQueQueda,
                sinConexion: true), null);
            await DisplayAlert(PdaTexts.RegistrarCobro, PdaTexts.CobroGuardadoSinConexion, PdaTexts.Cerrar);
        }
        finally
        {
            if (aplicado)
            {
                RecaudoListas.TrasCobro(fila, valor);
                await _local.GuardarObligacionesRecaudoAsync(_filas);
            }

            _spinner.IsVisible = _spinner.IsRunning = false;
            _ocupado = false;
        }

        await CargarAsync();
    }

    private async Task SubirPendientesAsync()
    {
        var pendientes = await _local.PagosRecaudoPendientesAsync();
        if (pendientes.Count == 0)
        {
            return;
        }

        var quedan = new List<PagoPendienteRecaudo>();
        foreach (var pago in pendientes)
        {
            var envio = await _api.RegistrarPagoRecaudoAsync(RecaudoColaPagos.Solicitud(pago), CancellationToken.None);
            if (!envio.IsSuccess)
            {
                quedan.Add(pago);
            }
        }

        await _local.GuardarPagosRecaudoAsync(quedan);
    }
}
