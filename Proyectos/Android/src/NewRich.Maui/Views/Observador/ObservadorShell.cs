using NewRich.Pda.Core;
using NewRich.Maui.Views.Shared;

namespace NewRich.Maui.Views.Observador;

public sealed class ObservadorShell : Shell
{
    public ObservadorShell(IServiceProvider services)
    {
        FlyoutBehavior = FlyoutBehavior.Disabled;
        Items.Add(new TabBar
        {
            Items =
            {
                new ShellContent { Title = PdaTexts.Inicio, Route = "oinicio", ContentTemplate = new DataTemplate(() => services.GetRequiredService<ObservadorHomePage>()) },
                new ShellContent { Title = PdaTexts.Consultas, Route = "consultas", ContentTemplate = new DataTemplate(() => services.GetRequiredService<ConsultasPage>()) },
                new ShellContent { Title = PdaTexts.CasosPremios, Route = "casos", ContentTemplate = new DataTemplate(() => services.GetRequiredService<CasosPage>()) },
                new ShellContent { Title = PdaTexts.Soporte, Route = "osoporte", ContentTemplate = new DataTemplate(() => services.GetRequiredService<SoportePage>()) }
            }
        });
    }
}
