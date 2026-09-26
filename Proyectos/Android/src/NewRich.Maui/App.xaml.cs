namespace NewRich.Maui;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        _services = services;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var ventana = new Window(_services.GetRequiredService<Views.Shared.ArranquePage>());
        ventana.Created += (_, _) =>
            _services.GetRequiredService<Services.VigilanteInactividad>().Iniciar(ventana.Dispatcher);
        return ventana;
    }
}
