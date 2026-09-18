using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class HorarioOperacionTests
{
    [Theory]
    [InlineData("09:59:00", true)]
    [InlineData("10:00:00", true)]
    [InlineData("10:00:01", false)]
    [InlineData("20:00:00", false)]
    [InlineData("20:00:01", true)]
    public void Mismo_dia_abre_despues_de_la_apertura_y_cierra_despues_del_cierre(string ahora, bool fuera)
    {
        var resultado = HorarioOperacion.EstaFuera(
            TimeSpan.Parse(ahora),
            TimeSpan.Parse("10:00:00"),
            TimeSpan.Parse("20:00:00"));

        resultado.Should().Be(fuera);
    }

    [Theory]
    [InlineData("21:59:00", true)]
    [InlineData("22:00:00", true)]
    [InlineData("22:00:01", false)]
    [InlineData("23:30:00", false)]
    [InlineData("05:00:00", false)]
    [InlineData("06:00:00", false)]
    [InlineData("06:00:01", true)]
    [InlineData("12:00:00", true)]
    public void Horario_que_cruza_medianoche_permite_noche_y_madrugada(string ahora, bool fuera)
    {
        var resultado = HorarioOperacion.EstaFuera(
            TimeSpan.Parse(ahora),
            TimeSpan.Parse("22:00:00"),
            TimeSpan.Parse("06:00:00"));

        resultado.Should().Be(fuera);
    }

    [Fact]
    public void Horas_iguales_no_son_validas_para_configurar()
    {
        HorarioOperacion.SonDistintas(TimeSpan.Parse("10:00:00"), TimeSpan.Parse("10:00:00"))
            .Should().BeFalse();
        HorarioOperacion.SonDistintas(TimeSpan.Parse("10:00:00"), TimeSpan.Parse("20:00:00"))
            .Should().BeTrue();
    }
}
