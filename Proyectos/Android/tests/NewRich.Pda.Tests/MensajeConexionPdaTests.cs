using FluentAssertions;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Ventas;

namespace NewRich.Pda.Tests;

public sealed class MensajeConexionPdaTests
{
    [Fact]
    public void Sin_servidor_informa_que_puede_operar_offline_con_codigos()
    {
        PdaTexts.SinConexionServidor.Should().Be(
            "No hay conexión con el servidor. Puede usar el PDA sin conexión. Para vender debe tener códigos offline descargados.");
        PdaTexts.SinConexionServidor.Should().NotContain("USB");
        PdaTexts.SinConexionServidor.Should().NotContain("Wi-Fi");
        PdaTexts.AvisoOperacionSinServidor(12).Should().Be(
            "No hay conexión con el servidor. Puede seguir operando con 12 códigos offline disponibles.");
        PdaTexts.AvisoOperacionSinServidor(0).Should().Be(
            "No hay conexión con el servidor. No cuenta con más códigos offline para realizar ventas. Debe conectarse o solicitar más códigos al administrador.");
    }

    [Fact]
    public void La_venta_sin_red_pregunta_si_continuar_offline()
    {
        PdaTexts.ContinuarOfflinePregunta.Should().Be("Conexión no disponible. ¿Desea continuar en modo offline?");
    }

    [Fact]
    public void El_ingreso_nuevo_si_pide_conexion()
    {
        PdaTexts.IngresoRequiereConexion.Should().Be(
            "No hay conexión con el servidor. Para iniciar sesión debe conectarse. Después podrá vender sin conexión con códigos offline.");
    }

    [Fact]
    public void Sin_codigos_explica_que_hay_que_conectarse_a_descargarlos()
    {
        PdaTexts.BorradorConservado.Should().Contain("códigos offline");
    }

    [Fact]
    public void Politica_con_codigos_permite_el_canal_offline()
    {
        PoliticaVentaPda.TrasFalloDeRed(1).Should().Be(CanalVenta.OfrecerOffline);
        PoliticaVentaPda.TrasFalloDeRed(0).Should().Be(CanalVenta.Bloqueado);
    }
}
