using FluentAssertions;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class ResolucionBoletoReglaTests
{
    [Fact]
    public void Sin_ninguna_loteria_publicada_el_boleto_sigue_jugado()
    {
        var lineas = new[]
        {
            new LineaResultadoBoleto("Armenia", "9845", null),
            new LineaResultadoBoleto("Cali", "9845", null)
        };

        ResolucionBoletoRegla.Resolver(lineas).Should().Be(EstadoBoleto.Jugado);
    }

    [Fact]
    public void Si_acierta_en_la_loteria_publicada_el_boleto_queda_ganador_aunque_falten_las_demas()
    {
        var lineas = new[]
        {
            new LineaResultadoBoleto("Armenia", "5432", "5432"),
            new LineaResultadoBoleto("Cali", "5432", null)
        };

        ResolucionBoletoRegla.Resolver(lineas).Should().Be(EstadoBoleto.Ganador);
    }

    [Fact]
    public void Si_no_acierta_en_la_loteria_publicada_y_faltan_otras_el_boleto_sigue_jugado()
    {
        var lineas = new[]
        {
            new LineaResultadoBoleto("Armenia", "9845", "5432"),
            new LineaResultadoBoleto("Cali", "9845", null)
        };

        ResolucionBoletoRegla.Resolver(lineas).Should().Be(EstadoBoleto.Jugado);
    }

    [Fact]
    public void Con_todas_las_loterias_publicadas_y_sin_aciertos_el_boleto_queda_no_ganador()
    {
        var lineas = new[]
        {
            new LineaResultadoBoleto("Armenia", "9845", "5432"),
            new LineaResultadoBoleto("Cali", "9845", "1111")
        };

        ResolucionBoletoRegla.Resolver(lineas).Should().Be(EstadoBoleto.NoGanador);
    }

    [Fact]
    public void Un_boleto_sin_lineas_sigue_jugado()
    {
        ResolucionBoletoRegla.Resolver([]).Should().Be(EstadoBoleto.Jugado);
    }

    [Fact]
    public void El_numero_ganador_se_compara_sin_espacios_sobrantes()
    {
        var lineas = new[] { new LineaResultadoBoleto("Armenia", "785 ", " 785") };

        ResolucionBoletoRegla.Resolver(lineas).Should().Be(EstadoBoleto.Ganador);
    }

    [Fact]
    public void Faltan_resultados_solo_cuando_alguna_loteria_sigue_sin_publicar()
    {
        var pendientes = new[]
        {
            new LineaResultadoBoleto("Armenia", "9845", "5432"),
            new LineaResultadoBoleto("Cali", "9845", null)
        };
        var completas = new[] { new LineaResultadoBoleto("Armenia", "9845", "5432") };

        ResolucionBoletoRegla.FaltanResultados(pendientes).Should().BeTrue();
        ResolucionBoletoRegla.FaltanResultados(completas).Should().BeFalse();
    }
}
