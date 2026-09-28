using NewRich.Application.Contracts.Versiones;
using NewRich.Domain.Services;
using NewRich.Maui.Data;
using NewRich.Maui.Views.Shared;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;

namespace NewRich.Maui.Services;

public sealed class ActualizacionEnVivoServicio
{
    private readonly ChatEnVivoServicio _vivo;
    private readonly NewRichApiClient _api;
    private readonly NavegadorApp _nav;
    private readonly ITokenStore _tokens;
    private readonly SesionPda _sesion;
    private readonly LocalDatabase _local;
    private readonly SemaphoreSlim _puerta = new(1, 1);
    private readonly object _candado = new();
    private Task<bool>? _revision;
    private DescargaActualizacionPage? _pantalla;
    private int? _omitida;
    private bool _actualizando;
    private bool _instaladorAbierto;

    public ActualizacionEnVivoServicio(
        ChatEnVivoServicio vivo,
        NewRichApiClient api,
        NavegadorApp nav,
        ITokenStore tokens,
        SesionPda sesion,
        LocalDatabase local)
    {
        _vivo = vivo;
        _api = api;
        _nav = nav;
        _tokens = tokens;
        _sesion = sesion;
        _local = local;
        _vivo.ConexionLista += () => _ = RevisarAhoraAsync();
        _vivo.ActualizacionRecibida += aviso => _ = RevisarAhoraAsync(aviso);
    }

    public Task<bool> RevisarAhoraAsync(VersionAplicacionResponse? aviso = null)
    {
        lock (_candado)
        {
            if (!ActualizacionAplicacion.PermiteIngresar(_actualizando))
            {
                return Task.FromResult(false);
            }

            if (_revision is { IsCompleted: false })
            {
                return _revision;
            }

            _revision = EjecutarAsync(aviso);
            return _revision;
        }
    }

    private async Task<bool> EjecutarAsync(VersionAplicacionResponse? aviso)
    {
        if (!await _puerta.WaitAsync(0))
        {
            return false;
        }

        var reemplazo = false;
        try
        {
            var vigente = aviso;
            if (vigente is null)
            {
                var consulta = await _api.VersionVigenteAsync(CancellationToken.None);
                if (!consulta.IsSuccess || consulta.Data is null)
                {
                    return true;
                }

                vigente = consulta.Data;
            }

            var instalada = int.TryParse(AppInfo.Current.BuildString, out var numeroInstalado) ? numeroInstalado : 0;
            if (!ActualizacionAplicacion.RequiereDescarga(instalada, vigente.NumeroCompilacion))
            {
                return true;
            }

            if (_omitida == vigente.NumeroCompilacion)
            {
                return true;
            }

            var decision = await PreguntarAsync(vigente.NombreVersion, vigente.NumeroCompilacion);
            if (decision is null)
            {
                return true;
            }

            if (decision == false)
            {
                _omitida = vigente.NumeroCompilacion;
                return true;
            }

            _actualizando = true;
            var ruta = Path.Combine(FileSystem.CacheDirectory, "actualizaciones", "newrich-update.apk");
            var pantalla = await MostrarPantallaAsync(ruta);
            reemplazo = pantalla is not null;
            pantalla?.Aplicar(DescargaActualizacion.Preparando());
            var progreso = new Progress<ProgresoBytes>(avisoBytes =>
                MainThread.BeginInvokeOnMainThread(() =>
                    pantalla?.Aplicar(DescargaActualizacion.Descargando(avisoBytes.Leidos, avisoBytes.Total))));
            var descarga = await _api.DescargarVersionVigenteAsync(ruta, progreso, CancellationToken.None);
            if (!descarga.IsSuccess)
            {
                await AvisarAsync(string.IsNullOrWhiteSpace(descarga.Message)
                    ? PdaTexts.ActualizacionNoDescargada
                    : descarga.Message);
                await VolverAlIngresoAsync();
                return false;
            }

            pantalla?.Aplicar(DescargaActualizacion.Instalando());
            await CerrarSesionLocalAsync();
            await AbrirInstaladorAsync(pantalla, ruta);
            return false;
        }
        catch (Exception)
        {
            if (reemplazo || _actualizando)
            {
                await VolverAlIngresoAsync();
                return false;
            }

            return true;
        }
        finally
        {
            _puerta.Release();
        }
    }

    private Task<DescargaActualizacionPage?> MostrarPantallaAsync(string ruta) =>
        MainThread.InvokeOnMainThreadAsync(() =>
        {
            var pagina = new DescargaActualizacionPage(() => AlRegresar(ruta));
            _pantalla = pagina;
            _nav.Mostrar(pagina);
            return pagina;
        });

    private async Task AbrirInstaladorAsync(DescargaActualizacionPage? pantalla, string ruta)
    {
#if ANDROID
        if (!InstaladorApk.PuedeInstalar())
        {
            await AvisarAsync(PdaTexts.ActualizacionPermiso);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                pantalla?.VigilarRegreso();
                InstaladorApk.PedirPermiso();
            });
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            _instaladorAbierto = true;
            pantalla?.VigilarRegreso();
            InstaladorApk.Abrir(ruta);
        });
#else
        await VolverAlIngresoAsync();
#endif
    }

    private void AlRegresar(string ruta)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
#if ANDROID
            if (!_instaladorAbierto && InstaladorApk.PuedeInstalar())
            {
                _instaladorAbierto = true;
                _pantalla?.VigilarRegreso();
                InstaladorApk.Abrir(ruta);
                return;
            }
#endif
            _ = VolverAlIngresoAsync();
        });
    }

    private async Task CerrarSesionLocalAsync()
    {
        await _tokens.BorrarAsync();
        _sesion.Usuario = null;
        _sesion.Borrador = null;
        await _local.BorrarSesionAsync();
    }

    private Task VolverAlIngresoAsync()
    {
        _actualizando = false;
        _instaladorAbierto = false;
        return MainThread.InvokeOnMainThreadAsync(_nav.IrALogin);
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
