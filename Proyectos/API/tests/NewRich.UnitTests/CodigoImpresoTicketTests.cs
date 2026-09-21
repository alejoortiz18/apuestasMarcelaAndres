using FluentAssertions;
using NewRich.Application.Contracts.Offline;
using NewRich.Application.Services;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class CodigoImpresoTicketTests
{
    [Fact]
    public void Venta_en_linea_usa_el_prefijo_aol()
    {
        CodigoImpresoTicket.De("7986875", "1.clave.nonce.cipher.tag").Should().Be("AOL-7986875");
        CodigoImpresoTicket.De("7986875", null).Should().Be(CodigoPublicoGenerator.FormatoImpreso("7986875"));
    }

    [Fact]
    public void Venta_offline_muestra_el_consecutivo_de_la_tirilla()
    {
        var qr = SobreQrOfflineCodec.Armar("1.clave.nonce.cipher.tag", "OFF-000018", new JugadaOffline());

        CodigoImpresoTicket.De("4839201", qr).Should().Be("OFF-000018");
    }

    [Fact]
    public void Si_hay_consecutivo_offline_gana_sobre_el_codigo_interno()
    {
        CodigoImpresoTicket.De("4839201", "qr-interno", "OFF-000018").Should().Be("OFF-000018");
    }

    [Fact]
    public void SoloOffline_devuelve_el_consecutivo_cuando_la_venta_fue_offline()
    {
        CodigoImpresoTicket.SoloOffline("4839201", null, "OFF-000020").Should().Be("OFF-000020");
    }

    [Fact]
    public void SoloOffline_queda_vacio_cuando_la_venta_fue_en_linea()
    {
        CodigoImpresoTicket.SoloOffline("1699284", "1.clave.nonce.cipher.tag").Should().BeNull();
    }

    [Theory]
    [InlineData(null, "-")]
    [InlineData("", "-")]
    [InlineData("   ", "-")]
    [InlineData("AOL-1699284", "-")]
    [InlineData("OFF-000020", "OFF-000020")]
    public void CeldaVentaOffline_muestra_guion_si_no_hay_consecutivo(string? valor, string esperado)
    {
        CodigoImpresoTicket.CeldaVentaOffline(valor).Should().Be(esperado);
    }
}
