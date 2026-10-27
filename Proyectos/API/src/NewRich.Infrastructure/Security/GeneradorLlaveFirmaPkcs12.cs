using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using NewRich.Application.Abstractions;

namespace NewRich.Infrastructure.Security;

/// <summary>
/// Genera la llave de firma de Android: RSA 4096 con certificado propio de 30 años, en un PKCS12 que keytool y
/// apksigner abren. El alias va como friendlyName en las dos bolsas porque Java lo usa para encontrar la llave,
/// y en Linux .NET no permite ponerlo con X509Certificate2.FriendlyName.
/// </summary>
public sealed class GeneradorLlaveFirmaPkcs12 : IGeneradorLlaveFirma
{
    private const string OidFriendlyName = "1.2.840.113549.1.9.20";
    private const int Iteraciones = 100_000;
    private static readonly PbeParameters Cifrado = new(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, Iteraciones);

    public LlaveFirmaGenerada Generar(string alias, DateTime ahoraUtc)
    {
        using var rsa = RSA.Create(4096);
        var pedido = new CertificateRequest("CN=NewRich, O=NewRich, C=CO", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var desde = new DateTimeOffset(DateTime.SpecifyKind(ahoraUtc, DateTimeKind.Utc)).AddDays(-1);
        using var certificado = pedido.CreateSelfSigned(desde, desde.AddDays(1).AddYears(30));
        var clave = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));

        var nombre = new Pkcs9AttributeObject(new Oid(OidFriendlyName), BmpString(alias));
        var idLlave = new Pkcs9LocalKeyId(certificado.GetCertHash());

        var llaves = new Pkcs12SafeContents();
        var bolsaLlave = llaves.AddShroudedKey(rsa, clave, Cifrado);
        bolsaLlave.Attributes.Add(nombre);
        bolsaLlave.Attributes.Add(idLlave);

        var certificados = new Pkcs12SafeContents();
        var bolsaCertificado = certificados.AddCertificate(certificado);
        bolsaCertificado.Attributes.Add(nombre);
        bolsaCertificado.Attributes.Add(idLlave);

        var almacen = new Pkcs12Builder();
        almacen.AddSafeContentsEncrypted(certificados, clave, Cifrado);
        almacen.AddSafeContentsUnencrypted(llaves);
        almacen.SealWithMac(clave, HashAlgorithmName.SHA256, Iteraciones);

        return new LlaveFirmaGenerada(almacen.Encode(), clave, Convert.ToHexString(SHA256.HashData(certificado.RawData)));
    }

    private static byte[] BmpString(string texto)
    {
        var escritor = new AsnWriter(AsnEncodingRules.DER);
        escritor.WriteCharacterString(UniversalTagNumber.BMPString, texto);
        return escritor.Encode();
    }
}
