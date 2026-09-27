using FluentAssertions;
using NewRich.Admin.Constants;
using NewRich.Admin.Services.Pda;

namespace NewRich.Admin.Tests;

public sealed class InstalacionApkTests
{
    [Fact]
    public void La_identidad_interna_viaja_en_un_solo_comando_para_que_el_celular_no_desvie_el_archivo()
    {
        string.Join(" ", InstalacionApk.ArgumentosIdentidadInterna("PDA-8279866F"))
            .Should().Be("shell run-as com.newrich.pda sh -c 'echo PDA-8279866F > files/identidad.txt'");
        string.Join(" ", InstalacionApk.ArgumentosSerieInterna("a6hy7l99cizhlbtw"))
            .Should().Be("shell run-as com.newrich.pda sh -c 'echo a6hy7l99cizhlbtw > files/serie.txt'");
    }

    [Fact]
    public void El_instalador_directo_acepta_el_apk_de_prueba_y_evita_el_envio_incremental()
    {
        string.Join(" ", InstalacionApk.ArgumentosDirectos(@"C:\apk\NewRich.apk"))
            .Should().Be(@"install -r -t -d --no-incremental C:\apk\NewRich.apk");
    }

    [Fact]
    public void Un_celular_que_cancela_la_instalacion_se_reintenta_por_el_instalador_del_sistema()
    {
        var fallo = new AdbResultado(1, string.Empty, "Failure [INSTALL_FAILED_USER_RESTRICTED: Install canceled by user]");

        InstalacionApk.DebeReintentarComoCelular(fallo).Should().BeTrue();
        InstalacionApk.MensajeFallo(fallo).Should().Be(UiTexts.PdaFalloInstalacionCelular);
    }

    [Fact]
    public void Un_apk_marcado_como_prueba_tambien_se_reintenta()
    {
        InstalacionApk.DebeReintentarComoCelular(new AdbResultado(1, string.Empty, "INSTALL_FAILED_TEST_ONLY"))
            .Should().BeTrue();
    }

    [Fact]
    public void Una_firma_distinta_exige_reemplazar_el_paquete()
    {
        InstalacionApk.DebeReemplazarPaquete(new AdbResultado(1, string.Empty, "INSTALL_FAILED_UPDATE_INCOMPATIBLE: Existing package signatures do not match"))
            .Should().BeTrue();
        InstalacionApk.ArgumentosDesinstalar("com.newrich.pda")
            .Should().Equal("uninstall", "com.newrich.pda");
    }

    [Fact]
    public void Un_pda_que_instala_no_cambia_de_camino()
    {
        InstalacionApk.DebeReintentarComoCelular(new AdbResultado(0, "Success", string.Empty))
            .Should().BeFalse();
        InstalacionApk.MensajeFallo(new AdbResultado(1, string.Empty, "unknown"))
            .Should().Be(UiTexts.PdaFalloInstalacion);
    }
}
