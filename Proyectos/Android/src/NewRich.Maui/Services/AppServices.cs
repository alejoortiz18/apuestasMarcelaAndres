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

public interface IPrinterService
{
    Task<ResultadoImpresion> ImprimirAsync(string tirilla, string? contenidoQr = null);
}

public sealed class PrinterService : IPrinterService
{
    public Task<ResultadoImpresion> ImprimirAsync(string tirilla, string? contenidoQr = null) =>
        ImpresoraInternaSenraise.ImprimirAsync(tirilla, contenidoQr);
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
