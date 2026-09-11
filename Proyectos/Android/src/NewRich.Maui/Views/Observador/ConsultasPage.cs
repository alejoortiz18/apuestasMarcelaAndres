using NewRich.Application.Contracts.Boletos;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Maui.Views;

namespace NewRich.Maui.Views.Observador;

public sealed class ConsultasPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly Entry _codigo = Ui.Entrada(string.Empty);
    private readonly Entry _numero = Ui.Entrada(string.Empty);
    private readonly Picker _filas = new() { ItemsSource = Paginacion.OpcionesFilas.Cast<object>().ToList(), SelectedIndex = 1 };
    private readonly VerticalStackLayout _lista = new() { Spacing = 8 };
    private readonly Label _resumen = new() { FontSize = 12, TextColor = Ui.Muted };
    private int _pagina = 1;
    private IReadOnlyList<BoletoListaResponse> _datos = [];

    public ConsultasPage(NewRichApiClient api)
    {
        _api = api;
        Title = PdaTexts.Consultas;
        BackgroundColor = Ui.Paper;
        var buscar = Ui.Primario(PdaTexts.Buscar);
        buscar.Clicked += async (_, _) => { _pagina = 1; await CargarAsync(); };
        var limpiar = Ui.Secundario(PdaTexts.Limpiar);
        limpiar.Clicked += async (_, _) =>
        {
            _codigo.Text = string.Empty;
            _numero.Text = string.Empty;
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
                    Ui.Campo(PdaTexts.TicketCode),
                    _codigo,
                    Ui.Campo(PdaTexts.NumeroApostado),
                    _numero,
                    Ui.Campo(PdaTexts.FilasPorPagina),
                    _filas,
                    buscar,
                    limpiar,
                    _resumen,
                    _lista
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarAsync();
    }

    private async Task CargarAsync()
    {
        var resultado = await _api.BoletosAsync(new FiltroBoletosRequest
        {
            CodigoPublico = string.IsNullOrWhiteSpace(_codigo.Text) ? null : _codigo.Text.Trim(),
            Numero = string.IsNullOrWhiteSpace(_numero.Text) ? null : _numero.Text.Trim()
        }, CancellationToken.None);
        _datos = resultado.IsSuccess && resultado.Data is not null ? resultado.Data : [];
        if (!resultado.IsSuccess)
        {
            await this.AvisoAsync(PdaTexts.Consultas, resultado.Message, PdaTexts.Cerrar);
        }

        Pintar();
    }

    private void Pintar()
    {
        var filas = _filas.SelectedItem is int n ? n : 10;
        _resumen.Text = Paginacion.Resumen(_pagina, filas, _datos.Count);
        _lista.Children.Clear();
        foreach (var item in Paginacion.Pagina(_datos, _pagina, filas))
        {
            _lista.Children.Add(Ui.Tarjeta(new Label
            {
                Text = $"{item.CodigoPublico} · {item.Vendedor} · {FormatoDinero.Pesos(item.Total)} · {item.Estado}",
                TextColor = Ui.Ink
            }));
        }
    }
}
