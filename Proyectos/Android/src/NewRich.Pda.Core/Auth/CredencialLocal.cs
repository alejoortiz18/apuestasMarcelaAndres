using System.Security.Cryptography;
using System.Text;

namespace NewRich.Pda.Core.Auth;

public sealed class CredencialLocalRegistro
{
    public string Usuario { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
}

/// <summary>Guarda la contraseña hasheada en el PDA para permitir ingreso offline.</summary>
public static class CredencialLocal
{
    public static CredencialLocalRegistro Crear(string usuario, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        return new CredencialLocalRegistro
        {
            Usuario = usuario.Trim(),
            Salt = Convert.ToBase64String(salt),
            Hash = Hashear(password, salt)
        };
    }

    public static bool Coincide(CredencialLocalRegistro? registro, string usuario, string password)
    {
        if (registro is null
            || string.IsNullOrWhiteSpace(registro.Usuario)
            || string.IsNullOrWhiteSpace(registro.Salt)
            || string.IsNullOrWhiteSpace(registro.Hash))
        {
            return false;
        }

        if (!string.Equals(registro.Usuario, usuario.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var salt = Convert.FromBase64String(registro.Salt);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(registro.Hash),
            Convert.FromBase64String(Hashear(password, salt)));
    }

    private static string Hashear(string password, byte[] salt)
    {
        var bytes = Encoding.UTF8.GetBytes(password ?? string.Empty);
        var mezclado = new byte[salt.Length + bytes.Length];
        Buffer.BlockCopy(salt, 0, mezclado, 0, salt.Length);
        Buffer.BlockCopy(bytes, 0, mezclado, salt.Length, bytes.Length);
        return Convert.ToBase64String(SHA256.HashData(mezclado));
    }
}
