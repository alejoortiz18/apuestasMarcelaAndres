using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Maui.Views;
using NewRich.Maui.Views.Shared;

namespace NewRich.Maui.Views.Recaudador;

public sealed class RecaudadorShell : Shell
{
    public RecaudadorShell(IServiceProvider services)
    {
        FlyoutBehavior = FlyoutBehavior.Disabled;
        Shell.SetTabBarIsVisible(this, false);
        Items.Add(new TabBar
        {
            Items =
            {
                Contenido(PdaTexts.Inicio, "recaudo", () => services.GetRequiredService<RecaudadorHomePage>()),
                Contenido(PdaTexts.HistorialRecaudo, "rhistorial", () => services.GetRequiredService<RecaudadorHistorialPage>()),
                Contenido(PdaTexts.MetricasRecaudo, "rmetricas", () => services.GetRequiredService<RecaudadorMetricasPage>()),
                Contenido(PdaTexts.Soporte, "rsoporte", () => services.GetRequiredService<SoportePage>()),
                Contenido(PdaTexts.Mas, "rmas", () => services.GetRequiredService<RecaudadorMasPage>())
            }
        });
        Navigated += (_, args) =>
            BarraMenuRecaudador.Asegurar(CurrentPage, args.Current?.Location?.OriginalString);
        Loaded += (_, _) =>
            BarraMenuRecaudador.Asegurar(CurrentPage, CurrentState?.Location?.OriginalString);
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
