using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;

namespace NewRich.Maui.Views.Observador;

public sealed class CasosPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly VerticalStackLayout _lista = new() { Spacing = 8 };

    public CasosPage(NewRichApiClient api)
    {
        _api = api;
        Title = PdaTexts.CasosPremios;
        BackgroundColor = Ui.Paper;
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 10,
                Children =
                {
                    new Label { Text = PdaTexts.EntregaNoDisponible, FontSize = 12, TextColor = Ui.Muted },
                    _lista
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var resultado = await _api.PremiosAsync(CancellationToken.None);
        _lista.Children.Clear();
        if (!resultado.IsSuccess)
        {
            _lista.Children.Add(new Label { Text = resultado.Message, TextColor = Ui.Danger });
            return;
        }

        foreach (var caso in resultado.Data ?? [])
        {
            _lista.Children.Add(Ui.Tarjeta(new Label
            {
                Text = $"{caso.Ticket} · {caso.Estado} · {caso.Vendedor}",
                TextColor = Ui.Ink
            }));
        }
    }
}
