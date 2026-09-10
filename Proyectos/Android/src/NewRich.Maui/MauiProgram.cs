using Microsoft.Extensions.Logging;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;
using NewRich.Maui.Data;
using NewRich.Maui.Services;
using NewRich.Maui.Views.Observador;
using NewRich.Maui.Views.Shared;
using NewRich.Maui.Views.Vendedor;

namespace NewRich.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<SesionPda>();
        builder.Services.AddSingleton<ApiOpciones>(_ => new ApiOpciones
        {
            BaseUrl = PdaConexion.BaseUrl(DeviceInfo.Current.DeviceType == DeviceType.Virtual)
        });
        builder.Services.AddSingleton<ITokenStore, SecureTokenStore>();
        builder.Services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(8) });
        builder.Services.AddSingleton<NewRichApiClient>();
        builder.Services.AddSingleton<LocalDatabase>();
        builder.Services.AddSingleton<IPrinterService, PrinterService>();
        builder.Services.AddSingleton<IPdfService, PdfService>();
        builder.Services.AddSingleton<NavegadorApp>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<PasswordPage>();
        builder.Services.AddTransient<VendedorHomePage>();
        builder.Services.AddTransient<TipoApuestaPage>();
        builder.Services.AddTransient<ConstruirApuestaPage>();
        builder.Services.AddTransient<TirillaVendidaPage>();
        builder.Services.AddTransient<HistoricoPage>();
        builder.Services.AddTransient<ResultadosPage>();
        builder.Services.AddTransient<ValidarTicketPage>();
        builder.Services.AddTransient<SoportePage>();
        builder.Services.AddTransient<ConfiguracionPage>();
        builder.Services.AddTransient<MasPage>();
        builder.Services.AddTransient<VendedorShell>();
        builder.Services.AddTransient<ObservadorHomePage>();
        builder.Services.AddTransient<ConsultasPage>();
        builder.Services.AddTransient<CasosPage>();
        builder.Services.AddTransient<ObservadorShell>();

        return builder.Build();
    }
}
