using NewRich.Domain.Enums;
using NewRich.Maui.Data;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;

namespace NewRich.Maui.Services;

public sealed class SecureTokenStore : ITokenStore
{
    private const string Clave = "newrich.jwt";

    public Task GuardarAsync(string token) => SecureStorage.Default.SetAsync(Clave, token);

    public Task<string?> ObtenerAsync() => SecureStorage.Default.GetAsync(Clave);

    public Task BorrarAsync()
    {
        SecureStorage.Default.Remove(Clave);
        return Task.CompletedTask;
    }
}

public sealed class SincronizacionOfflineServicio
{
    private readonly NewRichApiClient _api;
    private readonly LocalDatabase _offline;
    private readonly SemaphoreSlim _candado = new(1, 1);

    public SincronizacionOfflineServicio(NewRichApiClient api, LocalDatabase offline)
    {
        _api = api;
        _offline = offline;
    }

    public async Task SincronizarEnSilencioAsync(
        bool conectado,
        RolUsuario rol,
        bool debeCambiarPassword,
        CancellationToken cancellationToken)
    {
        if (!DescargaCodigosOffline.SincronizarEnSilencio(conectado, rol, debeCambiarPassword))
        {
            return;
        }

        await _candado.WaitAsync(cancellationToken);
        try
        {
            var resultado = await _api.DescargarOfflineAsync(cancellationToken);
            if (resultado.IsSuccess)
            {
                var guardar = DescargaCodigosOffline.ParaGuardar(resultado.Data);
                if (guardar.Count > 0)
                {
                    await _offline.GuardarDescargaAsync(guardar);
                }
            }

            var pendientes = await _offline.VentasPendientesAsync();
            if (pendientes.Count == 0)
            {
                return;
            }

            var sync = await _api.SincronizarVentasOfflineAsync(pendientes, cancellationToken);
            if (sync.IsSuccess && sync.Data is not null)
            {
                await _offline.MarcarSincronizadasAsync(sync.Data.Sincronizados);
            }
        }
        catch (HttpRequestException)
        {
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception)
        {
        }
        finally
        {
            _candado.Release();
        }
    }
}

public interface IPrinterService
{
    Task<ResultadoImpresion> ImprimirAsync(string tirilla, string? contenidoQr = null);
}

public sealed class PrinterService : IPrinterService
{
    public Task<ResultadoImpresion> ImprimirAsync(string tirilla, string? contenidoQr = null)
    {
        try
        {
            return ImpresoraInternaSenraise.ImprimirAsync(tirilla, contenidoQr);
        }
        catch (Exception)
        {
            return Task.FromResult(ResultadoImpresion.Fallo(PdaTexts.ErrorImpresion));
        }
    }
}

public interface IPdfService
{
    Task CompartirAsync(string nombreArchivo, byte[] pdf);
}

public sealed class PdfService : IPdfService
{
    public async Task CompartirAsync(string nombreArchivo, byte[] pdf)
    {
        var archivo = Path.GetFileNameWithoutExtension(nombreArchivo) + ".pdf";
        var ruta = Path.Combine(FileSystem.CacheDirectory, archivo);
        await File.WriteAllBytesAsync(ruta, pdf);
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = archivo,
            File = new ShareFile(ruta)
        });
    }
}

public sealed class NavegadorApp
{
    private readonly IServiceProvider _services;

    public NavegadorApp(IServiceProvider services)
    {
        _services = services;
    }

    public void IrALogin() => Asignar(_services.GetRequiredService<Views.Shared.LoginPage>());

    public void IrAPassword() => Asignar(_services.GetRequiredService<Views.Shared.PasswordPage>());

    public void IrAVendedor() => Asignar(_services.GetRequiredService<Views.Vendedor.VendedorShell>());

    public void IrAObservador() => Asignar(_services.GetRequiredService<Views.Observador.ObservadorShell>());

    private static void Asignar(Page pagina)
    {
        var ventana = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault();
        if (ventana is not null)
        {
            ventana.Page = pagina;
        }
    }
}
