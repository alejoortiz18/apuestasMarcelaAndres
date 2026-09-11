using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NewRich.Application.Abstractions;

namespace NewRich.Infrastructure.Security;

public sealed class AesGcmQrCryptoService : IQrCryptoService
{
    private readonly byte[] _key;
    private readonly Guid _keyId;

    public AesGcmQrCryptoService(IConfiguration configuration)
    {
        _key = Convert.FromHexString(configuration["Qr:MasterKey"]!);
        _keyId = Guid.Parse(configuration["Qr:KeyId"]!);
    }

    public string Encrypt(QrPayload payload)
    {
        var plaintext = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        var nonce = RandomNumberGenerator.GetBytes(12);
        var cipher = new byte[plaintext.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, plaintext, cipher, tag);
        return string.Join('.', "1", _keyId.ToString("N"), Convert.ToBase64String(nonce), Convert.ToBase64String(cipher), Convert.ToBase64String(tag));
    }

    public QrPayload? Decrypt(string qrContent)
    {
        try
        {
            var normalizado = NormalizarAlfabetoImpreso(qrContent);
            var parts = normalizado.Split('.');
            if (parts.Length != 5)
            {
                return null;
            }

            var nonce = Convert.FromBase64String(parts[2]);
            var cipher = Convert.FromBase64String(parts[3]);
            var tag = Convert.FromBase64String(parts[4]);
            var plaintext = new byte[cipher.Length];
            using var aes = new AesGcm(_key, 16);
            aes.Decrypt(nonce, cipher, tag, plaintext);
            return JsonSerializer.Deserialize<QrPayload>(Encoding.UTF8.GetString(plaintext));
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizarAlfabetoImpreso(string qrContent) =>
        (qrContent ?? string.Empty)
            .Trim()
            .Replace('¡', '+')
            .Replace('¿', '=')
            .Replace('-', '/');

    public string HashClaveValidacion(string claveValidacion)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(claveValidacion)));
    }

    public string GenerarClaveValidacion()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
    }
}
