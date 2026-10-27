using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace NewRich.UnitTests;

/// <summary>Arma un APK mínimo: un zip con el bloque de firma de Android antes del directorio central.</summary>
internal static class ApkFirmadoFalso
{
    public const uint BloqueV2 = 0x7109871a;
    public const uint BloqueV3 = 0xf05368c0;

    public static byte[] CertificadoNuevo(string nombre = "CN=Prueba")
    {
        using var rsa = RSA.Create(2048);
        var pedido = new CertificateRequest(nombre, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificado = pedido.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        return certificado.RawData;
    }

    public static string Huella(byte[] certificado) => Convert.ToHexString(SHA256.HashData(certificado));

    public static byte[] SinFirma() => Zip();

    public static byte[] Firmado(byte[] certificado, uint idBloque = BloqueV2) =>
        Firmado([(idBloque, ValorFirmante(certificado))]);

    public static byte[] Firmado(IReadOnlyList<(uint Id, byte[] Valor)> pares)
    {
        var zip = Zip();
        var eocd = FinDirectorio(zip);
        var inicioDirectorio = (int)BinaryPrimitives.ReadUInt32LittleEndian(zip.AsSpan(eocd + 16));
        var bloque = Bloque(pares);

        var resultado = new byte[zip.Length + bloque.Length];
        zip.AsSpan(0, inicioDirectorio).CopyTo(resultado);
        bloque.CopyTo(resultado, inicioDirectorio);
        zip.AsSpan(inicioDirectorio).CopyTo(resultado.AsSpan(inicioDirectorio + bloque.Length));
        BinaryPrimitives.WriteUInt32LittleEndian(
            resultado.AsSpan(eocd + bloque.Length + 16),
            (uint)(inicioDirectorio + bloque.Length));
        return resultado;
    }

    private static byte[] ValorFirmante(byte[] certificado)
    {
        var digests = Prefijo(Prefijo([1, 2, 3, 4]));
        var certificados = Prefijo(Prefijo(certificado));
        var datosFirmados = Prefijo([.. digests, .. certificados, .. Prefijo([])]);
        var firmante = Prefijo([.. datosFirmados, .. Prefijo([]), .. Prefijo([9, 9])]);
        return Prefijo(firmante);
    }

    private static byte[] Bloque(IReadOnlyList<(uint Id, byte[] Valor)> pares)
    {
        using var cuerpo = new MemoryStream();
        foreach (var (id, valor) in pares)
        {
            Escribir64(cuerpo, (ulong)(valor.Length + 4));
            Escribir32(cuerpo, id);
            cuerpo.Write(valor);
        }

        var tamano = (ulong)(cuerpo.Length + 8 + 16);
        using var bloque = new MemoryStream();
        Escribir64(bloque, tamano);
        cuerpo.Position = 0;
        cuerpo.CopyTo(bloque);
        Escribir64(bloque, tamano);
        bloque.Write(Encoding.ASCII.GetBytes("APK Sig Block 42"));
        return bloque.ToArray();
    }

    private static byte[] Zip()
    {
        using var memoria = new MemoryStream();
        using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var entrada = zip.CreateEntry("AndroidManifest.xml").Open();
            entrada.Write(Encoding.UTF8.GetBytes("<manifest />"));
        }

        return memoria.ToArray();
    }

    private static int FinDirectorio(byte[] zip)
    {
        for (var i = zip.Length - 22; i >= 0; i--)
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(zip.AsSpan(i)) == 0x06054b50)
            {
                return i;
            }
        }

        throw new InvalidOperationException("El zip no tiene fin de directorio central.");
    }

    private static byte[] Prefijo(byte[] contenido)
    {
        var resultado = new byte[contenido.Length + 4];
        BinaryPrimitives.WriteUInt32LittleEndian(resultado, (uint)contenido.Length);
        contenido.CopyTo(resultado, 4);
        return resultado;
    }

    private static void Escribir64(Stream destino, ulong valor)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, valor);
        destino.Write(buffer);
    }

    private static void Escribir32(Stream destino, uint valor)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, valor);
        destino.Write(buffer);
    }
}
