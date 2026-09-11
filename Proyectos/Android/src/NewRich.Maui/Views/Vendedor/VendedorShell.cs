using NewRich.Pda.Core;
using NewRich.Maui.Views.Shared;

namespace NewRich.Maui.Views.Vendedor;

public sealed class VendedorShell : Shell
{
    public VendedorShell(IServiceProvider services)
    {
        FlyoutBehavior = FlyoutBehavior.Disabled;
        Shell.SetTabBarIsVisible(this, false);
        Items.Add(new TabBar
        {
            Items =
            {
                Contenido(PdaTexts.Inicio, "inicio", () => services.GetRequiredService<VendedorHomePage>()),
                Contenido(PdaTexts.Vender, "vender", () => services.GetRequiredService<TipoApuestaPage>()),
                Contenido(PdaTexts.Historico, "historico", () => services.GetRequiredService<HistoricoPage>()),
                Contenido(PdaTexts.Soporte, "soporte", () => services.GetRequiredService<SoportePage>()),
                Contenido(PdaTexts.Mas, "mas", () => services.GetRequiredService<MasPage>())
            }
        });
        Navigated += (_, args) =>
            BarraMenuVendedor.Asegurar(CurrentPage, args.Current?.Location?.OriginalString);
        Loaded += (_, _) =>
            BarraMenuVendedor.Asegurar(CurrentPage, CurrentState?.Location?.OriginalString);
    }

    private static ShellContent Contenido(string titulo, string ruta, Func<Page> factory)
    {
        var contenido = new ShellContent
        {
            Title = titulo,
            Route = ruta,
            ContentTemplate = new DataTemplate(factory)
        };
        Shell.SetTabBarIsVisible(contenido, false);
        return contenido;
    }
}
