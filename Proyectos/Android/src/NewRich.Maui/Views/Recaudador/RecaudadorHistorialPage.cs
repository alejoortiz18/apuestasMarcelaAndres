using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Maui.Views;

namespace NewRich.Maui.Views.Recaudador;

public sealed class RecaudadorHistorialPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly VerticalStackLayout _lista = new() { Spacing = 8 };
    private readonly ActivityIndicator _spinner = new() { Color = Ui.Gold };

    public RecaudadorHistorialPage(NewRichApiClient api)
    {
        _api = api;
        Title = PdaTexts.HistorialRecaudo;
        Content = new ScrollView
        {
            BackgroundColor = Ui.Paper,
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 10,
                Children =
                {
                    new Label { Text = PdaTexts.HistorialRecaudo, FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink },
                    _spinner,
                    _lista
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _spinner.IsRunning = _spinner.IsVisible = true;
        _lista.Children.Clear();
        try
        {
            var historial = await _api.HistorialRecaudoAsync(CancellationToken.None);
            if (!historial.IsSuccess)
            {
                _lista.Children.Add(new Label { Text = historial.Message, TextColor = Ui.Danger });
                return;
            }

            if (historial.Data is null || historial.Data.Count == 0)
            {
                _lista.Children.Add(new Label { Text = PdaTexts.SinCobrosHistorial, TextColor = Ui.Muted });
                return;
            }

            foreach (var movimiento in historial.Data)
            {
                _lista.Children.Add(new Frame
                {
                    BackgroundColor = Colors.White,
                    BorderColor = Ui.Line,
                    CornerRadius = 12,
                    Padding = 12,
                    Content = new VerticalStackLayout
                    {
                        Children =
                        {
                            new Label { Text = movimiento.Vendedor, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink },
                            new Label { Text = $"{movimiento.FechaHora:yyyy-MM-dd HH:mm} · {movimiento.Grupo}", TextColor = Ui.Muted, FontSize = 12 },
                            new Label { Text = $"Recibido {RecaudoPagoVista.Miles(movimiento.ValorRecibido)} · Saldo {RecaudoPagoVista.Miles(movimiento.SaldoResultante)}", TextColor = Ui.Ink },
                            new Label { Text = movimiento.Estado, FontAttributes = FontAttributes.Bold, TextColor = Ui.Dark }
                        }
                    }
                });
            }
        }
        finally
        {
            _spinner.IsRunning = _spinner.IsVisible = false;
        }
    }
}
