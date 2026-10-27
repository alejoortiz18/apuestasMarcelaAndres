using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using NewRich.Infrastructure.Security;

namespace NewRich.UnitTests;

public sealed class GeneradorLlaveFirmaTests
{
    private static readonly DateTime Ahora = new(2026, 10, 27, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Genera_una_llave_rsa_de_4096_bits_valida_30_años_que_abre_con_su_clave()
    {
        var llave = new GeneradorLlaveFirmaPkcs12().Generar("newrich", Ahora);

        using var certificado = X509CertificateLoader.LoadPkcs12(llave.Almacen, llave.Clave, X509KeyStorageFlags.Exportable);
        certificado.HasPrivateKey.Should().BeTrue();
        certificado.GetRSAPrivateKey()!.KeySize.Should().Be(4096);
        certificado.NotAfter.ToUniversalTime().Should().BeOnOrAfter(Ahora.AddYears(30).AddMinutes(-1));
        llave.Huella.Should().Be(Convert.ToHexString(SHA256.HashData(certificado.RawData)));
        llave.Clave.Length.Should().BeGreaterThanOrEqualTo(32);
    }

    [Fact]
    public void La_llave_y_el_certificado_llevan_el_alias_para_que_android_la_encuentre()
    {
        var llave = new GeneradorLlaveFirmaPkcs12().Generar("newrich", Ahora);

        var info = Pkcs12Info.Decode(llave.Almacen, out _, skipCopy: true);
        info.VerifyMac(llave.Clave).Should().BeTrue();
        var bolsas = info.AuthenticatedSafe.SelectMany(contenido =>
        {
            if (contenido.ConfidentialityMode == Pkcs12ConfidentialityMode.Password)
            {
                contenido.Decrypt(llave.Clave);
            }

            return contenido.GetBags();
        }).ToList();

        bolsas.Should().HaveCount(2);
        bolsas.Select(Alias).Should().AllBe("newrich");
    }

    [Fact]
    public void Cada_llave_nueva_es_distinta()
    {
        var generador = new GeneradorLlaveFirmaPkcs12();

        generador.Generar("newrich", Ahora).Huella.Should().NotBe(generador.Generar("newrich", Ahora).Huella);
    }

    private static string? Alias(Pkcs12SafeBag bolsa)
    {
        var atributo = bolsa.Attributes.Cast<CryptographicAttributeObject>()
            .FirstOrDefault(a => a.Oid.Value == "1.2.840.113549.1.9.20");
        if (atributo is null)
        {
            return null;
        }

        var lector = new AsnReader(atributo.Values[0].RawData, AsnEncodingRules.DER);
        return lector.ReadCharacterString(UniversalTagNumber.BMPString);
    }
}
