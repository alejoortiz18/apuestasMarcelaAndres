using NewRich.Application.Contracts.Versiones;
using NewRich.Domain.Services;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;

namespace NewRich.Maui.Services;

public sealed class ActualizacionEnVivoServicio
{
    private readonly ChatEnVivoServicio _vivo;
    private readonly NewRichApiClient _api;
    private readonly SemaphoreSlim _puerta = new(1, 1);
    private int? _omitida;

    public ActualizacionEnVivoServicio(ChatEnVivoServicio vivo, NewRichApiClient api)
    {
        _vivo = vivo;
        _api = api;
        _vivo.ConexionLista += () => _ = RevisarAsync(null);
        _vivo.ActualizacionRecibida += aviso => _ = RevisarAsync(aviso);
    }

    private async Task RevisarAsync(VersionAplicacionResponse? aviso)
    {
        if (!await _puerta.WaitAsync(0))
        {
            return;
        }

        try
        {
            var vigente = aviso;
            if (vigente is null)
            {
                var consulta = await _api.VersionVigenteAsync(CancellationToken.None);
                if (!consulta.IsSuccess || consulta.Data is null)
                {
                    return;
                }

                vigente = consulta.Data;
            }

            var instalada = int.TryParse(AppInfo.Current.BuildString, out var numeroInstalado) ? numeroInstalado : 0;
            if (!ActualizacionAplicacion.RequiereDescarga(instalada, vigente.NumeroCompilacion))
            {
                return;
            }

            if (_omitida == vigente.NumeroCompilacion)
            {
                return;
            }

            var decision = await PreguntarAsync(vigente.NombreVersion, vigente.NumeroCompilacion);
            if (decision is null)
            {
                return;
            }

            if (decision == false)
            {
                _omitida = vigente.NumeroCompilacion;
                return;
            }

            var ruta = Path.Combine(FileSystem.CacheDirectory, "actualizaciones", "newrich-update.apk");
            var descarga = await _api.DescargarVersionVigenteAsync(ruta, CancellationToken.None);
            if (!descarga.IsSuccess)
            {
                await AvisarAsync(string.IsNullOrWhiteSpace(descarga.Message)
                    ? PdaTexts.ActualizacionNoDescargada
                    : descarga.Message);
                return;
            }

#if ANDROID
            if (!InstaladorApk.PuedeInstalar())
            {
                await AvisarAsync(PdaTexts.ActualizacionPermiso);
                await MainThread.InvokeOnMainThreadAsync(InstaladorApk.PedirPermiso);
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() => InstaladorApk.Abrir(ruta));
#endif
        }
        catch (Exception)
        {
        }
        finally
        {
            _puerta.Release();
        }
    }

    private static async Task<bool?> PreguntarAsync(string nombreVersion, int numeroCompilacion)
    {
        return await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var pagina = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page;
            if (pagina is null)
            {
                return (bool?)null;
            }

            return await pagina.DisplayAlert(
                PdaTexts.ActualizacionTitulo,
                PdaTexts.ActualizacionMensaje(nombreVersion, numeroCompilacion),
                PdaTexts.ActualizacionDescargar,
                PdaTexts.ActualizacionAhoraNo);
        });
    }

    private static Task AvisarAsync(string mensaje) =>
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var pagina = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page;
            if (pagina is null)
            {
                return;
            }

            await pagina.DisplayAlert(PdaTexts.ActualizacionTitulo, mensaje, PdaTexts.ActualizacionAhoraNo);
        });
}
