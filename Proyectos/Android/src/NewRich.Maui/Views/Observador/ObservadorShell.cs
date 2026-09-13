using NewRich.Pda.Core;
using NewRich.Maui.Views;
using NewRich.Maui.Views.Shared;

namespace NewRich.Maui.Views.Observador;

public sealed class ObservadorShell : Shell
{
    public ObservadorShell(IServiceProvider services)
    {
        FlyoutBehavior = FlyoutBehavior.Disabled;
        Shell.SetTabBarIsVisible(this, false);
        Items.Add(new TabBar
        {
            Items =
            {
                Contenido(PdaTexts.Inicio, "oinicio", () => services.GetRequiredService<ObservadorHomePage>()),
                Contenido(PdaTexts.Validar, "ovalidar", () => services.GetRequiredService<ObservadorValidarPage>()),
                Contenido(PdaTexts.Consultas, "consultas", () => services.GetRequiredService<ConsultasPage>()),
                Contenido(PdaTexts.Soporte, "osoporte", () => services.GetRequiredService<SoportePage>()),
                Contenido(PdaTexts.Mas, "omas", () => services.GetRequiredService<ObservadorMasPage>())
            }
        });
        Navigated += (_, args) =>
            BarraMenuObservador.Asegurar(CurrentPage, args.Current?.Location?.OriginalString);
        Loaded += (_, _) =>
            BarraMenuObservador.Asegurar(CurrentPage, CurrentState?.Location?.OriginalString);
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
