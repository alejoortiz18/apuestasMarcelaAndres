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
    public IReadOnlyList<DiscoUsbInfo> Listar()
    {
        var sistema = DiscoExternoUsb.NormalizarLetra(Path.GetPathRoot(Environment.SystemDirectory));
        var resultado = new List<DiscoUsbInfo>();
        foreach (var disco in DriveInfo.GetDrives())
        {
            if (!disco.IsReady)
            {
                continue;
            }

            var letra = DiscoExternoUsb.NormalizarLetra(disco.Name);
            if (string.Equals(letra, sistema, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var bus = ConsultarBus(letra);
            var esUsb = string.Equals(bus, "USB", StringComparison.OrdinalIgnoreCase)
                || (disco.DriveType == DriveType.Removable && bus == "DESCONOCIDO");
            if (!DiscoExternoUsb.EsApto(esUsb ? "USB" : bus, esDiscoSistema: false))
            {
                continue;
            }

            var serial = ConsultarSerial(letra);
            var volumen = disco.VolumeSerialNumber();
            var fs = disco.DriveFormat;
            resultado.Add(new DiscoUsbInfo(
                letra,
                string.IsNullOrWhiteSpace(disco.VolumeLabel) ? letra : disco.VolumeLabel,
                string.IsNullOrWhiteSpace(serial) ? volumen : serial,
                volumen,
                fs,
                DiscoExternoUsb.EsNtfs(fs)));
        }

        return resultado;
    }

    private static string ConsultarBus(string letra)
    {
        try
        {
            var deviceId = ConsultarDeviceId(letra);
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return "DESCONOCIDO";
            }

            using var busqueda = new ManagementObjectSearcher(
                "SELECT InterfaceType FROM Win32_DiskDrive WHERE DeviceID = '" + Escapar(deviceId) + "'");
            foreach (var item in busqueda.Get())
            {
                return item["InterfaceType"]?.ToString() ?? "DESCONOCIDO";
            }
        }
        catch (ManagementException)
        {
            return "DESCONOCIDO";
        }

        return "DESCONOCIDO";
    }

    private static string ConsultarSerial(string letra)
    {
        try
        {
            var deviceId = ConsultarDeviceId(letra);
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return string.Empty;
            }

            using var busqueda = new ManagementObjectSearcher(
                "SELECT SerialNumber FROM Win32_DiskDrive WHERE DeviceID = '" + Escapar(deviceId) + "'");
            foreach (var item in busqueda.Get())
            {
                return (item["SerialNumber"]?.ToString() ?? string.Empty).Trim();
            }
        }
        catch (ManagementException)
        {
            return string.Empty;
        }

        return string.Empty;
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

        try
        {
            var proceso = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "format.com"),
                Arguments = $"{letra} /FS:NTFS /Q /Y /V:NEWRICH",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            if (proceso is null)
            {
                return LlaveMessages.FormatoFallido;
            }

            proceso.WaitForExit(120_000);
            return proceso.ExitCode == 0 ? null : LlaveMessages.FormatoFallido;
        }
        catch (Exception)
        {
            return LlaveMessages.FormatoFallido;
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
