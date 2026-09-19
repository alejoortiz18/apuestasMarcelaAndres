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

    [Fact]
    public void La_configuracion_no_muestra_filtros_adicionales()
    {
        var configuracion = ConsultaObservadorCampos.Obtener(PdaTexts.ConsultaConfiguracion);

        configuracion.Should().BeEmpty();
    }

    [Fact]
    public void Los_boletos_muestran_su_estado_y_codigo_compatibles_con_el_filtro()
    {
        var boletos = ConsultaObservadorCampos.Obtener(PdaTexts.ConsultaBoletos);

        boletos.Select(x => x.Etiqueta).Should().Contain(PdaTexts.TicketCode);
        boletos.Select(x => x.Etiqueta).Should().Contain(PdaTexts.NumeroApostado);
        boletos.Select(x => x.Etiqueta).Should().Contain(PdaTexts.EstadoBoleto);
    }

    [Fact]
    public void Las_ventas_muestran_numero_loteria_y_rango_de_fechas()
    {
        var ventas = ConsultaObservadorCampos.Obtener(PdaTexts.ConsultaVentas);

        ventas.Select(x => x.Etiqueta).Should().Contain(PdaTexts.NumeroApostado);
        ventas.Select(x => x.Etiqueta).Should().Contain(PdaTexts.Loteria);
        ventas.Select(x => x.Etiqueta).Should().Contain(PdaTexts.Desde);
        ventas.Select(x => x.Etiqueta).Should().Contain(PdaTexts.Hasta);
    }

    [Fact]
    public void Los_vendedores_muestran_grupo_y_rango_de_fechas()
    {
        var vendedores = ConsultaObservadorCampos.Obtener(PdaTexts.ConsultaVendedores);

        vendedores.Select(x => x.Etiqueta).Should().Contain(PdaTexts.Vendedor);
        vendedores.Select(x => x.Etiqueta).Should().Contain(PdaTexts.Grupo);
        vendedores.Select(x => x.Etiqueta).Should().Contain(PdaTexts.Desde);
        vendedores.Select(x => x.Etiqueta).Should().Contain(PdaTexts.Hasta);
    }

    [Fact]
    public void La_tarjeta_de_total_aparece_para_ventas_y_boletos()
    {
        ConsultaObservadorCampos.DebeMostrarTarjetaTotalVentas(PdaTexts.ConsultaVentas).Should().BeTrue();
        ConsultaObservadorCampos.DebeMostrarTarjetaTotalVentas(PdaTexts.ConsultaBoletos).Should().BeTrue();
    }

    [Fact]
    public void La_tarjeta_de_total_no_aparece_en_las_demas_consultas()
    {
        ConsultaObservadorCampos.DebeMostrarTarjetaTotalVentas(PdaTexts.ConsultaResultados).Should().BeFalse();
        ConsultaObservadorCampos.DebeMostrarTarjetaTotalVentas(PdaTexts.ConsultaVendedores).Should().BeFalse();
        ConsultaObservadorCampos.DebeMostrarTarjetaTotalVentas(PdaTexts.ConsultaDispositivos).Should().BeFalse();
        ConsultaObservadorCampos.DebeMostrarTarjetaTotalVentas(PdaTexts.ConsultaConfiguracion).Should().BeFalse();
    }

    [Fact]
    public void El_titulo_del_total_cambia_segun_el_tipo_de_consulta()
    {
        ConsultaObservadorCampos.TituloTotalFiltro(PdaTexts.ConsultaVentas).Should().Be(PdaTexts.TotalFiltroVentas);
        ConsultaObservadorCampos.TituloTotalFiltro(PdaTexts.ConsultaBoletos).Should().Be(PdaTexts.TotalFiltroBoletos);
        ConsultaObservadorCampos.TituloTotalFiltro(PdaTexts.ConsultaResultados).Should().BeNull();
    }
}
