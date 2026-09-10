using FluentAssertions;
using NewRich.Application.Services;
using NewRich.Domain.Enums;

namespace NewRich.UnitTests;

public sealed class TirillaCuerpoTests
{
    [Fact]
    public void Combinado_usa_columna_total_y_listado_de_loterias()
    {
        TirillaCuerpo.EsCombinada(TipoApuesta.COMBINADO).Should().BeTrue();
        TirillaCuerpo.EsCombinada(TipoApuesta.INDIVIDUAL).Should().BeFalse();
        TirillaCuerpo.EtiquetaTipo(TipoApuesta.COMBINADO).Should().Be("COMBINADO");
        TirillaCuerpo.EtiquetaTipo(TipoApuesta.INDIVIDUAL).Should().Be("INDIVIDUAL");
    }

    [Fact]
    public void Leyenda_incluye_la_vigencia_en_dias()
    {
        var texto = TirillaCuerpo.Leyenda(30);

        texto.Should().Contain("GRACIAS POR SU COMPRA.");
        texto.Should().Contain("CONSERVE SU TICKET EN PERFECTO ESTADO.");
        texto.Should().Contain("30 días");
        texto.Should().Contain("el premio caducará y no será pagado.");
        texto.Should().Contain("La aprobación del premio se realizará después de transcurridas 24 horas desde el momento en que el cliente lo haya reportado como ganador.");
        texto.Should().EndWith("ganador.");
    }

    [Fact]
    public void Leyenda_usa_el_cuerpo_parametrizado_y_conserva_gracias()
    {
        var texto = TirillaCuerpo.Leyenda(12, "Conserve el papel.\nVigencia: {vigenciaDias} días.\nAprobación a las 24 horas.");

        texto.Should().StartWith("GRACIAS POR SU COMPRA.");
        texto.Should().Contain("Conserve el papel.");
        texto.Should().Contain("12 días");
        texto.Should().Contain("Aprobación a las 24 horas.");
        texto.Should().NotContain("{vigenciaDias}");
        texto.Should().NotContain("CONSERVE SU TICKET EN PERFECTO ESTADO.");
    }

    [Fact]
    public void Leyenda_quita_gracias_si_el_administrador_la_pego_en_el_cuerpo()
    {
        var texto = TirillaCuerpo.Leyenda(7, "GRACIAS POR SU COMPRA.\nSolo el cuerpo.");

        texto.Should().Be("GRACIAS POR SU COMPRA." + Environment.NewLine + "Solo el cuerpo.");
    }

    [Fact]
    public void LeyendaDeRespuesta_compone_si_llega_vacia_o_antigua()
    {
        TirillaCuerpo.LeyendaDeRespuesta(30, null).Should().Be(TirillaCuerpo.Leyenda(30));
        TirillaCuerpo.LeyendaDeRespuesta(30, "").Should().Be(TirillaCuerpo.Leyenda(30));
        TirillaCuerpo.LeyendaDeRespuesta(30, "Recuerde cuidar este boleto, se paga al portador.")
            .Should().Be(TirillaCuerpo.Leyenda(30));
        TirillaCuerpo.LeyendaDeRespuesta(15, TirillaCuerpo.Leyenda(15))
            .Should().Be(TirillaCuerpo.Leyenda(15));
    }

    [Fact]
    public void Pesos_usa_formato_colombiano()
    {
        TirillaCuerpo.Pesos(5000).Should().Be("$5.000");
    }
}
