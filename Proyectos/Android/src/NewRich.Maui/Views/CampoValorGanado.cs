using NewRich.Pda.Core;

namespace NewRich.Maui.Views;

/// <summary>
/// Campo numérico para el valor ganado. El teclado es numérico y solo guarda dígitos;
/// los puntos de miles se muestran encima, sin escribirse en el Entry nativo (en Android
/// eso tumba la app). Al leer <see cref="Texto"/> siguen los dígitos para el backend.
/// </summary>
public sealed class CampoValorGanado : Grid
{
    private readonly Label _vista;
    private readonly string _placeholder;

    public CampoValorGanado(string placeholder)
    {
        _placeholder = placeholder;
        BackgroundColor = Color.FromArgb("#fbfdfb");
        MinimumHeightRequest = 44;

        _vista = new Label
        {
            Text = placeholder,
            TextColor = Ui.Muted,
            FontSize = 16,
            VerticalTextAlignment = TextAlignment.Center,
            Padding = new Thickness(12, 10),
            InputTransparent = true
        };

        Interno = new Entry
        {
            Keyboard = Keyboard.Numeric,
            TextColor = Colors.Transparent,
            PlaceholderColor = Colors.Transparent,
            BackgroundColor = Colors.Transparent,
            Placeholder = placeholder
        };
        Interno.TextChanged += AlDigitar;
        Interno.HandlerChanged += OcultarCursorNativo;

        Children.Add(_vista);
        Children.Add(Interno);
    }

    public Entry Interno { get; }

    public string? Texto => Interno.Text;

    public event EventHandler? TextoCambiado;

    private void AlDigitar(object? sender, TextChangedEventArgs args)
    {
        var limpio = EntradaEntera.SoloDigitos(args.NewTextValue);
        if (limpio != args.NewTextValue)
        {
            Interno.Text = limpio;
            return;
        }

        if (string.IsNullOrEmpty(limpio))
        {
            _vista.Text = _placeholder;
            _vista.TextColor = Ui.Muted;
        }
        else
        {
            _vista.Text = EntradaEntera.ConPuntosDeMil(limpio);
            _vista.TextColor = Ui.Ink;
        }

        TextoCambiado?.Invoke(this, EventArgs.Empty);
    }

    private static void OcultarCursorNativo(object? sender, EventArgs e)
    {
#if ANDROID
        if (sender is Entry entrada && entrada.Handler?.PlatformView is Android.Widget.EditText nativo)
        {
            nativo.SetCursorVisible(false);
        }
#endif
    }
}
