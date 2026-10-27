using FluentAssertions;
using NewRich.Application.Services;

namespace NewRich.UnitTests;

public sealed class LectorFirmaApkTests
{
    [Theory]
    [InlineData(ApkFirmadoFalso.BloqueV2)]
    [InlineData(ApkFirmadoFalso.BloqueV3)]
    public void Lee_la_huella_del_certificado_del_bloque_de_firma(uint idBloque)
    {
        var certificado = ApkFirmadoFalso.CertificadoNuevo();
        using var apk = new MemoryStream(ApkFirmadoFalso.Firmado(certificado, idBloque));

        LectorFirmaApk.Huella(apk).Should().Be(ApkFirmadoFalso.Huella(certificado));
    }

    [Fact]
    public void Prefiere_el_bloque_v3_cuando_el_apk_trae_v2_y_v3()
    {
        var v2 = ApkFirmadoFalso.CertificadoNuevo("CN=V2");
        var v3 = ApkFirmadoFalso.CertificadoNuevo("CN=V3");
        var soloV2 = ApkFirmadoFalso.Firmado(v2);
        var soloV3 = ApkFirmadoFalso.Firmado(v3, ApkFirmadoFalso.BloqueV3);
        using var apk = new MemoryStream(ApkFirmadoFalso.Firmado(
        [
            (ApkFirmadoFalso.BloqueV2, ValorDe(soloV2)),
            (ApkFirmadoFalso.BloqueV3, ValorDe(soloV3))
        ]));

        LectorFirmaApk.Huella(apk).Should().Be(ApkFirmadoFalso.Huella(v3));
    }

    [Fact]
    public void Un_zip_sin_bloque_de_firma_no_tiene_huella()
    {
        using var apk = new MemoryStream(ApkFirmadoFalso.SinFirma());

        LectorFirmaApk.Huella(apk).Should().BeNull();
    }

    [Fact]
    public void Un_archivo_que_no_es_zip_no_tiene_huella()
    {
        using var apk = new MemoryStream([1, 2, 3, 4, 5]);

        LectorFirmaApk.Huella(apk).Should().BeNull();
    }

    [Fact]
    public void Deja_el_archivo_al_inicio_para_poder_subirlo_despues()
    {
        using var apk = new MemoryStream(ApkFirmadoFalso.Firmado(ApkFirmadoFalso.CertificadoNuevo()));

        LectorFirmaApk.Huella(apk);

        apk.Position.Should().Be(0);
    }

    // Saca el valor del único par de un APK falso para combinarlo con otro.
    private static byte[] ValorDe(byte[] apk)
    {
        var texto = System.Text.Encoding.ASCII.GetBytes("APK Sig Block 42");
        var fin = apk.AsSpan().IndexOf(texto);
        var tamano = (int)BitConverter.ToUInt64(apk, fin - 8);
        var inicio = fin - 8 - tamano + 16 + 8;
        var largoPar = (int)BitConverter.ToUInt64(apk, inicio);
        return apk.AsSpan(inicio + 12, largoPar - 4).ToArray();
    }
}
