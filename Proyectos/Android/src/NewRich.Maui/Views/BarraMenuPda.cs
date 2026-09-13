using Microsoft.Maui.Controls.Shapes;
using NewRich.Pda.Core;

namespace NewRich.Maui.Views;

public sealed class BarraMenuPda : Border
{
    private static readonly Color FondoBarra = Color.FromArgb("#0B1A14");
    private static readonly Color Oro = Color.FromArgb("#D4B15A");
    private static readonly Color Crema = Color.FromArgb("#F0E6C8");
    private static readonly Color FondoActivo = Color.FromArgb("#163028");

    public BarraMenuPda(string rutaActiva, IReadOnlyList<ItemMenuInferior> items)
    {
        Stroke = Oro;
        StrokeThickness = 1.5;
        BackgroundColor = FondoBarra;
        StrokeShape = new RoundRectangle { CornerRadius = 28 };
        Padding = new Thickness(6, 8);
        Margin = new Thickness(10, 0, 10, 12);
        HorizontalOptions = LayoutOptions.Fill;
        Shadow = new Shadow
        {
            Brush = Color.FromArgb("#66000000"),
            Offset = new Point(0, 4),
            Radius = 12,
            Opacity = 0.45f
        };

        var fila = new Grid
        {
            ColumnSpacing = 2,
            HeightRequest = 64
        };
        for (var i = 0; i < items.Count; i++)
        {
            fila.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var item = items[i];
            var activo = string.Equals(item.Ruta, rutaActiva, StringComparison.OrdinalIgnoreCase);
            var celda = CrearCelda(item, activo);
            Grid.SetColumn(celda, i);
            fila.Add(celda);
        }

        Content = fila;
    }

    public static void Asegurar(
        Page? pagina,
        string? ubicacion,
        IReadOnlyList<ItemMenuInferior> items,
        Func<string?, string> rutaActiva,
        string capaId)
    {
        if (pagina is not ContentPage contenido || contenido.Content is null)
        {
            return;
        }

        Shell.SetTabBarIsVisible(contenido, false);
        var ruta = rutaActiva(ubicacion);
        if (contenido.Content is Grid capa && string.Equals(capa.ClassId, capaId, StringComparison.Ordinal)
            && capa.RowDefinitions.Count >= 2)
        {
            foreach (var anterior in capa.Children.OfType<BarraMenuPda>().ToList())
            {
                capa.Remove(anterior);
            }

            var barra = new BarraMenuPda(ruta, items);
            Grid.SetRow(barra, 1);
            capa.Add(barra);
            return;
        }

        var original = ExtraerContenido(contenido, capaId);
        if (original is null)
        {
            return;
        }

        original.VerticalOptions = LayoutOptions.Fill;
        var nueva = new Grid { ClassId = capaId };
        nueva.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        nueva.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        Grid.SetRow(original, 0);
        nueva.Add(original);
        var menu = new BarraMenuPda(ruta, items);
        Grid.SetRow(menu, 1);
        nueva.Add(menu);
        contenido.Content = nueva;
    }

    private static View? ExtraerContenido(ContentPage pagina, string capaId)
    {
        if (pagina.Content is Grid capa && string.Equals(capa.ClassId, capaId, StringComparison.Ordinal))
        {
            return capa.Children.OfType<View>().FirstOrDefault(v => v is not BarraMenuPda);
        }

        return pagina.Content;
    }

    private static View CrearCelda(ItemMenuInferior item, bool activo)
    {
        var color = activo ? Oro : Crema;
        var pila = new VerticalStackLayout
        {
            Spacing = 4,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                Icono(item.Icono, color),
                new Label
                {
                    Text = item.Titulo,
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = color,
                    HorizontalTextAlignment = TextAlignment.Center
                }
            }
        };

        View tappable = activo
            ? new Border
            {
                Stroke = Oro,
                StrokeThickness = 1.4,
                BackgroundColor = FondoActivo,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(8, 6),
                HorizontalOptions = LayoutOptions.Center,
                Content = pila
            }
            : pila;

        var toque = new TapGestureRecognizer();
        toque.Tapped += async (_, _) =>
        {
            if (Shell.Current is null)
            {
                return;
            }

            await Shell.Current.GoToAsync($"//{item.Ruta}");
        };
        tappable.GestureRecognizers.Add(toque);
        return tappable;
    }

    private static View Icono(IconoMenuInferior icono, Color color)
    {
        var path = new Microsoft.Maui.Controls.Shapes.Path
        {
            Stroke = color,
            StrokeThickness = 1.7,
            StrokeLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            Aspect = Stretch.Uniform,
            HeightRequest = 22,
            WidthRequest = 22,
            HorizontalOptions = LayoutOptions.Center,
            Data = Geometry(icono)
        };
        return path;
    }

    private static Geometry Geometry(IconoMenuInferior icono)
    {
        var datos = icono switch
        {
            IconoMenuInferior.Casa => "M3 10.5 L12 3 L21 10.5 V20 H14 V13 H10 V20 H3 Z",
            IconoMenuInferior.Corona => "M3 18 H21 M4 18 L6 8 L10 13 L12 6 L14 13 L18 8 L20 18 Z",
            IconoMenuInferior.Documento => "M7 3 H14 L19 8 V21 H7 Z M14 3 V8 H19 M9 12 H17 M9 16 H15",
            IconoMenuInferior.Auricular => "M5 14 V12 C5 7 8.5 4 12 4 C15.5 4 19 7 19 12 V14 M5 14 H8 V20 H5 Z M16 14 H19 V20 H16 Z",
            _ => "M12 3 C7 3 4 7 4 12 C4 17 7 21 12 21 C17 21 20 17 20 12 C20 7 17 3 12 3 Z M4 12 H20 M12 3 C10 8 10 16 12 21 C14 16 14 8 12 3"
        };
        return (Geometry)new PathGeometryConverter().ConvertFromInvariantString(datos)!;
    }
}

public static class BarraMenuVendedor
{
    public static void Asegurar(Page? pagina, string? ubicacion) =>
        BarraMenuPda.Asegurar(
            pagina,
            ubicacion,
            MenuInferiorVendedor.Items,
            MenuInferiorVendedor.RutaActiva,
            MenuInferiorVendedor.CapaId);
}

public static class BarraMenuObservador
{
    public static void Asegurar(Page? pagina, string? ubicacion) =>
        BarraMenuPda.Asegurar(
            pagina,
            ubicacion,
            MenuInferiorObservador.Items,
            MenuInferiorObservador.RutaActiva,
            MenuInferiorObservador.CapaId);
}
