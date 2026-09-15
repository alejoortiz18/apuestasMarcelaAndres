using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using NewRich.Admin.Constants;

namespace NewRich.Admin.Services.Pda;

/// <summary>Ejecuta adb como proceso en el computador donde el administrador conecta el PDA.</summary>
public sealed class AdbProceso : IAdb
{
    private readonly OpcionesRegistroPda _opciones;

    public AdbProceso(IOptions<OpcionesRegistroPda> opciones)
    {
        _opciones = opciones.Value;
    }

    public async Task<AdbResultado> EjecutarAsync(IReadOnlyList<string> argumentos, CancellationToken cancellationToken)
    {
        var inicio = new ProcessStartInfo(_opciones.RutaAdb)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argumento in argumentos)
        {
            inicio.ArgumentList.Add(argumento);
        }

        using var espera = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        espera.CancelAfter(TimeSpan.FromSeconds(_opciones.TiempoLimiteSegundos));

        try
        {
            using var proceso = Process.Start(inicio);
            if (proceso is null)
            {
                return new AdbResultado(-1, string.Empty, UiTexts.PdaAdbNoDisponible);
            }

            var salida = proceso.StandardOutput.ReadToEndAsync(espera.Token);
            var error = proceso.StandardError.ReadToEndAsync(espera.Token);
            await proceso.WaitForExitAsync(espera.Token);
            return new AdbResultado(proceso.ExitCode, await salida, await error);
        }
        catch (Win32Exception)
        {
            return new AdbResultado(-1, string.Empty, UiTexts.PdaAdbNoDisponible);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new AdbResultado(-1, string.Empty, UiTexts.PdaAdbSinRespuesta);
        }
    }
}

/// <summary>Busca el instalador de la aplicacion en el disco del computador del administrador.</summary>
public sealed class ApkPdaEnDisco : IApkPda
{
    private readonly OpcionesRegistroPda _opciones;
    private readonly IHostEnvironment _entorno;

    public ApkPdaEnDisco(IOptions<OpcionesRegistroPda> opciones, IHostEnvironment entorno)
    {
        _opciones = opciones.Value;
        _entorno = entorno;
    }

    public string? RutaDisponible()
    {
        if (string.IsNullOrWhiteSpace(_opciones.RutaApk))
        {
            return null;
        }

        var ruta = Path.IsPathRooted(_opciones.RutaApk)
            ? _opciones.RutaApk
            : Path.GetFullPath(Path.Combine(_entorno.ContentRootPath, _opciones.RutaApk));

        return File.Exists(ruta) ? ruta : null;
    }
}
