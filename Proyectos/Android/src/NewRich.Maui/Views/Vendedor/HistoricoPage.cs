using NewRich.Application.Contracts.Ventas;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Ventas;

namespace NewRich.Maui.Views.Vendedor;

public sealed class HistoricoPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly DatePicker _desde;
    private readonly DatePicker _hasta;
    private readonly Picker _filas = new() { ItemsSource = Paginacion.OpcionesFilas.Cast<object>().ToList(), SelectedIndex = 1 };
    private readonly VerticalStackLayout _lista = new() { Spacing = 8 };
    private readonly Label _resumen = new() { FontSize = 12, TextColor = Ui.Muted };
    private int _pagina = 1;
    private IReadOnlyList<VentaResponse> _datos = [];

    public HistoricoPage(NewRichApiClient api)
    {
        _api = api;
        Title = PdaTexts.Historico;
        BackgroundColor = Ui.Paper;
        var hoy = DateTime.Today;
        _desde = new DatePicker { Date = HistoricoVentasReglas.FechaMinima(hoy), MinimumDate = HistoricoVentasReglas.FechaMinima(hoy), MaximumDate = hoy };
        _hasta = new DatePicker { Date = hoy, MinimumDate = HistoricoVentasReglas.FechaMinima(hoy), MaximumDate = hoy };
        var buscar = Ui.Primario(PdaTexts.Buscar);
        buscar.Clicked += async (_, _) => { _pagina = 1; await CargarAsync(); };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 10,
                Children =
                {
                    new Label { Text = PdaTexts.HistoricoAyuda, TextColor = Ui.Muted, FontSize = 12 },
                    Ui.Campo(PdaTexts.Fecha),
                    _desde,
                    _hasta,
                    Ui.Campo(PdaTexts.FilasPorPagina),
                    _filas,
                    buscar,
                    _resumen,
                    _lista,
                    Paginador()
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarAsync();
    }

    private View Paginador()
    {
        Button Boton(string texto, Func<Task> accion)
        {
            var b = Ui.Secundario(texto);
            b.HeightRequest = 40;
            b.Clicked += async (_, _) => await accion();
            return b;
        }

        return new HorizontalStackLayout
        {
            Spacing = 6,
            Children =
            {
                Boton(PdaTexts.PaginaInicio, async () => { _pagina = 1; Pintar(); }),
                Boton(PdaTexts.PaginaAnterior, async () => { _pagina = Math.Max(1, _pagina - 1); Pintar(); }),
                Boton(PdaTexts.PaginaSiguiente, async () =>
                {
                    var filas = Filas();
                    _pagina = Math.Min(Paginacion.TotalPaginas(_datos.Count, filas), _pagina + 1);
                    Pintar();
                }),
                Boton(PdaTexts.PaginaUltimo, async () =>
                {
                    _pagina = Paginacion.TotalPaginas(_datos.Count, Filas());
                    Pintar();
                })
            }
        };
    }

    private int Filas() => _filas.SelectedItem is int n ? n : 10;

    private async Task CargarAsync()
    {
        var desde = _desde.Date ?? DateTime.Today;
        var hasta = _hasta.Date ?? DateTime.Today;
        if (!HistoricoVentasReglas.FechaPermitida(desde, DateTime.Today) || !HistoricoVentasReglas.FechaPermitida(hasta, DateTime.Today))
        {
            await DisplayAlert(PdaTexts.Historico, PdaTexts.HistoricoAyuda, PdaTexts.Cerrar);
            return;
        }

        var resultado = await _api.VentasAsync(new ConsultaVentasRequest
        {
            FechaInicial = desde,
            FechaFinal = hasta.AddDays(1).AddTicks(-1)
        }, CancellationToken.None);

        _datos = resultado.IsSuccess && resultado.Data is not null ? resultado.Data : [];
        if (!resultado.IsSuccess)
        {
            await DisplayAlert(PdaTexts.Historico, resultado.Message, PdaTexts.Cerrar);
        }

        Pintar();
    }

    private void Pintar()
    {
        var filas = Filas();
        _resumen.Text = Paginacion.Resumen(_pagina, filas, _datos.Count);
        _lista.Children.Clear();
        var pagina = Paginacion.Pagina(_datos, _pagina, filas);
        if (pagina.Count == 0)
        {
            _lista.Children.Add(new Label { Text = PdaTexts.SinVentas, TextColor = Ui.Muted });
            return;
        }

        foreach (var venta in pagina)
        {
            _lista.Children.Add(Ui.Tarjeta(new VerticalStackLayout
            {
                Children =
                {
                    new Label { Text = venta.CodigoPublico, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink },
                    new Label { Text = $"{venta.FechaVenta:yyyy-MM-dd HH:mm} · {FormatoDinero.Pesos(venta.Total)} · {venta.EstadoBoleto}", FontSize = 12, TextColor = Ui.Muted }
                }
            }));
        }
    }
}
