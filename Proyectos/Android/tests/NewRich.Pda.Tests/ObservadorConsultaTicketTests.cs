using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class ObservadorConsultaTicketTests
{
    [Fact]
    public void Usa_una_ruta_de_api_propia_del_observador()
    {
        ObservadorConsultaTicket.RutaApi.Should().Be("api/ObservadorAndroid/ConsultarTicketMob");
    }

    [Fact]
    public void Acepta_el_contenido_del_qr_de_una_tirilla()
    {
        ObservadorConsultaTicket.ListoParaConsultar("NR3.AEDTSOJYGQ4TGOAA").Should().BeTrue();
        ObservadorConsultaTicket.ListoParaConsultar("NR2.AEDTSOJYGQ4TGOAA").Should().BeTrue();
        ObservadorConsultaTicket.ListoParaConsultar("NR1.AEDTSOJYGQ4TGOAA").Should().BeTrue();
        ObservadorConsultaTicket.ListoParaConsultar("").Should().BeFalse();
    }

    [Fact]
    public void Acepta_el_codigo_impreso_del_recibo()
    {
        ObservadorConsultaTicket.ListoParaConsultar("AOL-6661571").Should().BeTrue();
        ObservadorConsultaTicket.ListoParaConsultar("6661571").Should().BeTrue();
        ObservadorConsultaTicket.ListoParaConsultar(" 6661571 ").Should().BeTrue();
    }

    [Fact]
    public void Acepta_el_payload_cifrado_del_qr()
    {
        var payload = "1.8f2c1a6e4b094d739e215a7c0b8d3f14.AAAA.BBBBBBBB.CCCC";

        ObservadorConsultaTicket.ListoParaConsultar(payload).Should().BeTrue();
    }

    [Fact]
    public void Rechaza_texto_que_no_es_un_ticket()
    {
        ObservadorConsultaTicket.ListoParaConsultar(null).Should().BeFalse();
        ObservadorConsultaTicket.ListoParaConsultar("   ").Should().BeFalse();
        ObservadorConsultaTicket.ListoParaConsultar("hola mundo").Should().BeFalse();
        ObservadorConsultaTicket.ListoParaConsultar("AOL-12").Should().BeFalse();
        ObservadorConsultaTicket.ListoParaConsultar("NR3.").Should().BeFalse();
        ObservadorConsultaTicket.ListoParaConsultar("12345678").Should().BeFalse();
    }

    [Fact]
    public void No_depende_de_la_regla_del_vendedor()
    {
        const string qrTirilla = "NR3.AEDTSOJYGQ4TGOAA";

        LecturaTicket.ListaParaConsultar(qrTirilla).Should().BeFalse();
        ObservadorConsultaTicket.ListoParaConsultar(qrTirilla).Should().BeTrue();
    }
}
