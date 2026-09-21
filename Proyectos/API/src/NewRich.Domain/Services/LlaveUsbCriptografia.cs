using System.Security.Cryptography;
using System.Text;

namespace NewRich.Domain.Services;

public sealed record MaterialLlaveUsb(
    string Codigo,
    string ClavePublica,
    string SecretoEnvuelto,
    string Huella);

public static class LlaveUsbCriptografia
{
    public static MaterialLlaveUsb Generar(string codigo, string serialUsb, string volumen)
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var publica = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());
        var privada = ecdsa.ExportPkcs8PrivateKey();
        var envuelto = Envolver(privada, serialUsb, volumen);
        return new MaterialLlaveUsb(codigo, publica, envuelto, Huella(serialUsb, volumen));
    }

    public static bool TryDesenvolver(string secretoEnvuelto, string serialUsb, string volumen, out byte[] privada)
    {
        privada = [];
        try
        {
            var raw = Convert.FromBase64String(secretoEnvuelto);
            if (raw.Length < 28)
            {
                return false;
            }

            var nonce = raw.AsSpan(0, 12);
            var tag = raw.AsSpan(12, 16);
            var cipher = raw.AsSpan(28);
            privada = new byte[cipher.Length];
            using var aes = new AesGcm(ClaveEnvoltorio(serialUsb, volumen), 16);
            aes.Decrypt(nonce, cipher, tag, privada);
            return true;
        }
        catch (CryptographicException)
        {
            privada = [];
            return false;
        }
        catch (FormatException)
        {
            privada = [];
            return false;
        }
    }

    public static string Firmar(byte[] privadaPkcs8, string payload)
    {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(privadaPkcs8, out _);
        var firma = ecdsa.SignData(Encoding.UTF8.GetBytes(payload), HashAlgorithmName.SHA256);
        return Convert.ToBase64String(firma);
    }

    public static bool Verificar(string clavePublica, string payload, string firma)
    {
        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(clavePublica), out _);
            return ecdsa.VerifyData(Encoding.UTF8.GetBytes(payload), Convert.FromBase64String(firma), HashAlgorithmName.SHA256);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    public static string PayloadLogin(string codigo, string usuario, string huella, long unix) =>
        $"{codigo}|{usuario}|{huella}|{unix}";

    public static string Huella(string serialUsb, string volumen)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"nr-llave|{serialUsb}|{volumen}"));
        return Convert.ToHexString(bytes);
    }

    public static string NuevoCodigo()
    {
        var n = RandomNumberGenerator.GetBytes(4);
        return $"KEY-{Convert.ToHexString(n)}";
    }

    private static string Envolver(byte[] privada, string serialUsb, string volumen)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var cipher = new byte[privada.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(ClaveEnvoltorio(serialUsb, volumen), 16);
        aes.Encrypt(nonce, privada, cipher, tag);
        var raw = new byte[12 + 16 + cipher.Length];
        Buffer.BlockCopy(nonce, 0, raw, 0, 12);
        Buffer.BlockCopy(tag, 0, raw, 12, 16);
        Buffer.BlockCopy(cipher, 0, raw, 28, cipher.Length);
        return Convert.ToBase64String(raw);
    }

    private static byte[] ClaveEnvoltorio(string serialUsb, string volumen) =>
        SHA256.HashData(Encoding.UTF8.GetBytes($"newrich-llave-usb|{serialUsb}|{volumen}"));
}

public static class DiscoExternoUsb
{
    public static bool EsApto(string busType, bool esDiscoSistema) =>
        !esDiscoSistema && string.Equals(busType, "USB", StringComparison.OrdinalIgnoreCase);

    public static bool EsNtfs(string? fileSystem) =>
        string.Equals(fileSystem, "NTFS", StringComparison.OrdinalIgnoreCase);

    public static string NormalizarLetra(string? letra)
    {
        if (string.IsNullOrWhiteSpace(letra))
        {
            return string.Empty;
        }

        var recorte = letra.Trim();
        var c = char.ToUpperInvariant(recorte[0]);
        return char.IsLetter(c) ? $"{c}:" : string.Empty;
    }
}

public sealed record DiscoUsbInfo(
    string Letra,
    string Etiqueta,
    string Serial,
    string Volumen,
    string SistemaArchivos,
    bool EsNtfs);

public static class SeleccionDiscoUsb
{
    public static ResultadoSeleccionUsb Elegir(
        IReadOnlyList<DiscoUsbInfo> discos,
        string? letra,
        out DiscoUsbInfo? elegido)
    {
        elegido = null;
        if (discos.Count == 0)
        {
            return ResultadoSeleccionUsb.Ninguna;
        }

        if (discos.Count == 1 && string.IsNullOrWhiteSpace(letra))
        {
            elegido = discos[0];
            return ResultadoSeleccionUsb.Ok;
        }

        var normalizada = DiscoExternoUsb.NormalizarLetra(letra);
        if (string.IsNullOrWhiteSpace(normalizada))
        {
            return discos.Count > 1 ? ResultadoSeleccionUsb.Varias : ResultadoSeleccionUsb.Ninguna;
        }

        elegido = discos.FirstOrDefault(d => string.Equals(d.Letra, normalizada, StringComparison.OrdinalIgnoreCase));
        return elegido is null ? ResultadoSeleccionUsb.Ninguna : ResultadoSeleccionUsb.Ok;
    }
}

public enum ResultadoSeleccionUsb
{
    Ok = 1,
    Ninguna = 2,
    Varias = 3
}

public static class PruebaLlaveUsb
{
    public static bool TryFirmarLogin(
        string secretoEnvuelto,
        string serialUsb,
        string volumen,
        string codigo,
        string usuario,
        long unix,
        out string firma,
        out string huella)
    {
        firma = string.Empty;
        huella = LlaveUsbCriptografia.Huella(serialUsb, volumen);
        if (!LlaveUsbCriptografia.TryDesenvolver(secretoEnvuelto, serialUsb, volumen, out var privada))
        {
            return false;
        }

        var payload = LlaveUsbCriptografia.PayloadLogin(codigo, usuario, huella, unix);
        firma = LlaveUsbCriptografia.Firmar(privada, payload);
        CryptographicOperations.ZeroMemory(privada);
        return true;
    }
}
