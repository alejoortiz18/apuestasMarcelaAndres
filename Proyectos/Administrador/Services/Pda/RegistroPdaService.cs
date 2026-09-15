using Microsoft.Extensions.Options;
using NewRich.Admin.Constants;
using NewRich.Application.Contracts.Dispositivos;
using NewRich.Constants;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Services.Pda;

/// <summary>Un paso del registro con el porcentaje que ve el administrador.</summary>
public sealed record AvanceRegistroPda(int Porcentaje, string Mensaje);

/// <summary>Estado del equipo antes de iniciar el registro.</summary>
public sealed record VerificacionPda(bool Listo, string Mensaje, string? Modelo);

/// <summary>
/// Cierre del registro. No expone el codigo del dispositivo: el administrador nunca debe conocerlo.
/// </summary>
public sealed record ResultadoRegistroPda(bool Exitoso, string Mensaje, string? Modelo);

/// <summary>Recibe cada paso del registro para mostrarlo en tiempo real.</summary>
public interface IAvanceRegistroPda
{
    Task ReportarAsync(AvanceRegistroPda avance, CancellationToken cancellationToken);
}

public interface IRegistroPdaService
{
    Task<VerificacionPda> VerificarAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Registra el equipo para el tipo de usuario que lo va a operar. El tipo debe coincidir con el
    /// perfil del usuario que despues se asocie, por eso lo elige el administrador al iniciar.
    /// </summary>
    Task<ResultadoRegistroPda> RegistrarAsync(TipoDispositivo tipo, IAvanceRegistroPda avance, CancellationToken cancellationToken);
}

/// <summary>
/// Registro guiado del PDA. Detecta el equipo conectado por USB, pide a la API el codigo unico,
/// lo graba en el aparato, instala la aplicacion y confirma el resultado. El codigo solo viaja
/// entre la API y el dispositivo; nunca se muestra ni se pide en pantalla.
/// </summary>
public sealed class RegistroPdaService : IRegistroPdaService
{
    private readonly IAdb _adb;
    private readonly IApkPda _apk;
    private readonly IAdminApiClient _api;
    private readonly OpcionesRegistroPda _opciones;

    public RegistroPdaService(IAdb adb, IApkPda apk, IAdminApiClient api, IOptions<OpcionesRegistroPda> opciones)
    {
        _adb = adb;
        _apk = apk;
        _api = api;
        _opciones = opciones.Value;
    }

    public async Task<VerificacionPda> VerificarAsync(CancellationToken cancellationToken)
    {
        var (dispositivo, problema) = await DetectarAsync(cancellationToken);
        if (dispositivo is null)
        {
            return new VerificacionPda(false, problema!, null);
        }

        if (_apk.RutaDisponible() is null)
        {
            return new VerificacionPda(false, UiTexts.PdaSinAplicacionDisponible, dispositivo.Modelo);
        }

        return new VerificacionPda(true, UiTexts.PdaListoParaRegistrar, dispositivo.Modelo);
    }

