using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Ventas;
using NewRich.Domain.Enums;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Maui.Views;

namespace NewRich.Maui.Views.Observador;

public sealed class ConsultasPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly Picker _tipo = new();
    private readonly Entry _codigo = Ui.Entrada(string.Empty);
    private readonly Entry _numero = Ui.Entrada(string.Empty);
    private readonly Picker _estado = new();
    private readonly DatePicker _fecha = new() { Date = DateTime.Today };
    private readonly Picker _filas = new() { ItemsSource = Paginacion.OpcionesFilas.Cast<object>().ToList(), SelectedIndex = 1 };
    private readonly VerticalStackLayout _lista = new() { Spacing = 8 };
    private readonly Label _resumen = new() { FontSize = 12, TextColor = Ui.Muted };
    private int _pagina = 1;
    private IReadOnlyList<string> _datos = [];

    public ConsultasPage(NewRichApiClient api)
    {
        _api = api;
        Title = PdaTexts.Consultas;
        BackgroundColor = Ui.Paper;
        _tipo.ItemsSource = new List<string>
        {
            PdaTexts.ConsultaBoletos,
            PdaTexts.ConsultaVentas,
            PdaTexts.ConsultaVendedores,
            PdaTexts.ConsultaDispositivos,
            PdaTexts.ConsultaResultados,
            PdaTexts.ConsultaConfiguracion
        };
        _tipo.SelectedIndex = 0;
        _estado.ItemsSource = new List<string>
        {
            PdaTexts.FiltroGeneral,
            "por jugar",
            "jugados",
            "ganadores",
            "no ganadores",
            "vencidos",
            "pagados"
        };
        _estado.SelectedIndex = 0;
        var buscar = Ui.Primario(PdaTexts.Buscar);
        buscar.Clicked += async (_, _) => { _pagina = 1; await CargarAsync(); };
        var limpiar = Ui.Secundario(PdaTexts.Limpiar);
        limpiar.Clicked += async (_, _) =>
        {
            _codigo.Text = string.Empty;
            _numero.Text = string.Empty;
            _estado.SelectedIndex = 0;
            _pagina = 1;
            await CargarAsync();
        };
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 10,
                Children =
                {
                    new Label { Text = PdaTexts.ConsultasAyuda, TextColor = Ui.Muted, FontSize = 12 },
                    Ui.Campo(PdaTexts.TipoConsulta),
                    _tipo,
                    Ui.Campo(PdaTexts.TicketCode),
                    _codigo,
                    Ui.Campo(PdaTexts.NumeroApostado),
                    _numero,
                    Ui.Campo(PdaTexts.EstadoBoleto),
                    _estado,
                    Ui.Campo(PdaTexts.Fecha),
                    _fecha,
                    Ui.Campo(PdaTexts.FilasPorPagina),
                    _filas,
                    buscar,
                    limpiar,
                    _resumen,
                    _lista,
                    Paginador()
                }
            }
        };
    }

    private View Paginador()
    {
        Button Boton(string texto, Action accion)
        {
            var b = Ui.Secundario(texto);
            b.HeightRequest = 40;
            b.Clicked += (_, _) => accion();
            return b;
        }

        return new HorizontalStackLayout
        {
            Spacing = 6,
            Children =
            {
                Boton(PdaTexts.PaginaInicio, () => { _pagina = 1; Pintar(); }),
                Boton(PdaTexts.PaginaAnterior, () => { _pagina = Math.Max(1, _pagina - 1); Pintar(); }),
                Boton(PdaTexts.PaginaSiguiente, () =>
                {
                    var filas = _filas.SelectedItem is int n ? n : 10;
                    _pagina = Math.Min(Paginacion.TotalPaginas(_datos.Count, filas), _pagina + 1);
                    Pintar();
                }),
                Boton(PdaTexts.PaginaUltimo, () =>
                {
                    _pagina = Paginacion.TotalPaginas(_datos.Count, Filas());
                    Pintar();
                })
            }
        };
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
        var tipo = _tipo.SelectedItem as string ?? PdaTexts.ConsultaBoletos;
        IReadOnlyList<string> filas = [];
        string mensaje = string.Empty;
        var ok = true;

        if (tipo == PdaTexts.ConsultaBoletos)
        {
            var estado = _estado.SelectedItem as string;
            var resultado = await _api.BoletosAsync(new FiltroBoletosRequest
            {
                CodigoPublico = TextoOpcional(_codigo.Text),
                Numero = TextoOpcional(_numero.Text),
                Estado = estado == PdaTexts.FiltroGeneral ? null : estado
            }, CancellationToken.None);
            ok = resultado.IsSuccess;
            mensaje = resultado.Message;
            filas = (resultado.Data ?? []).Select(x => $"{x.CodigoPublico} · {x.Vendedor} · {FormatoDinero.Pesos(x.Total)} · {x.Estado}").ToList();
        }
        else if (tipo == PdaTexts.ConsultaVentas)
        {
            var resultado = await _api.VentasAsync(new ConsultaVentasRequest
            {
                Numero = TextoOpcional(_numero.Text),
                FechaInicial = (_fecha.Date ?? DateTime.Today).Date,
                FechaFinal = (_fecha.Date ?? DateTime.Today).Date.AddDays(1).AddTicks(-1)
            }, CancellationToken.None);
            ok = resultado.IsSuccess;
            mensaje = resultado.Message;
            filas = (resultado.Data ?? []).Select(x => $"{x.CodigoImpreso} · {x.Vendedor} · {FormatoDinero.Pesos(x.Total)} · {x.EstadoBoleto}").ToList();
        }
        else if (tipo == PdaTexts.ConsultaVendedores)
        {
            var resultado = await _api.UsuariosAsync(CancellationToken.None);
            ok = resultado.IsSuccess;
            mensaje = resultado.Message;
            filas = (resultado.Data ?? [])
                .Where(u => u.Rol == RolUsuario.Vendedor)
                .Select(u => $"{u.NombreCompleto} · {u.Alias} · {u.Estado} · {u.CodigoDispositivo}")
                .ToList();
        }
        else if (tipo == PdaTexts.ConsultaDispositivos)
        {
            var resultado = await _api.DispositivosAsync(CancellationToken.None);
            ok = resultado.IsSuccess;
            mensaje = resultado.Message;
            filas = (resultado.Data ?? []).Select(d => $"{d.CodigoDispositivo} · {d.Tipo} · {(d.Conectado ? PdaTexts.Conectado : PdaTexts.SinConexion)} · {d.UsuarioAsociado}").ToList();
        }
        else if (tipo == PdaTexts.ConsultaResultados)
        {
            var resultado = await _api.ResultadosAsync(DateOnly.FromDateTime(_fecha.Date ?? DateTime.Today), null, CancellationToken.None);
            ok = resultado.IsSuccess;
            mensaje = resultado.Message;
            filas = (resultado.Data ?? []).Select(r => $"{r.FechaJuego:yyyy-MM-dd} · {r.Loteria} · {r.Numero}").ToList();
        }
        else
        {
            var resultado = await _api.OperativaAsync(CancellationToken.None);
            ok = resultado.IsSuccess;
            mensaje = resultado.Message;
            if (resultado.Data is not null)
            {
                var c = resultado.Data;
                filas =
                [
                    $"{PdaTexts.HorarioAbierto}: {c.HoraCierre}",
                    $"Vigencia: {c.VigenciaPremiosDias} días",
                    $"Máximo COMBINADO: {c.MaxJuegosCombinado}",
                    $"Máximo INDIVIDUAL: {c.MaxLineasIndividual}"
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

    private void Pintar()
    {
        var filas = _filas.SelectedItem is int n ? n : 10;
        _resumen.Text = Paginacion.Resumen(_pagina, filas, _datos.Count);
        _lista.Children.Clear();
        if (_datos.Count == 0)
        {
            _lista.Children.Add(new Label { Text = PdaTexts.SinVentas, TextColor = Ui.Muted });
            return;
        }

        foreach (var item in Paginacion.Pagina(_datos, _pagina, filas))
        {
            _lista.Children.Add(Ui.Tarjeta(new Label { Text = item, TextColor = Ui.Ink }));
        }
    }

    private static string? TextoOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
