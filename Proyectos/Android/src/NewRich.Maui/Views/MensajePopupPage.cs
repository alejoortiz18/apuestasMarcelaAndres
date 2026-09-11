using Microsoft.Maui.Controls.Shapes;
using NewRich.Pda.Core;

namespace NewRich.Maui.Views;

public sealed class MensajePopupPage : ContentPage
{
    private static readonly Color Velo = Color.FromArgb("#99000000");
    private static readonly Color Tarjeta = Color.FromArgb("#0c1c18");
    private static readonly Color Oro = Color.FromArgb("#e8c56b");
    private static readonly Color TintaOro = Color.FromArgb("#1a1408");
    private static readonly Color TituloColor = Color.FromArgb("#f4efe4");
    private static readonly Color CuerpoColor = Color.FromArgb("#c5cfc8");

    private readonly TaskCompletionSource<bool> _resultado = new();

    public MensajePopupPage(MensajeEmergente mensaje)
    {
        BackgroundColor = Velo;
        NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);

        var icono = new Border
        {
            Style = null,
            WidthRequest = 52,
            HeightRequest = 52,
            Stroke = Oro,
            StrokeThickness = 2,
            BackgroundColor = Colors.Transparent,
            StrokeShape = new Ellipse(),
            HorizontalOptions = LayoutOptions.Center,
            Content = new Label
            {
                Style = null,
                Text = "!",
                FontSize = 26,
                FontAttributes = FontAttributes.Bold,
                TextColor = Oro,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };

        var titulo = new Label
        {
            Style = null,
            Text = mensaje.Titulo,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = TituloColor,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var cuerpo = new Label
        {
            Style = null,
            Text = mensaje.Cuerpo,
            FontSize = 14,
            TextColor = CuerpoColor,
            HorizontalTextAlignment = TextAlignment.Center,
            LineHeight = 1.2
        };

        var botones = new Grid
        {
            ColumnSpacing = 10,
            Margin = new Thickness(0, 8, 0, 0)
        };

        if (mensaje.EsConfirmacion)
        {
            botones.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            botones.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var no = BotonContorno(mensaje.Secundario!);
            no.Clicked += (_, _) => Completar(false);
            var si = BotonOro(mensaje.Principal);
            si.Clicked += (_, _) => Completar(true);
            botones.Add(no, 0);
            botones.Add(si, 1);
        }
        else
        {
            botones.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var ok = BotonOro(mensaje.Principal);
            ok.Clicked += (_, _) => Completar(true);
            botones.Add(ok);
        }

        var tarjeta = new Border
        {
            Style = null,
            BackgroundColor = Tarjeta,
            Stroke = Color.FromArgb("#66e8c56b"),
            StrokeThickness = 1.2,
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            Padding = new Thickness(22, 24, 22, 18),
            Margin = new Thickness(28, 0),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children = { icono, titulo, cuerpo, botones }
            }
        };

        Content = tarjeta;
    }

    public Task<bool> Resultado => _resultado.Task;

    protected override bool OnBackButtonPressed()
    {
        Completar(false);
        return true;
    }

    private void Completar(bool valor)
    {
        _resultado.TrySetResult(valor);
    }

    private static Button BotonOro(string texto) => new()
    {
        Style = null,
        Text = texto,
        BackgroundColor = Oro,
        TextColor = TintaOro,
        FontAttributes = FontAttributes.Bold,
        FontSize = 14,
        CornerRadius = 18,
        HeightRequest = 46,
        Padding = new Thickness(8, 0)
    };

    private static Button BotonContorno(string texto) => new()
    {
        Style = null,
        Text = texto,
        BackgroundColor = Colors.Transparent,
        TextColor = Oro,
        FontAttributes = FontAttributes.Bold,
        FontSize = 14,
        CornerRadius = 18,
        HeightRequest = 46,
        BorderColor = Oro,
        BorderWidth = 1.4,
        Padding = new Thickness(8, 0)
    };
}

public static class PaginaMensajes
{
    public static Task AvisoAsync(this Page pagina, string titulo, string cuerpo, string boton) =>
        MostrarAsync(pagina, MensajeEmergente.Aviso(titulo, cuerpo, boton));

    public static Task<bool> ConfirmarAsync(this Page pagina, string titulo, string cuerpo, string principal, string secundario) =>
        MostrarAsync(pagina, MensajeEmergente.Confirmar(titulo, cuerpo, principal, secundario));

    private static async Task<bool> MostrarAsync(Page pagina, MensajeEmergente mensaje)
    {
        var popup = new MensajePopupPage(mensaje);
        await pagina.Navigation.PushModalAsync(popup, false);
        var resultado = await popup.Resultado;
        await pagina.Navigation.PopModalAsync(false);
        return resultado;
    }
}
