using Microsoft.Maui.Controls.Shapes;
using NewRich.Pda.Core;

namespace NewRich.Maui.Views;

public sealed class CargandoOverlay : Grid
{
    private static readonly Color Tarjeta = Color.FromArgb("#141c16");
    private static readonly Color Oro = Color.FromArgb("#e0c36a");
    private static readonly Color Texto = Color.FromArgb("#f4efe4");

    private readonly Label _mensaje = new()
    {
        FontSize = 13,
        TextColor = Texto,
        HorizontalTextAlignment = TextAlignment.Center
    };
    private readonly ActivityIndicator _indicador = new()
    {
        Color = Oro,
        IsRunning = false,
        WidthRequest = 36,
        HeightRequest = 36,
        HorizontalOptions = LayoutOptions.Center
    };

    public CargandoOverlay()
    {
        IsVisible = false;
        InputTransparent = true;
        ZIndex = 100;
        BackgroundColor = Color.FromArgb("#66000000");
        Children.Add(new Border
        {
            BackgroundColor = Tarjeta,
            Stroke = Color.FromArgb("#33e0c36a"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Padding = new Thickness(28, 22),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                HorizontalOptions = LayoutOptions.Center,
                Children = { _indicador, _mensaje }
            }
        });
    }

    public void Mostrar(string? mensaje = null)
    {
        _mensaje.Text = string.IsNullOrWhiteSpace(mensaje) ? PdaTexts.Cargando : mensaje;
        _indicador.IsVisible = true;
        _indicador.IsRunning = true;
        IsVisible = true;
        InputTransparent = false;
        Opacity = 1;
    }

    public void Ocultar()
    {
        _indicador.IsRunning = false;
        IsVisible = false;
        InputTransparent = true;
    }
}
