using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace NewRich.Application.Services;

/// <summary>
/// Lee el certificado del primer firmante del bloque de firma de Android (esquemas v3 y v2).
/// No verifica la firma criptográfica: eso lo hace Android al instalar. Sirve para saber con qué llave se firmó.
/// </summary>
public static class LectorFirmaApk
{
    private const uint IdBloqueV2 = 0x7109871a;
    private const uint IdBloqueV3 = 0xf05368c0;
    private const uint FirmaFinDirectorio = 0x06054b50;
    private const int TamanoFinDirectorio = 22;
    private static readonly byte[] Magia = Encoding.ASCII.GetBytes("APK Sig Block 42");

    /// <summary>SHA-256 del certificado en hexadecimal sin separadores, o null si el APK no trae firma v2 ni v3.</summary>
    public static string? Huella(Stream apk)
    {
        var certificado = Certificado(apk);
        return certificado is null ? null : Convert.ToHexString(SHA256.HashData(certificado));
    }

    public static byte[]? Certificado(Stream apk)
    {
        if (!apk.CanSeek)
        {
            throw new ArgumentException("El APK debe poder recorrerse.", nameof(apk));
        }

        try
        {
            var bloque = BloqueFirma(apk);
            if (bloque is null)
            {
                return null;
            }

            var pares = Pares(bloque);
            var valor = pares.TryGetValue(IdBloqueV3, out var v3) ? v3
                : pares.TryGetValue(IdBloqueV2, out var v2) ? v2
                : null;
            return valor is null ? null : PrimerCertificado(valor);
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or EndOfStreamException or InvalidDataException or OverflowException)
        {
            return null;
        }
        finally
        {
            apk.Position = 0;
        }
    }

    private static byte[]? BloqueFirma(Stream apk)
    {
        var inicioDirectorio = InicioDirectorioCentral(apk);
        if (inicioDirectorio is null || inicioDirectorio < 32)
        {
            return null;
        }

        var pie = Leer(apk, inicioDirectorio.Value - 24, 24);
        if (!pie.AsSpan(8).SequenceEqual(Magia))
        {
            return null;
        }

        var tamano = BinaryPrimitives.ReadUInt64LittleEndian(pie);
        var inicioBloque = inicioDirectorio.Value - (long)tamano - 8;
        if (tamano < 24 || inicioBloque < 0)
        {
            return null;
        }

        var bloque = Leer(apk, inicioBloque, checked((int)(tamano + 8)));
        if (BinaryPrimitives.ReadUInt64LittleEndian(bloque) != tamano)
        {
            return null;
        }

        return bloque.AsSpan(8, bloque.Length - 8 - 24).ToArray();
    }

    private static long? InicioDirectorioCentral(Stream apk)
    {
        if (apk.Length < TamanoFinDirectorio)
        {
            return null;
        }

        var cola = (int)Math.Min(apk.Length, ushort.MaxValue + TamanoFinDirectorio);
        var final = Leer(apk, apk.Length - cola, cola);
        for (var i = final.Length - TamanoFinDirectorio; i >= 0; i--)
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(final.AsSpan(i)) != FirmaFinDirectorio)
            {
                continue;
            }

            var comentario = BinaryPrimitives.ReadUInt16LittleEndian(final.AsSpan(i + 20));
            if (i + TamanoFinDirectorio + comentario != final.Length)
            {
                continue;
            }

            return BinaryPrimitives.ReadUInt32LittleEndian(final.AsSpan(i + 16));
        }

        return null;
    }

    private static Dictionary<uint, byte[]> Pares(byte[] pares)
    {
        var resultado = new Dictionary<uint, byte[]>();
        var posicion = 0;
        while (posicion + 12 <= pares.Length)
        {
            var largo = checked((int)BinaryPrimitives.ReadUInt64LittleEndian(pares.AsSpan(posicion)));
            if (largo < 4 || posicion + 8 + largo > pares.Length)
            {
                throw new InvalidDataException();
            }

            var id = BinaryPrimitives.ReadUInt32LittleEndian(pares.AsSpan(posicion + 8));
            resultado.TryAdd(id, pares.AsSpan(posicion + 12, largo - 4).ToArray());
            posicion += 8 + largo;
        }

        return resultado;
    }

    // valor = firmantes; firmante = datos firmados, ...; datos firmados = resúmenes, certificados, ...
    private static byte[] PrimerCertificado(byte[] valor)
    {
        var firmantes = ConPrefijo(valor, 0);
        var firmante = ConPrefijo(firmantes, 0);
        var datosFirmados = ConPrefijo(firmante, 0);
        var resumenes = ConPrefijo(datosFirmados, 0);
        var certificados = ConPrefijo(datosFirmados, 4 + resumenes.Length);
        return ConPrefijo(certificados, 0);
    }

    private static byte[] ConPrefijo(byte[] datos, int posicion)
    {
        var largo = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(datos.AsSpan(posicion)));
        if (largo < 0 || posicion + 4 + largo > datos.Length)
        {
            throw new InvalidDataException();
        }

        return datos.AsSpan(posicion + 4, largo).ToArray();
    }

    private static byte[] Leer(Stream apk, long posicion, int cantidad)
    {
        apk.Position = posicion;
        var buffer = new byte[cantidad];
        apk.ReadExactly(buffer);
        return buffer;
    }
}
