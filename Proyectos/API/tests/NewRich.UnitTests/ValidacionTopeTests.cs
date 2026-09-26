using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class ValidacionTopeTests
{
    [Fact]
    public void El_juego_cabe_si_acumulado_mas_valor_no_supera_el_tope()
    {
        ValidacionTope.PuedeJugar(50_000m, 15_000m, 35_000m).Should().BeTrue();
        ValidacionTope.Disponible(50_000m, 15_000m).Should().Be(35_000m);
    }

    [Fact]
    public void Exactamente_el_tope_esta_permitido()
    {
        ValidacionTope.PuedeJugar(50_000m, 40_000m, 10_000m).Should().BeTrue();
        ValidacionTope.Disponible(50_000m, 50_000m).Should().Be(0m);
    }

    [Fact]
    public void Superar_el_tope_usa_la_plantilla_configurada()
    {
        var topes = new[] { new ValidacionTope.TopeLoteria(Id("a"), "Medellín", 50_000m) };
        var acumulados = new[] { new ValidacionTope.Acumulado(Id("a"), "1234", 48_000m) };
        var aportes = new[] { new ValidacionTope.Aporte(Id("a"), "Medellín", "1234", 5_000m) };

        var resultado = ValidacionTope.Evaluar(
            aportes,
            topes,
            acumulados,
            "Tope en {loteria} número {numero}. Ingresó {valorIngresado}. Quedan {valorDisponible}.");

        resultado.Ok.Should().BeFalse();
        resultado.Mensaje.Should().Be("Tope en Medellín número 1234. Ingresó 5.000. Quedan 2.000.");
    }

    [Fact]
    public void Superar_el_tope_sin_plantilla_usa_el_mensaje_por_defecto()
    {
        ValidacionTope.PlantillaSuperacionDefecto.Should().Contain("{numero}");
        ValidacionTope.PlantillaSuperacionDefecto.Should().Contain("{loteria}");
        ValidacionTope.PlantillaSuperacionDefecto.Should().Contain("{valorIngresado}");
        ValidacionTope.PlantillaSuperacionDefecto.Should().Contain("{valorDisponible}");
    }

    [Fact]
    public void Superar_el_tope_no_esta_permitido()
    {
        ValidacionTope.PuedeJugar(50_000m, 50_000m, 2_000m).Should().BeFalse();
        ValidacionTope.PuedeJugar(50_000m, 45_000m, 10_000m).Should().BeFalse();
        ValidacionTope.Disponible(50_000m, 45_000m).Should().Be(5_000m);
    }

    [Fact]
    public void Tope_en_cero_impide_usar_la_loteria()
    {
        ValidacionTope.LoteriaJugable(0m).Should().BeFalse();
        ValidacionTope.PuedeJugar(0m, 0m, 1_000m).Should().BeFalse();
        ValidacionTope.Disponible(0m, 0m).Should().Be(0m);
    }

    [Fact]
    public void Tope_positivo_permite_usar_la_loteria()
    {
        ValidacionTope.LoteriaJugable(1_000m).Should().BeTrue();
        ValidacionTope.TopeInicialExistentes.Should().Be(1_000m);
    }

    [Fact]
    public void Varias_loterias_se_validan_por_separado_con_el_mismo_valor()
    {
        var topes = new[]
        {
            new ValidacionTope.TopeLoteria(Id("a"), "Medellín", 50_000m),
            new ValidacionTope.TopeLoteria(Id("b"), "Bogotá", 100_000m)
        };
        var acumulados = new[]
        {
            new ValidacionTope.Acumulado(Id("a"), "1234", 48_000m),
            new ValidacionTope.Acumulado(Id("b"), "1234", 0m)
        };
        var aportes = new[]
        {
            new ValidacionTope.Aporte(Id("a"), "Medellín", "1234", 5_000m),
            new ValidacionTope.Aporte(Id("b"), "Bogotá", "1234", 5_000m)
        };

        var resultado = ValidacionTope.Evaluar(aportes, topes, acumulados);

        resultado.Ok.Should().BeFalse();
        resultado.Disponible.Should().Be(2_000m);
        resultado.Mensaje.Should().Contain("1234");
        resultado.Mensaje.Should().Contain("Medellín");
        resultado.Mensaje.Should().Contain("5.000");
        resultado.Mensaje.Should().Contain("2.000");
    }

    [Fact]
    public void Dentro_del_mismo_boleto_se_suman_aportes_del_mismo_numero_y_loteria()
    {
        var topes = new[] { new ValidacionTope.TopeLoteria(Id("a"), "Medellín", 10_000m) };
        var aportes = new[]
        {
            new ValidacionTope.Aporte(Id("a"), "Medellín", "1234", 6_000m),
            new ValidacionTope.Aporte(Id("a"), "Medellín", "1234", 5_000m)
        };

        var resultado = ValidacionTope.Evaluar(aportes, topes, []);

        resultado.Ok.Should().BeFalse();
        resultado.Disponible.Should().Be(10_000m);
    }

    [Fact]
    public void Cuando_no_queda_disponible_indica_que_alcanzo_el_tope()
    {
        var topes = new[] { new ValidacionTope.TopeLoteria(Id("a"), "Medellín", 50_000m) };
        var acumulados = new[] { new ValidacionTope.Acumulado(Id("a"), "1234", 50_000m) };
        var aportes = new[] { new ValidacionTope.Aporte(Id("a"), "Medellín", "1234", 2_000m) };

        var resultado = ValidacionTope.Evaluar(aportes, topes, acumulados);

        resultado.Ok.Should().BeFalse();
        resultado.Disponible.Should().Be(0m);
        resultado.Mensaje.Should().Contain("alcanzado el tope");
        resultado.Mensaje.Should().Contain("50.000");
    }

    private static Guid Id(string marca) =>
        Guid.Parse(marca switch
        {
            "a" => "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            "b" => "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            _ => throw new ArgumentOutOfRangeException(nameof(marca))
        });
}
