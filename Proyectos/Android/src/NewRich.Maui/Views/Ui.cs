using NewRich.Pda.Core;

namespace NewRich.Maui.Views;

public static class Ui
{
    public static readonly Color Ink = Color.FromArgb("#17212b");
    public static readonly Color Muted = Color.FromArgb("#687883");
    public static readonly Color Paper = Color.FromArgb("#f3f7f5");
    public static readonly Color Green = Color.FromArgb("#087f5b");
    public static readonly Color Dark = Color.FromArgb("#123d35");
    public static readonly Color Gold = Color.FromArgb("#f0b744");
    public static readonly Color Line = Color.FromArgb("#dce6e1");
    public static readonly Color Danger = Color.FromArgb("#bc4747");
    public static readonly Color DangerBg = Color.FromArgb("#fae5e3");
    public static readonly Color Mint = Color.FromArgb("#dff3e9");
    public static readonly Color InfoBg = Color.FromArgb("#e4f1fa");
    public static readonly Color Info = Color.FromArgb("#246b9f");
    public static readonly Color Warn = Color.FromArgb("#a9670c");
    public static readonly Color WarnBg = Color.FromArgb("#fbedd6");

    public static Label Titulo(string texto) => new()
    {
        Text = texto,
        FontSize = 22,
        FontAttributes = FontAttributes.Bold,
        TextColor = Colors.White
    };

    public static Label Marca(string texto) => new()
    {
        Text = texto,
        FontSize = 12,
        FontAttributes = FontAttributes.Bold,
        TextColor = Green,
        CharacterSpacing = 2
    };

    public static Label Campo(string texto) => new()
    {
        Text = texto,
        FontSize = 12,
        FontAttributes = FontAttributes.Bold,
        TextColor = Ink
    };

    public static Entry Entrada(string placeholder, bool password = false) => new()
    {
        Placeholder = placeholder,
        IsPassword = password,
        BackgroundColor = Color.FromArgb("#fbfdfb"),
        TextColor = Ink
    };

    public static Entry Entero(string placeholder, int? maxLength = null)
    {
        var entrada = Entrada(placeholder);
        entrada.Keyboard = Keyboard.Numeric;
        if (maxLength.HasValue)
        {
            entrada.MaxLength = maxLength.Value;
        }

        entrada.TextChanged += (_, args) =>
        {
            var limpio = EntradaEntera.SoloDigitos(args.NewTextValue);
            if (limpio == args.NewTextValue)
            {
                return;
            }

            entrada.Text = limpio;
        };
        return entrada;
    }

    public static Button Primario(string texto) => new()
    {
        Text = texto,
        BackgroundColor = Dark,
        TextColor = Colors.White,
        FontAttributes = FontAttributes.Bold,
        CornerRadius = 10,
        HeightRequest = 48
    };

    public static Button Secundario(string texto) => new()
    {
        Text = texto,
        BackgroundColor = Colors.White,
        TextColor = Dark,
        FontAttributes = FontAttributes.Bold,
        CornerRadius = 10,
        HeightRequest = 48,
        BorderColor = Line,
        BorderWidth = 1
    };

    public static Button Peligro(string texto) => new()
    {
        Text = texto,
        BackgroundColor = Danger,
        TextColor = Colors.White,
        FontAttributes = FontAttributes.Bold,
        CornerRadius = 10,
        HeightRequest = 44
    };

    public static Frame Banner(string texto, Color fondo, Color letra) => new()
    {
        BackgroundColor = fondo,
        BorderColor = Colors.Transparent,
        CornerRadius = 12,
        Padding = 12,
        Content = new Label { Text = texto, TextColor = letra, FontSize = 12 }
    };

    public static Frame Tarjeta(View contenido) => new()
    {
        BackgroundColor = Colors.White,
        BorderColor = Line,
        CornerRadius = 12,
        Padding = 16,
        Content = contenido
    };

    public static ScrollView Pagina(View contenido) => new()
    {
        BackgroundColor = Paper,
        Content = new VerticalStackLayout
        {
            Padding = new Thickness(16, 18, 16, 24),
            Spacing = 14,
            Children = { contenido }
        }
    };

    public static string EstadoLinea(string pda, bool conectado, bool horarioCerrado, int offline) =>
        $"PDA {pda} · {(conectado ? PdaTexts.Conectado : PdaTexts.SinConexion)} · {(horarioCerrado ? PdaTexts.HorarioCerrado : PdaTexts.HorarioAbierto)} · {PdaTexts.CodigosOffline}: {offline}";
}
