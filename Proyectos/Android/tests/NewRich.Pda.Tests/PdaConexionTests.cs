using FluentAssertions;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;

namespace NewRich.Pda.Tests;

public sealed class PdaConexionTests
{
    [Fact]
    public void Login_incluye_el_codigo_del_dispositivo_sin_pedirlo_en_pantalla()
    {
        var request = PdaConexion.Login("alejitoo", "x");

        request.Usuario.Should().Be("alejitoo");
        request.CodigoDispositivo.Should().Be(PdaConexion.CodigoDispositivo);
        request.CodigoDispositivo.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Url_de_dispositivo_fisico_usa_la_red_local_y_no_el_emulador()
    {
        PdaConexion.BaseUrl(emulador: false).Should().Be(PdaConexion.UrlRedLocal);
        PdaConexion.UrlRedLocal.Should().StartWith("http://");
        PdaConexion.UrlRedLocal.Should().NotContain("10.0.2.2");
        PdaConexion.UrlRedLocal.Should().Contain(":5295");
    }

    [Fact]
    public void Url_de_emulador_usa_el_alias_del_host()
    {
        PdaConexion.BaseUrl(emulador: true).Should().Be("http://10.0.2.2:5295/");
    }

    [Fact]
    public void Dispositivo_fisico_prueba_la_red_local_y_el_puente_usb()
    {
        PdaConexion.UrlsPara(emulador: false).Should().Equal(
            "http://192.168.1.19:5295/",
            "http://127.0.0.1:5295/");
    }
}
