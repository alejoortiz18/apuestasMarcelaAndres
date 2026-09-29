using System.Management;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using NewRich.Application.Contracts.Auth;
using NewRich.Constants.Messages;
using NewRich.Domain.Services;

namespace NewRich.Admin.Services.Usb;

public sealed record ContenidoLlaveUsb(int Version, string Codigo, string SecretoEnvuelto);

public interface IInventarioUsb
{
    IReadOnlyList<DiscoUsbInfo> Listar();
}

public interface IPreparadorLlaveUsb
{
    string? FormatearNtfs(DiscoUsbInfo disco);
    string? EscribirLlave(DiscoUsbInfo disco, ContenidoLlaveUsb contenido);
}

public interface ILectorLlaveUsb
{
    ContenidoLlaveUsb? Leer(DiscoUsbInfo disco);
    PruebaLlaveAdministradorRequest? CrearPrueba(DiscoUsbInfo disco, string usuario);
}

[SupportedOSPlatform("windows")]
public sealed class InventarioUsbWindows : IInventarioUsb
{
    // Cada recorrido consulta WMI por disco y cuesta cientos de milisegundos, y el ingreso
    // lo pide dos veces seguidas (al dibujar el formulario y al enviarlo). Se reutiliza el
    // último recorrido mientras las unidades se vean iguales: conectar, retirar o formatear
    // una memoria cambia la firma y obliga a leer de nuevo.
    private static readonly TimeSpan VigenciaMaxima = TimeSpan.FromMinutes(1);
    private static readonly object Candado = new();
    private static IReadOnlyList<DiscoUsbInfo>? _ultimoResultado;
    private static string _ultimaFirma = string.Empty;
    private static DateTime _ultimaLectura = DateTime.MinValue;

    public IReadOnlyList<DiscoUsbInfo> Listar()
    {
        var firma = FirmaUnidades();
        lock (Candado)
        {
            if (_ultimoResultado is not null
                && firma == _ultimaFirma
                && DateTime.UtcNow - _ultimaLectura < VigenciaMaxima)
            {
                return _ultimoResultado;
            }

            _ultimoResultado = Recorrer();
            _ultimaFirma = firma;
            _ultimaLectura = DateTime.UtcNow;
            return _ultimoResultado;
        }
    }

    /// <summary>
    /// Datos que Windows entrega sin consultar WMI, suficientes para notar que las unidades cambiaron.
    /// </summary>
    private static string FirmaUnidades()
    {
        // Una unidad que no responde deja esta lectura esperando; si eso pasa se recorre de
        // nuevo, que es el camino que ya limita el tiempo por disco.
        var consulta = Task.Run(ArmarFirma);
        return consulta.Wait(TimeSpan.FromSeconds(2))
            ? consulta.Result
            : Guid.NewGuid().ToString();
    }

    private static string ArmarFirma()
    {
        var partes = new List<string>();
        foreach (var disco in DriveInfo.GetDrives())
        {
            if (disco.DriveType is not (DriveType.Removable or DriveType.Fixed))
            {
                continue;
            }

            try
            {
                partes.Add(disco.IsReady
                    ? $"{disco.Name}|{disco.DriveType}|{disco.DriveFormat}|{disco.VolumeLabel}|{disco.TotalSize}"
                    : $"{disco.Name}|vacia");
            }
            catch (IOException)
            {
                partes.Add($"{disco.Name}|error");
            }
            catch (UnauthorizedAccessException)
            {
                partes.Add($"{disco.Name}|error");
            }
        }

        return string.Join(";", partes);
    }

