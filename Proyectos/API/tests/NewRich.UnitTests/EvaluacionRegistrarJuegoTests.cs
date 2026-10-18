using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class EvaluacionRegistrarJuegoTests
{
    [Fact]
    public void Evaluar_no_registra_si_las_dos_jugadas_estan_vacias()
    {
        var decision = EvaluacionRegistrarJuego.Evaluar(
            [new FilaRegistroEntrada(false, false, false)],
            0,
            false,
            0,
            [string.Empty],
            [],
            false);

        decision.Filas.Should().Be(ParteJuego.Vacia);
        decision.Numeros.Should().Be(ParteJuego.Vacia);
        decision.PuedeRegistrar.Should().BeFalse();
    }

    [Fact]
    public void Evaluar_registra_las_dos_cuando_estan_completas()
    {
        var decision = EvaluacionRegistrarJuego.Evaluar(
            [new FilaRegistroEntrada(true, true, true)],
            2,
            true,
            10_000,
            ["1234", "5678"],
            [],
            false);

        decision.Filas.Should().Be(ParteJuego.Lista);
        decision.Numeros.Should().Be(ParteJuego.Lista);
        decision.PuedeRegistrar.Should().BeTrue();
        decision.Apuestas.Should().Be(4);
        decision.ValorPorApuesta.Should().Be(2_500);
        decision.ApuestasConSobrante.Should().Be(0);
    }

    [Fact]
    public void Evaluar_deja_pasar_solo_la_jugada_completa()
    {
        var soloFilas = EvaluacionRegistrarJuego.Evaluar(
            [new FilaRegistroEntrada(true, true, true)],
            0,
            false,
            0,
            [string.Empty],
            [],
            false);
        var soloNumeros = EvaluacionRegistrarJuego.Evaluar(
            [new FilaRegistroEntrada(false, false, false)],
            1,
            true,
            1_000,
            ["3214"],
            [],
            false);

        soloFilas.PuedeRegistrar.Should().BeTrue();
        soloFilas.Numeros.Should().Be(ParteJuego.Vacia);
        soloNumeros.PuedeRegistrar.Should().BeTrue();
        soloNumeros.Filas.Should().Be(ParteJuego.Vacia);
    }

    [Fact]
    public void Evaluar_no_registra_una_jugada_a_medias_ni_con_horario_cerrado()
    {
        var parcial = EvaluacionRegistrarJuego.Evaluar(
            [new FilaRegistroEntrada(true, false, false)],
            1,
            false,
            0,
            [string.Empty],
            [],
            false);
        var cerrado = EvaluacionRegistrarJuego.Evaluar(
            [new FilaRegistroEntrada(true, true, true)],
            1,
            true,
            1_000,
            ["3214"],
            [],
            true);

        parcial.Filas.Should().Be(ParteJuego.Parcial);
        parcial.Numeros.Should().Be(ParteJuego.Parcial);
        parcial.PuedeRegistrar.Should().BeFalse();
        cerrado.PuedeRegistrar.Should().BeFalse();
    }

    [Fact]
    public void Evaluar_rechaza_el_numero_que_no_cabe_en_juego_numeros()
    {
        var corto = EvaluacionRegistrarJuego.Evaluar(
            [],
            1,
            true,
            1_000,
            ["12"],
            [],
            false);
        var restringido = EvaluacionRegistrarJuego.Evaluar(
            [],
            1,
            true,
            1_000,
            ["1234"],
            ["1234"],
            false);
        var cortoDeDinero = EvaluacionRegistrarJuego.Evaluar(
            [],
            2,
            true,
            3,
            ["1111", "2222"],
            [],
            false);

        corto.Numeros.Should().Be(ParteJuego.Parcial);
        corto.Motivo.Should().Be(MotivoJuegoNumeros.Formato);
        corto.Detalle.Should().Be("12");
        restringido.Motivo.Should().Be(MotivoJuegoNumeros.Restringido);
        cortoDeDinero.Motivo.Should().Be(MotivoJuegoNumeros.Insuficiente);
        cortoDeDinero.Apuestas.Should().Be(4);
    }
}
