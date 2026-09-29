using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Maui.Views;

namespace NewRich.Maui.Views.Recaudador;

public sealed class RecaudadorMetricasPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly VerticalStackLayout _cuerpo = new() { Spacing = 10 };
    private readonly ActivityIndicator _spinner = new() { Color = Ui.Gold };

    public RecaudadorMetricasPage(NewRichApiClient api)
    {
        _api = api;
        Title = PdaTexts.MetricasRecaudo;
        Content = new ScrollView
        {
            BackgroundColor = Ui.Paper,
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 12,
                Children =
                {
                    new Label { Text = PdaTexts.MetricasRecaudo, FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink },
                    _spinner,
                    _cuerpo
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _spinner.IsRunning = _spinner.IsVisible = true;
        _cuerpo.Children.Clear();
        try
        {
            var metricas = await _api.MetricasRecaudoAsync(CancellationToken.None);
            var obligaciones = await _api.ObligacionesRecaudoAsync(null, CancellationToken.None);
            if (!metricas.IsSuccess || metricas.Data is null)
            {
                _cuerpo.Children.Add(new Label { Text = metricas.Message, TextColor = Ui.Danger });
                return;
            }

            var m = metricas.Data;
            _cuerpo.Children.Add(Tarjeta(PdaTexts.RecaudoDelDia, RecaudoPagoVista.Miles(m.TotalRecaudado)));
            _cuerpo.Children.Add(Tarjeta(PdaTexts.TotalPendiente, RecaudoPagoVista.Miles(m.TotalPendiente)));
            _cuerpo.Children.Add(Tarjeta(PdaTexts.DeudaAcumulada, RecaudoPagoVista.Miles(m.TotalPendiente), Ui.Danger));
            _cuerpo.Children.Add(new Label { Text = $"Al día {m.VendedoresAlDia} · En deuda {m.VendedoresEnDeuda} · Avance {m.PorcentajeRecaudado}%", TextColor = Ui.Ink });

            if (obligaciones.IsSuccess && obligaciones.Data is not null)
            {
                foreach (var grupo in RecaudoListas.Agrupar(obligaciones.Data))
                {
                    var porRecaudar = grupo.Sum(f => f.ValorACobrar);
                    var recaudado = grupo.Sum(f => f.PagosHoy);
                    var pendiente = grupo.Sum(f => f.TotalPendiente);
                    _cuerpo.Children.Add(new Label { Text = grupo.Key, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink, Margin = new Thickness(0, 8, 0, 0) });
                    _cuerpo.Children.Add(Barra(recaudado, porRecaudar <= 0m ? 1m : porRecaudar));
                    _cuerpo.Children.Add(new Label
                    {
                        Text = $"Debía {RecaudoPagoVista.Miles(porRecaudar)} · Recaudado {RecaudoPagoVista.Miles(recaudado)} · Pendiente {RecaudoPagoVista.Miles(pendiente)}",
                        FontSize = 12,
                        TextColor = Ui.Muted
                    });
                }
            }
        }
        finally
        {
            _spinner.IsRunning = _spinner.IsVisible = false;
        }
    }

    private static View Tarjeta(string titulo, string valor, Color? color = null) => new Frame
    {
        BackgroundColor = Colors.White,
        BorderColor = Ui.Line,
        CornerRadius = 12,
        Padding = 14,
        Content = new VerticalStackLayout
        {
            Children =
            {
                new Label { Text = titulo, TextColor = Ui.Muted, FontSize = 12 },
                new Label { Text = valor, FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = color ?? Ui.Ink }
            }
        }
    };

    private static View Barra(decimal recaudado, decimal total)
    {
        var ratio = (double)Math.Clamp(total == 0m ? 0m : recaudado / total, 0m, 1m);
        return new Grid
        {
            HeightRequest = 14,
            Children =
            {
                new BoxView { Color = Ui.Line, CornerRadius = 7 },
                new BoxView { Color = Ui.Green, CornerRadius = 7, HorizontalOptions = LayoutOptions.Start, WidthRequest = Math.Max(8, 280 * ratio) }
            }
        };
    }
}
