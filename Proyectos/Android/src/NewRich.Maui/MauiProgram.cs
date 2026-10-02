using Microsoft.Extensions.Logging;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;
using NewRich.Maui.Data;
using NewRich.Maui.Services;
using NewRich.Maui.Views.Observador;
using NewRich.Maui.Views.Recaudador;
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
        RecargarIdentidad();
        builder.Services.AddSingleton<ApiOpciones>(_ => new ApiOpciones
        {
            BaseUrl = PdaConexion.BaseUrl(DeviceInfo.Current.DeviceType == DeviceType.Virtual)
        });
        builder.Services.AddSingleton<ITokenStore, SecureTokenStore>();
        builder.Services.AddSingleton(_ => new HttpClient(ConexionHttpPda.CrearControlador()) { Timeout = TimeSpan.FromSeconds(30) });
        builder.Services.AddSingleton<NewRichApiClient>();
        builder.Services.AddSingleton<LocalDatabase>();
        builder.Services.AddSingleton<SincronizacionOfflineServicio>();
        builder.Services.AddSingleton<IPrinterService, PrinterService>();
        builder.Services.AddSingleton<IPdfService, PdfService>();
        builder.Services.AddSingleton<ILectorCodigoBarrasServicio, LectorCodigoBarrasServicio>();
        builder.Services.AddSingleton<IEscanerQrVendedor, EscanerQrVendedorServicio>();
        builder.Services.AddSingleton<ILectorQrFotoVendedor, LectorQrFotoVendedorServicio>();
        builder.Services.AddSingleton<IEscanerQrCamara, EscanerQrCamaraServicio>();
        builder.Services.AddSingleton<IEscanerQrObservador, EscanerQrObservadorServicio>();
        builder.Services.AddSingleton<ILectorQrFotoObservador, LectorQrFotoObservadorServicio>();
        builder.Services.AddSingleton<NavegadorApp>();
        builder.Services.AddSingleton<VigilanteInactividad>();

        builder.Services.AddSingleton<ChatEnVivoServicio>();
        builder.Services.AddSingleton<ActualizacionEnVivoServicio>();
        builder.Services.AddSingleton<CodigosOfflineEnVivoServicio>();
        builder.Services.AddSingleton<LoteriasEnVivoServicio>();
        builder.Services.AddTransient<ArranquePage>();
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
        builder.Services.AddTransient<SoporteTecnicoPage>();
        builder.Services.AddTransient<ConfiguracionPage>();
        builder.Services.AddTransient<SincronizacionPage>();
        builder.Services.AddTransient<MasPage>();
        builder.Services.AddTransient<VendedorShell>();
        builder.Services.AddTransient<ObservadorHomePage>();
        builder.Services.AddTransient<ObservadorValidarPage>();
        builder.Services.AddTransient<ConsultasPage>();
        builder.Services.AddTransient<KpiPage>();
        builder.Services.AddTransient<ObservadorMasPage>();
        builder.Services.AddTransient<CasosPage>();
        builder.Services.AddTransient<ObservadorShell>();
        builder.Services.AddTransient<RecaudadorHomePage>();
        builder.Services.AddTransient<RecaudadorCobroPage>();
        builder.Services.AddTransient<RecaudadorHistorialPage>();
        builder.Services.AddTransient<RecaudadorMetricasPage>();
        builder.Services.AddTransient<RecaudadorMasPage>();
        builder.Services.AddTransient<RecaudadorSincronizacionPage>();
        builder.Services.AddTransient<RecaudadorShell>();

        var app = builder.Build();
        _ = app.Services.GetRequiredService<ActualizacionEnVivoServicio>();
        return app;
    }

    public static void RecargarIdentidad()
    {
        PdaConexion.CodigoDispositivo = PdaConexion.DesdeFuentes(
            LeerArchivoIdentidad(),
            LeerAjusteGlobal(),
            DeviceInfo.Current.Model);
        PdaConexion.NumeroSerie = LeerArchivoSerie();
    }

    /// <summary>
    /// Identidad que el registro deja en el almacenamiento del paquete. En Android 14 la
    /// aplicación no puede leer el ajuste global donde también se intenta grabar.
    /// </summary>
    private static string? LeerArchivoIdentidad()
    {
#if ANDROID
        try
        {
            var contexto = global::Android.App.Application.Context;
            var internas = contexto?.FilesDir?.AbsolutePath;
            var externas = contexto?.GetExternalFilesDir(null)?.AbsolutePath;
            foreach (var ruta in PdaConexion.RutasArchivoIdentidad(internas, externas))
            {
                var texto = LeerSiExiste(ruta);
                if (texto is not null)
                {
                    return texto;
                }
            }
        }
        catch (Exception)
        {
            return null;
        }
#endif
        return null;
    }

    private static string? LeerAjusteGlobal()
    {
#if ANDROID
        try
        {
            return global::Android.Provider.Settings.Global.GetString(
                global::Android.App.Application.Context!.ContentResolver,
                PdaConexion.ClaveCodigoProvisionado);
        }
        catch (Exception)
        {
            return null;
        }
#else
        return null;
#endif
    }

    private static string? LeerArchivoSerie()
    {
#if ANDROID
        try
        {
            var contexto = global::Android.App.Application.Context;
            foreach (var ruta in PdaConexion.RutasArchivoSerie(
                contexto?.FilesDir?.AbsolutePath,
                contexto?.GetExternalFilesDir(null)?.AbsolutePath))
            {
                var texto = LeerSiExiste(ruta);
                if (!string.IsNullOrWhiteSpace(texto))
                {
                    return texto.Trim();
                }
            }
        }
        catch (Exception)
        {
            return null;
        }
#endif
        return null;
    }

    private static string? LeerSiExiste(string ruta)
    {
        try
        {
            return File.Exists(ruta) ? File.ReadAllText(ruta) : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
