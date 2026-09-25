using FluentAssertions;
using NewRich.Pda.Core.Ventas;

namespace NewRich.Pda.Tests;

public class PoliticaVentaPdaTests
{
    [Fact]
    public void El_indicador_sin_internet_de_android_no_bloquea_la_venta_en_el_servidor()
    {
        PoliticaVentaPda.IntentarServidorAunqueAndroidReporteSinRed.Should().BeTrue();
    }

    [Fact]
    public void El_sondeo_al_servidor_no_hace_esperar_al_vendedor()
    {
        PoliticaVentaPda.MsSondeoServidor.Should().BeInRange(1000, 4000);
    }

    [Fact]
    public void Sin_servidor_y_sin_codigos_bloquea_la_venta()
    {
        PoliticaVentaPda.TrasFalloDeRed(0).Should().Be(CanalVenta.Bloqueado);
    }

    [Fact]
    public void Sin_servidor_con_codigos_ofrece_offline()
    {
        PoliticaVentaPda.TrasFalloDeRed(4).Should().Be(CanalVenta.OfrecerOffline);
    }
}
