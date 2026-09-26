using FluentAssertions;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;

namespace NewRich.Pda.Tests;

public sealed class PdaConexionTests
{
    [Fact]
    public void El_codigo_del_dispositivo_sale_del_modelo_del_equipo()
    {
        PdaConexion.CodigoDe("RMX3710").Should().Be("CEL-RMX3710");
        PdaConexion.CodigoDe("H10").Should().Be("CEL-H10");
    }

    [Fact]
    public void Login_incluye_el_codigo_del_dispositivo_sin_pedirlo_en_pantalla()
    {
        var request = PdaConexion.Login("alejitoo", "x");

        request.Usuario.Should().Be("alejitoo");
        request.CodigoDispositivo.Should().Be(PdaConexion.CodigoDispositivo);
        request.CodigoDispositivo.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Dispositivo_fisico_y_emulador_usan_el_api_de_produccion()
    {
        const string produccion = "https://api-ventas-prod-ffh4dmdhgpcsapda.westus3-01.azurewebsites.net/";

        PdaConexion.BaseUrl(emulador: false).Should().Be(produccion);
        PdaConexion.BaseUrl(emulador: true).Should().Be(produccion);
        PdaConexion.UrlsPara(emulador: false).Should().Equal(produccion);
        PdaConexion.UrlsPara(emulador: true).Should().Equal(produccion);
        PdaConexion.UrlProduccion.Should().StartWith("https://");
    }

    [Fact]
    public void Hub_de_chat_cuelga_de_la_misma_base_de_la_api()
    {
        PdaConexion.HubChat(PdaConexion.UrlProduccion).Should().Be(
            "https://api-ventas-prod-ffh4dmdhgpcsapda.westus3-01.azurewebsites.net/hubs/chat");
    }
}
