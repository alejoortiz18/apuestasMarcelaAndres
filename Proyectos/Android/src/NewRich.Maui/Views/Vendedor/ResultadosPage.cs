using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;

namespace NewRich.Maui.Views.Vendedor;

public sealed class ResultadosPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly DatePicker _fecha = new() { Date = DateTime.Today };
    private readonly VerticalStackLayout _lista = new() { Spacing = 8 };

    public ResultadosPage(NewRichApiClient api)
    {
        _api = api;
        Title = PdaTexts.ResultadosTitulo;
        BackgroundColor = Ui.Paper;
        var buscar = Ui.Primario(PdaTexts.Buscar);
        buscar.Clicked += async (_, _) => await CargarAsync();
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 10,
                Children =
                {
                    new Label { Text = PdaTexts.ResultadosSoloLectura, TextColor = Ui.Muted, FontSize = 12 },
                    Ui.Campo(PdaTexts.Fecha),
                    _fecha,
                    buscar,
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
        var resultado = await _api.ResultadosAsync(DateOnly.FromDateTime(_fecha.Date ?? DateTime.Today), null, CancellationToken.None);
        _lista.Children.Clear();
        if (!resultado.IsSuccess)
        {
            _lista.Children.Add(new Label { Text = resultado.Message, TextColor = Ui.Danger });
            return;
        }

        if (resultado.Data is null || resultado.Data.Count == 0)
        {
            _lista.Children.Add(new Label { Text = PdaTexts.SinResultados, TextColor = Ui.Muted });
            return;
        }

        foreach (var item in resultado.Data)
        {
            _lista.Children.Add(Ui.Tarjeta(new Label
            {
                Text = $"{item.FechaJuego:yyyy-MM-dd} · {item.Loteria} · {item.Numero}",
                TextColor = Ui.Ink
            }));
        }
    }
}
