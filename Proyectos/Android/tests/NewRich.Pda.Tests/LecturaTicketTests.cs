using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class LecturaTicketTests
{
    [Theory]
    [InlineData("AOL-0000153")]
    [InlineData("0000153")]
    [InlineData("NR2.OFF-000015.a1b2c3")]
    [InlineData("OFF-000018")]
    [InlineData("{\"codigo\":\"x\"}")]
    [InlineData("1.8f2c1a6e4b094d739e215a7c0b8d3f14.abc.def.ghi")]
    public void Marca_completa_una_lectura_lista_para_consultar(string texto)
    {
        LecturaTicket.ListaParaConsultar(texto).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12")]
    [InlineData("NR1.")]
    [InlineData("NR3.ABC")]
    [InlineData("NR3.ABCDEFG")]
    [InlineData("{")]
    public void No_consulta_lecturas_incompletas(string texto)
    {
        LecturaTicket.ListaParaConsultar(texto).Should().BeFalse();
    }

    [Fact]
    public void El_qr_del_vendedor_en_pda_abre_el_escaner_del_terminal()
    {
        LecturaTicket.AbrirEscanerDispositivo.Should().BeTrue();
        LecturaTicket.MsEstabilizacionWedge.Should().Be(400);
        PerfilDispositivo.VendedorUsaEscanerNativo("H10", "SENRAISE").Should().BeFalse();
        PerfilDispositivo.VendedorUsaCamaraInterna("H10", "SENRAISE").Should().BeTrue();
        LecturaTicket.AbrirCamaraAlValidarQr.Should().BeFalse();
        LecturaTicket.DispararLectorAlAbrirQr.Should().BeFalse();
        EscanerQrDispositivo.Paquete.Should().Be("com.android.qr_codescan");
        EscanerQrDispositivo.Actividad.Should().Be("com.android.qr_codescan.MipcaActivityCapture");
        EscanerQrDispositivo.Accion.Should().Be("com.google.zxing.client.android.SCAN");
        EscanerQrDispositivo.ExtrasInicio["SCAN_MODE"].Should().Be("QR_CODE_MODE");
        EscanerQrDispositivo.ExtrasInicio["SCAN_FORMATS"].Should().Be("QR_CODE");
        EscanerQrDispositivo.ExtrasInicio["RESULT_DISPLAY_DURATION_MS"].Should().Be("0");
    }

    [Theory]
    [InlineData(" NR1.abcDEFghij ", "", "NR1.abcDEFghij")]
    [InlineData("", "AOL-0000153", "AOL-0000153")]
    [InlineData("   ", "  ", null)]
    public void Pega_en_el_campo_el_codigo_leido_o_el_que_ya_esta(string leido, string caja, string? esperado)
    {
        LecturaTicket.CodigoParaPegar(leido, caja).Should().Be(esperado);
    }
}
