using NewRich.Pda.Core;
using NewRich.Maui.Views.Shared;

namespace NewRich.Maui.Views.Vendedor;

public sealed class VendedorShell : Shell
{
    public VendedorShell(IServiceProvider services)
    {
        FlyoutBehavior = FlyoutBehavior.Disabled;
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
    }

    private static ShellContent Contenido(string titulo, string ruta, Func<Page> factory) =>
        new()
        {
            Title = titulo,
            Route = ruta,
            ContentTemplate = new DataTemplate(factory)
        };
}