    public async Task<ResultadoRegistroPda> RegistrarAsync(TipoDispositivo tipo, IAvanceRegistroPda avance, CancellationToken cancellationToken)
    {
        await avance.ReportarAsync(new AvanceRegistroPda(0, UiTexts.PdaProgresoDetectando), cancellationToken);
        var (dispositivo, problema) = await DetectarAsync(cancellationToken);
        if (dispositivo is null)
        {
            return new ResultadoRegistroPda(false, problema!, null);
        }

        await avance.ReportarAsync(new AvanceRegistroPda(20, UiTexts.PdaProgresoConectado), cancellationToken);

        await avance.ReportarAsync(new AvanceRegistroPda(30, UiTexts.PdaProgresoValidando), cancellationToken);
        var modelo = await EjecutarAsync(dispositivo, cancellationToken, "shell", "getprop", "ro.product.model");
        if (!modelo.Exitoso)
        {
            return new ResultadoRegistroPda(false, UiTexts.PdaFalloValidacion, dispositivo.Modelo);
        }

        // La instalacion se revisa antes de pedir el codigo para no dejar registrado un equipo
        // en el que despues no se puede instalar la aplicacion.
        var rutaApk = _apk.RutaDisponible();
        if (rutaApk is null)
        {
            return new ResultadoRegistroPda(false, UiTexts.PdaSinAplicacionDisponible, dispositivo.Modelo);
        }

        var modeloEquipo = Preferir(modelo.Salida, dispositivo.Modelo);

        await avance.ReportarAsync(new AvanceRegistroPda(45, UiTexts.PdaProgresoGenerando), cancellationToken);
        var registro = await _api.RegistrarDispositivoAutomaticoAsync(new RegistrarPdaAutomaticoRequest
        {
            NumeroSerie = dispositivo.NumeroSerie,
            Modelo = modeloEquipo,
            Tipo = tipo
        }, cancellationToken);

        if (!registro.Success || registro.Data is null)
        {
            return new ResultadoRegistroPda(false, registro.Message, modeloEquipo);
        }

        await avance.ReportarAsync(new AvanceRegistroPda(60, UiTexts.PdaProgresoRegistrando), cancellationToken);
        var identidad = await EjecutarAsync(dispositivo, cancellationToken,
            "shell", "settings", "put", "global", ProvisionPda.ClaveCodigoDispositivo, registro.Data.CodigoDispositivo);
        if (!identidad.Exitoso)
        {
            return new ResultadoRegistroPda(false, UiTexts.PdaFalloGrabarIdentidad, modeloEquipo);
        }

        await avance.ReportarAsync(new AvanceRegistroPda(75, UiTexts.PdaProgresoInstalando), cancellationToken);
        var instalacion = await EjecutarAsync(dispositivo, cancellationToken, "install", "-r", rutaApk);
        if (!instalacion.Exitoso)
        {
            return new ResultadoRegistroPda(false, UiTexts.PdaFalloInstalacion, modeloEquipo);
        }

        await avance.ReportarAsync(new AvanceRegistroPda(90, UiTexts.PdaProgresoVerificando), cancellationToken);
        var instalada = await EjecutarAsync(dispositivo, cancellationToken, "shell", "pm", "path", _opciones.Paquete);
        if (!instalada.Exitoso || !instalada.Salida.Contains("package:", StringComparison.OrdinalIgnoreCase))
        {
            return new ResultadoRegistroPda(false, UiTexts.PdaInstalacionNoVerificada, modeloEquipo);
        }

        // El puente USB permite que la aplicacion alcance la API mientras el equipo sigue conectado.
        // Si no queda disponible la aplicacion usa la red local, asi que no detiene el registro.
        await avance.ReportarAsync(new AvanceRegistroPda(97, UiTexts.PdaProgresoFinalizando), cancellationToken);
        var puente = $"tcp:{_opciones.PuertoPuenteUsb}";
        await EjecutarAsync(dispositivo, cancellationToken, "reverse", puente, puente);

        await avance.ReportarAsync(new AvanceRegistroPda(100, UiTexts.PdaRegistroCompletado), cancellationToken);
        return new ResultadoRegistroPda(true, UiTexts.PdaRegistroCompletado, modeloEquipo);
    }

    private async Task<(DispositivoAdb? Dispositivo, string? Problema)> DetectarAsync(CancellationToken cancellationToken)
    {
        var listado = await _adb.EjecutarAsync(["devices", "-l"], cancellationToken);
        if (!listado.Exitoso)
        {
            return (null, UiTexts.PdaAdbNoDisponible);
        }

        var dispositivos = SalidaAdb.LeerDispositivos(listado.Salida);
        if (dispositivos.Count == 0)
        {
            return (null, UiTexts.PdaSinDispositivoConectado);
        }

        if (dispositivos.Count > 1)
        {
            return (null, UiTexts.PdaVariosDispositivos);
        }

        var unico = dispositivos[0];
        return unico.Estado switch
        {
            EstadoConexionAdb.Listo => (unico, null),
            EstadoConexionAdb.SinAutorizar => (null, UiTexts.PdaSinAutorizacionUsb),
            _ => (null, UiTexts.PdaDispositivoNoDisponible)
        };
    }

    private Task<AdbResultado> EjecutarAsync(DispositivoAdb dispositivo, CancellationToken cancellationToken, params string[] argumentos) =>
        _adb.EjecutarAsync(["-s", dispositivo.NumeroSerie, .. argumentos], cancellationToken);

    private static string? Preferir(string salida, string? alterno)
    {
        var limpio = salida.Trim();
        return limpio.Length > 0 ? limpio : alterno;
    }
}
