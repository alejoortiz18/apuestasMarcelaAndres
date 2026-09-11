using Microsoft.Maui.Controls.Shapes;
using NewRich.Pda.Core;

namespace NewRich.Maui.Views;

public sealed class CargandoOverlay : Grid
{
    private static readonly Color Velo = Color.FromArgb("#99000000");
    private static readonly Color Tarjeta = Color.FromArgb("#141c16");
    private static readonly Color Oro = Color.FromArgb("#e0c36a");
    private static readonly Color Texto = Color.FromArgb("#f4efe4");

    private readonly Label _mensaje = new()
    {
        Style = null,
        FontSize = 13,
        TextColor = Texto,
        HorizontalTextAlignment = TextAlignment.Center
    };
    private readonly Border _anillo;
    private bool _girando;

    public CargandoOverlay()
    {
        IsVisible = false;
        InputTransparent = true;
        ZIndex = 100;
        BackgroundColor = Velo;

        _anillo = new Border
        {
            Style = null,
            WidthRequest = 72,
            HeightRequest = 72,
            BackgroundColor = Colors.Transparent,
            Stroke = Oro,
            StrokeThickness = 2.4,
            StrokeDashArray = new DoubleCollection { 18, 10 },
            StrokeShape = new Ellipse(),
            Padding = 0,
            HorizontalOptions = LayoutOptions.Center
        };

        var corona = new Label
        {
            Style = null,
            Text = "♛",
            FontSize = 28,
            TextColor = Oro,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        var marca = new Grid
        {
            WidthRequest = 72,
            HeightRequest = 72,
            HorizontalOptions = LayoutOptions.Center,
            Children = { _anillo, corona }
        };

        var tarjeta = new Border
        {
            Style = null,
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
                Children = { marca, _mensaje }
            }
        };

        Children.Add(tarjeta);
    }

    public void Mostrar(string? mensaje = null)
    {
        _mensaje.Text = string.IsNullOrWhiteSpace(mensaje) ? PdaTexts.Cargando : mensaje;
        IsVisible = true;
        InputTransparent = false;
        if (_girando)
        {
            return;
        }

        _girando = true;
        _ = GirarAsync();
    }

    public void Ocultar()
    {
        _girando = false;
        IsVisible = false;
        InputTransparent = true;
        _anillo.CancelAnimations();
        _anillo.Rotation = 0;
    }

    private async Task GirarAsync()
    {
        while (_girando)
        {
            await _anillo.RotateToAsync(360, 900, Easing.Linear);
            _anillo.Rotation = 0;
        }
    }
}
