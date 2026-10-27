using FluentAssertions;
using NewRich.Application.Contracts.ApkStore;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Actualizacion;

namespace NewRich.Pda.Tests;

public sealed class VerificacionApkTests
{
    private static readonly VersionApkResponse Esperada = new()
    {
        NumeroCompilacion = 5,
        Sha256 = "AA11",
        HuellaFirma = "0BB2265B606AD712"
    };

    [Fact]
    public void Un_apk_que_coincide_en_archivo_firma_y_version_se_puede_instalar()
    {
        var descargado = new DatosApkDescargado("aa11", "0B:B2:26:5B:60:6A:D7:12", 5);

        VerificacionApk.Problema(descargado, Esperada, instalada: 4).Should().BeNull();
    }

    [Fact]
    public void Un_archivo_distinto_al_publicado_no_se_instala()
    {
        var descargado = new DatosApkDescargado("FF00", "0BB2265B606AD712", 5);

        VerificacionApk.Problema(descargado, Esperada, instalada: 4).Should().Be(PdaTexts.ActualizacionArchivoAlterado);
    }

    [Theory]
    [InlineData("1234")]
    [InlineData(null)]
    public void Un_apk_firmado_con_otra_llave_o_sin_firma_no_se_instala(string? huella)
    {
        var descargado = new DatosApkDescargado("AA11", huella, 5);

        VerificacionApk.Problema(descargado, Esperada, instalada: 4).Should().Be(PdaTexts.ActualizacionFirmaDistinta);
    }

    [Theory]
    [InlineData(6L, 4)]
    [InlineData(null, 4)]
    [InlineData(5L, 5)]
    public void Una_version_que_no_corresponde_o_no_es_mayor_no_se_instala(long? compilacionDelArchivo, int instalada)
    {
        var descargado = new DatosApkDescargado("AA11", "0BB2265B606AD712", compilacionDelArchivo);

        VerificacionApk.Problema(descargado, Esperada, instalada).Should().Be(PdaTexts.ActualizacionVersionNoCorresponde);
    }

    [Fact]
    public void El_sha256_del_archivo_se_calcula_en_hexadecimal()
    {
        var ruta = Path.GetTempFileName();
        File.WriteAllText(ruta, "abc");

        try
        {
            VerificacionApk.Sha256DeArchivo(ruta).Should()
                .Be("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD");
        }
        finally
        {
            File.Delete(ruta);
        }
    }
}
