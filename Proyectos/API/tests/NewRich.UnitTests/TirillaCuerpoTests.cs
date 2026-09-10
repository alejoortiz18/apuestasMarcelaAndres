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
    }

    [Fact]
    public void Pesos_usa_formato_colombiano()
    {
        TirillaCuerpo.Pesos(5000).Should().Be("$5.000");
    }
}