    private static IReadOnlyList<DiscoUsbInfo> Recorrer()
    {
        var sistema = DiscoExternoUsb.NormalizarLetra(Path.GetPathRoot(Environment.SystemDirectory));
        var resultado = new List<DiscoUsbInfo>();
        foreach (var disco in DriveInfo.GetDrives())
        {
            if (disco.DriveType is not (DriveType.Removable or DriveType.Fixed))
            {
                continue;
            }

            var letra = DiscoExternoUsb.NormalizarLetra(disco.Name);
            if (string.Equals(letra, sistema, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var info = DescribirConLimite(disco, letra);
            if (info is not null)
            {
                resultado.Add(info);
            }
        }

        return resultado;
    }

    private static DiscoUsbInfo? DescribirConLimite(DriveInfo disco, string letra)
    {
        // Una lectora vacía o un disco de red deja IsReady y WMI esperando, y la pantalla de ingreso no llega a dibujarse.
        var consulta = Task.Run(() => Describir(disco, letra));
        if (!consulta.Wait(TimeSpan.FromSeconds(2)))
        {
            return null;
        }

        return consulta.Result;
    }

    private static DiscoUsbInfo? Describir(DriveInfo disco, string letra)
    {
        try
        {
            if (!disco.IsReady)
            {
                return null;
            }

            var (bus, serial) = ConsultarDisco(letra);
            var esUsb = string.Equals(bus, "USB", StringComparison.OrdinalIgnoreCase)
                || (disco.DriveType == DriveType.Removable && bus == "DESCONOCIDO");
            if (!DiscoExternoUsb.EsApto(esUsb ? "USB" : bus, esDiscoSistema: false))
            {
                return null;
            }

            var volumen = disco.VolumeSerialNumber();
            var fs = disco.DriveFormat;
            return new DiscoUsbInfo(
                letra,
                string.IsNullOrWhiteSpace(disco.VolumeLabel) ? letra : disco.VolumeLabel,
                string.IsNullOrWhiteSpace(serial) ? volumen : serial,
                volumen,
                fs,
                DiscoExternoUsb.EsNtfs(fs));
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static (string Bus, string Serial) ConsultarDisco(string letra)
    {
        try
        {
            var deviceId = ConsultarDeviceId(letra);
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return ("DESCONOCIDO", string.Empty);
            }

            using var busqueda = new ManagementObjectSearcher(
                "SELECT InterfaceType, SerialNumber FROM Win32_DiskDrive WHERE DeviceID = '" + Escapar(deviceId) + "'");
            foreach (var item in busqueda.Get())
            {
                return (
                    item["InterfaceType"]?.ToString() ?? "DESCONOCIDO",
                    (item["SerialNumber"]?.ToString() ?? string.Empty).Trim());
            }
        }
        catch (ManagementException)
        {
            return ("DESCONOCIDO", string.Empty);
        }

        return ("DESCONOCIDO", string.Empty);
    }

    private static string ConsultarDeviceId(string letra)
    {
        var unidad = letra.TrimEnd('\\', ':') + ":";
        using var particion = new ManagementObjectSearcher(
            "ASSOCIATORS OF {Win32_LogicalDisk.DeviceID='" + unidad + "'} WHERE AssocClass=Win32_LogicalDiskToPartition");
        foreach (var part in particion.Get())
        {
            var partId = part["DeviceID"]?.ToString();
            if (string.IsNullOrWhiteSpace(partId))
            {
                continue;
            }

            using var disco = new ManagementObjectSearcher(
                "ASSOCIATORS OF {Win32_DiskPartition.DeviceID='" + Escapar(partId) + "'} WHERE AssocClass=Win32_DiskDriveToDiskPartition");
            foreach (var d in disco.Get())
            {
                return d["DeviceID"]?.ToString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static string Escapar(string valor) => valor.Replace("\\", "\\\\").Replace("'", "\\'");
}

[SupportedOSPlatform("windows")]
internal static class DriveInfoExtensiones
{
    public static string VolumeSerialNumber(this DriveInfo disco)
    {
        try
        {
            using var busqueda = new ManagementObjectSearcher(
                "SELECT VolumeSerialNumber FROM Win32_LogicalDisk WHERE DeviceID = '" + DiscoExternoUsb.NormalizarLetra(disco.Name) + "'");
            foreach (var item in busqueda.Get())
            {
                return (item["VolumeSerialNumber"]?.ToString() ?? string.Empty).Trim();
            }
        }
        catch (ManagementException)
        {
        }

        return disco.Name.TrimEnd('\\');
    }
}

[SupportedOSPlatform("windows")]
public sealed class PreparadorLlaveUsbWindows : IPreparadorLlaveUsb
{
    public string? FormatearNtfs(DiscoUsbInfo disco)
    {
        var letra = DiscoExternoUsb.NormalizarLetra(disco.Letra);
        if (string.IsNullOrWhiteSpace(letra))
        {
            return LlaveMessages.UsbNoDetectada;
        }

        if (!PreparacionVolumenUsb.DebeFormatear(disco.SistemaArchivos))
        {
            return PreparacionVolumenUsb.Vaciar(letra + "\\");
        }

        return EjecutarFormato(letra);
    }

    private static string? EjecutarFormato(string letra)
    {
        var registro = Path.Combine(Path.GetTempPath(), "newrich-formato-" + Guid.NewGuid().ToString("N") + ".log");
        try
        {
            var proceso = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe"),
                Arguments = "/c format " + letra + " /FS:NTFS /Q /X /Y /V:NEWRICH > \"" + registro + "\" 2>&1",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (proceso is null)
            {
                return LlaveMessages.FormatoFallido;
            }

            if (!proceso.WaitForExit(120_000))
            {
                try
                {
                    proceso.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                }

                return LlaveMessages.FormatoFallido;
            }

            return proceso.ExitCode == 0 ? null : LlaveMessages.FormatoFallido;
        }
        catch (Exception)
        {
            return LlaveMessages.FormatoFallido;
        }
        finally
        {
            try
            {
                if (File.Exists(registro))
                {
                    File.Delete(registro);
                }
            }
            catch (IOException)
            {
            }
        }
    }

    public string? EscribirLlave(DiscoUsbInfo disco, ContenidoLlaveUsb contenido)
    {
        try
        {
            var raiz = disco.Letra.TrimEnd('\\') + "\\";
            var carpeta = Path.Combine(raiz, "NewRichLlave");
            Directory.CreateDirectory(carpeta);
            var json = JsonSerializer.Serialize(contenido);
            var rutaJson = Path.Combine(carpeta, "llave.json");
            File.WriteAllText(rutaJson, json, Encoding.UTF8);
            File.WriteAllText(Path.Combine(carpeta, "proteger.cmd"), Protector, Encoding.ASCII);
            try
            {
                File.SetAttributes(rutaJson, FileAttributes.Hidden | FileAttributes.System);
                File.SetAttributes(carpeta, FileAttributes.Hidden | FileAttributes.System);
            }
            catch (IOException)
            {
            }

            return null;
        }
        catch (IOException)
        {
            return LlaveMessages.EscrituraFallida;
        }
        catch (UnauthorizedAccessException)
        {
            return LlaveMessages.EscrituraFallida;
        }
    }

    private const string Protector =
        "@echo off\r\n" +
        "attrib +h +s \"%~dp0llave.json\" >nul 2>&1\r\n" +
        "echo Esta memoria es una llave de administrador. No copie estos archivos a otra USB.\r\n";
}

public sealed class LectorLlaveUsb : ILectorLlaveUsb
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public ContenidoLlaveUsb? Leer(DiscoUsbInfo disco)
    {
        var ruta = Path.Combine(disco.Letra.TrimEnd('\\') + "\\", "NewRichLlave", "llave.json");
        if (!File.Exists(ruta))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ContenidoLlaveUsb>(File.ReadAllText(ruta), Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public PruebaLlaveAdministradorRequest? CrearPrueba(DiscoUsbInfo disco, string usuario)
    {
        var contenido = Leer(disco);
        if (contenido is null || string.IsNullOrWhiteSpace(contenido.Codigo) || string.IsNullOrWhiteSpace(contenido.SecretoEnvuelto))
        {
            return null;
        }

        var unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (!PruebaLlaveUsb.TryFirmarLogin(
                contenido.SecretoEnvuelto,
                disco.Serial,
                disco.Volumen,
                contenido.Codigo,
                usuario,
                unix,
                out var firma,
                out var huella))
        {
            return null;
        }

        return new PruebaLlaveAdministradorRequest
        {
            Codigo = contenido.Codigo,
            HuellaDispositivo = huella,
            Firma = firma,
            Unix = unix
        };
    }
}
