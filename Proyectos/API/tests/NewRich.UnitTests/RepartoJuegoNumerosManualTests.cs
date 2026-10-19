using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class RepartoJuegoNumerosManualTests
{
    private static readonly Guid Chontico = Guid.NewGuid();
    private static readonly Guid Medellin = Guid.NewGuid();

    [Fact]
    public void Cada_numero_se_juega_en_cada_loteria_seleccionada()
    {
        RepartoJuegoNumerosManual.CantidadApuestas(5, 2).Should().Be(10);
        RepartoJuegoNumerosManual.CantidadApuestas(0, 2).Should().Be(0);
        RepartoJuegoNumerosManual.CantidadApuestas(3, 0).Should().Be(0);
    }

    [Fact]
    public void Diez_mil_en_cinco_numeros_y_dos_loterias_dan_mil_por_apuesta()
    {
        var apuestas = RepartoJuegoNumerosManual.Repartir(
            10_000m,
            ["1234", "4323", "5676", "2398", "9878"],
            [Chontico, Medellin]);

        apuestas.Should().HaveCount(10);
        apuestas.Should().OnlyContain(a => a.Valor == 1_000m);
        apuestas.Sum(a => a.Valor).Should().Be(10_000m);
        apuestas.Where(a => a.Numero == "1234").Select(a => a.LoteriaId)
            .Should().Equal(Chontico, Medellin);
    }

    [Fact]
    public void El_sobrante_se_suma_de_a_un_peso_a_las_primeras_apuestas_y_la_suma_es_exacta()
    {
        var apuestas = RepartoJuegoNumerosManual.Repartir(10_000m, ["111", "222", "333"], [Chontico]);

        apuestas.Select(a => a.Valor).Should().Equal(3_334m, 3_333m, 3_333m);
        apuestas.Sum(a => a.Valor).Should().Be(10_000m);
        RepartoJuegoNumerosManual.ValorBase(10_000m, 3).Should().Be(3_333m);
        RepartoJuegoNumerosManual.ApuestasConSobrante(10_000m, 3).Should().Be(1);
    }

    [Fact]
    public void El_reparto_sigue_el_orden_de_los_numeros_y_luego_el_de_las_loterias()
    {
        var apuestas = RepartoJuegoNumerosManual.Repartir(1_003m, ["1234", "5678"], [Chontico, Medellin]);

        apuestas.Select(a => (a.Numero, a.LoteriaId, a.Valor)).Should().Equal(
            ("1234", Chontico, 251m),
            ("1234", Medellin, 251m),
            ("5678", Chontico, 251m),
            ("5678", Medellin, 250m));
    }

    [Fact]
    public void No_alcanza_si_el_valor_es_menor_que_la_cantidad_de_apuestas()
    {
        RepartoJuegoNumerosManual.AlcanzaParaTodas(9m, 10).Should().BeFalse();
        RepartoJuegoNumerosManual.AlcanzaParaTodas(10m, 10).Should().BeTrue();
        RepartoJuegoNumerosManual.AlcanzaParaTodas(10m, 0).Should().BeFalse();
    }

    [Fact]
    public void Sin_numeros_o_sin_loterias_no_hay_apuestas()
    {
        RepartoJuegoNumerosManual.Repartir(10_000m, [], [Chontico]).Should().BeEmpty();
        RepartoJuegoNumerosManual.Repartir(10_000m, ["1234"], []).Should().BeEmpty();
        RepartoJuegoNumerosManual.ValorBase(10_000m, 0).Should().Be(0m);
    }
}
