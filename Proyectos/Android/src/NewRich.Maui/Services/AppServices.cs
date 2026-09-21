using NewRich.Application.Contracts.Chat;
using NewRich.Application.Contracts.Offline;
using NewRich.Domain.Enums;
using NewRich.Maui.Data;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;

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

public sealed record AvanceSincronizacion(
    string Estado,
    int Completados,
    int Total,
    int Porcentaje,
    int VentasPendientes,
    int CodigosPendientes,
    string? Mensaje);

public sealed record ResultadoSincronizacion(
    bool Exito,
    int VentasSincronizadas,
    int CodigosRepuestos,
    int VentasPendientes,
    int CodigosPendientes,
    string Mensaje);

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

    public Task SincronizarEnSilencioAsync(
        bool conectado,
        RolUsuario rol,
        bool debeCambiarPassword,
        CancellationToken cancellationToken) =>
        SincronizarTodoAsync(conectado, rol, debeCambiarPassword, null, cancellationToken);

    public async Task<ResultadoSincronizacion> SincronizarTodoAsync(
        bool conectado,
        RolUsuario rol,
        bool debeCambiarPassword,
        IProgress<AvanceSincronizacion>? progreso,
        CancellationToken cancellationToken)
    {
        var ventasPendientes = await _offline.ContarVentasPendientesCantidadAsync();
        var gastados = await _offline.ObtenerGastadosPendientesAsync();
        if (!conectado)
        {
            Reportar(progreso, PdaTexts.SyncSinConexion, 0, 1, 0, ventasPendientes, gastados, PdaTexts.SyncSinConexion);
            return new ResultadoSincronizacion(false, 0, 0, ventasPendientes, gastados, PdaTexts.SyncSinConexion);
        }

        await _candado.WaitAsync(cancellationToken);
        try
        {
            if (ReporteTecnicoRegla.DebeEnviarPendientes(conectado, rol, debeCambiarPassword))
            {
                await EnviarReportesPendientesAsync(cancellationToken);
            }

            if (!DescargaCodigosOffline.SincronizarEnSilencio(conectado, rol, debeCambiarPassword))
            {
                return new ResultadoSincronizacion(false, 0, 0, ventasPendientes, gastados, PdaTexts.SyncNoAplica);
            }

            var pendientes = (await _offline.VentasPendientesAsync()).ToList();
            gastados = await _offline.ObtenerGastadosPendientesAsync();
            var total = pendientes.Count + 1;
            var completados = 0;
            var ventasOk = 0;
            Reportar(progreso, PdaTexts.SyncSincronizando, completados, total, 0, pendientes.Count, gastados, null);

            if (pendientes.Count > 0)
            {
                Reportar(progreso, PdaTexts.SyncValidando, completados, total, Porcentaje(completados, total), pendientes.Count, gastados, null);
                var sync = await _api.SincronizarVentasOfflineAsync(pendientes, cancellationToken);
                if (sync.IsSuccess && sync.Data is not null)
                {
                    await _offline.MarcarSincronizadasAsync(sync.Data.Sincronizados);
                    ventasOk = sync.Data.Sincronizados.Count;
                    completados += pendientes.Count;
                }
                else
                {
                    var msg = sync.Message ?? PdaTexts.SyncError;
                    Reportar(progreso, PdaTexts.SyncError, completados, total, Porcentaje(completados, total), pendientes.Count, gastados, msg);
                    return new ResultadoSincronizacion(false, ventasOk, 0, await _offline.ContarVentasPendientesCantidadAsync(), gastados, msg);
                }
            }

            var codigosRepuestos = 0;
            gastados = await _offline.ObtenerGastadosPendientesAsync();
            Reportar(progreso, PdaTexts.SyncReponiendo, completados, total, Porcentaje(completados, total), 0, gastados, null);
            var maximos = await _offline.ObtenerMaximosOfflineAsync();
            var repo = await _api.ReponerCodigosDiarioAsync(
                new ReponerCodigosOfflineRequest
                {
                    CodigosOfflineMaximos = maximos,
                    CodigosOfflineGastadosPendientes = gastados
                },
                cancellationToken);

            if (!repo.IsSuccess || repo.Data is null)
            {
                var msg = repo.Message ?? PdaTexts.SyncError;
                Reportar(progreso, PdaTexts.SyncError, completados, total, Porcentaje(completados, total), 0, gastados, msg);
                return new ResultadoSincronizacion(false, ventasOk, 0, 0, gastados, msg);
            }

            Reportar(progreso, PdaTexts.SyncActualizandoConfig, completados, total, Porcentaje(completados, total), 0, gastados, null);
            await _offline.GuardarMaximosOfflineAsync(repo.Data.CodigosOfflineMaximos);

            if (repo.Data.ReposicionExitosa)
            {
                await _offline.GuardarGastadosPendientesAsync(0);
                codigosRepuestos = repo.Data.CantidadRepuesta;
                gastados = 0;
            }
            else if (repo.Data.YaRealizadaHoy)
            {
                Reportar(progreso, PdaTexts.SyncReposicionYaRealizada, completados, total, Porcentaje(completados, total), 0, gastados, repo.Message);
            }

            completados += 1;
            await DescargarCodigosAsync(cancellationToken);

            var ventasRestantes = await _offline.ContarVentasPendientesCantidadAsync();
            Reportar(progreso, PdaTexts.SyncCompletado, total, total, 100, ventasRestantes, gastados, PdaTexts.SyncCompletado);
            return new ResultadoSincronizacion(true, ventasOk, codigosRepuestos, ventasRestantes, gastados, PdaTexts.SyncCompletado);
        }
        catch (HttpRequestException)
        {
            return new ResultadoSincronizacion(false, 0, 0, await _offline.ContarVentasPendientesCantidadAsync(), await _offline.ObtenerGastadosPendientesAsync(), PdaTexts.SyncSinConexion);
        }
        catch (TaskCanceledException)
        {
            return new ResultadoSincronizacion(false, 0, 0, await _offline.ContarVentasPendientesCantidadAsync(), await _offline.ObtenerGastadosPendientesAsync(), PdaTexts.SyncError);
        }
        catch (Exception)
        {
            return new ResultadoSincronizacion(false, 0, 0, await _offline.ContarVentasPendientesCantidadAsync(), await _offline.ObtenerGastadosPendientesAsync(), PdaTexts.SyncError);
        }
        finally
        {
            _candado.Release();
        }
    }

    private async Task DescargarCodigosAsync(CancellationToken cancellationToken)
    {
        var resultado = await _api.DescargarOfflineAsync(cancellationToken);
        if (!resultado.IsSuccess)
        {
            return;
        }

        var guardar = DescargaCodigosOffline.ParaGuardar(resultado.Data);
        if (guardar.Count > 0)
        {
            await _offline.GuardarDescargaAsync(guardar);
        }
    }

    public async Task<bool> EnviarReportesPendientesAsync(CancellationToken cancellationToken)
    {
        var reportes = await _offline.ReportesTecnicosPendientesAsync();
        var ok = true;
        foreach (var reporte in reportes)
        {
            if (!File.Exists(reporte.RutaPdf))
            {
                ok = false;
                continue;
            }

            var bytes = await File.ReadAllBytesAsync(reporte.RutaPdf, cancellationToken);
            var envio = await _api.ReportarTecnicoAsync(new ReporteTecnicoRequest
            {
                Observacion = reporte.Observacion,
                CodigoTicket = reporte.CodigoTicket,
                NombreArchivo = reporte.NombreArchivo,
                ContenidoBase64 = Convert.ToBase64String(bytes)
            }, cancellationToken);
            if (!envio.IsSuccess)
            {
                ok = false;
                continue;
            }

            await _offline.MarcarReporteTecnicoEnviadoAsync(reporte.Id);
            try
            {
                File.Delete(reporte.RutaPdf);
            }
            catch (IOException)
            {
            }
        }

        return ok;
    }

    private static int Porcentaje(int completados, int total) =>
        total <= 0 ? 100 : (int)Math.Round(100.0 * completados / total);

    private static void Reportar(
        IProgress<AvanceSincronizacion>? progreso,
        string estado,
        int completados,
        int total,
        int porcentaje,
        int ventas,
        int codigos,
        string? mensaje) =>
        progreso?.Report(new AvanceSincronizacion(estado, completados, total, porcentaje, ventas, codigos, mensaje));
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
