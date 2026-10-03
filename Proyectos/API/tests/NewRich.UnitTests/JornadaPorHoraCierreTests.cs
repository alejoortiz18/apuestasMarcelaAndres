using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class JornadaPorHoraCierreTests
{
    [Theory]
    [InlineData("09:00:00", "Mañana")]
    [InlineData("12:00:00", "Mañana")]
    [InlineData("12:00:01", "Tarde")]
    [InlineData("18:00:00", "Tarde")]
    [InlineData("18:00:01", "Noche")]
    [InlineData("22:00:00", "Noche")]
    [InlineData("23:59:00", "Noche")]
    public void Ubica_segun_la_hora_de_cierre(string horaFin, string jornada)
    {
        JornadaPorHoraCierre.Nombre(TimeSpan.Parse(horaFin)).Should().Be(jornada);
    }

    [Theory]
    [InlineData("Mañana", "11:00:00", true)]
    [InlineData("Mañana", "17:00:00", false)]
    [InlineData("Tarde", "17:00:00", true)]
    [InlineData("Tarde", "11:00:00", false)]
    [InlineData("Noche", "22:00:00", true)]
    [InlineData("Noche", "17:00:00", false)]
    [InlineData("Especial", "11:00:00", false)]
    public void La_hora_de_fin_debe_coincidir_con_la_jornada(string jornada, string horaFin, bool coincide)
    {
        JornadaPorHoraCierre.Coincide(jornada, TimeSpan.Parse(horaFin)).Should().Be(coincide);
    }

    [Fact]
    public void El_rango_de_manana_llega_hasta_mediodia()
    {
        JornadaPorHoraCierre.TryRangoHoraFin("Mañana", out var min, out var max).Should().BeTrue();
        min.Should().Be(TimeSpan.Zero);
        max.Should().Be(new TimeSpan(12, 0, 0));
    }

    [Fact]
    public void El_rango_de_tarde_empieza_despues_del_mediodia()
    {
        JornadaPorHoraCierre.TryRangoHoraFin("Tarde", out var min, out var max).Should().BeTrue();
        min.Should().Be(new TimeSpan(12, 1, 0));
        max.Should().Be(new TimeSpan(18, 0, 0));
    }

    [Fact]
    public void El_rango_de_noche_empieza_despues_de_las_seis()
    {
        JornadaPorHoraCierre.TryRangoHoraFin("Noche", out var min, out var max).Should().BeTrue();
        min.Should().Be(new TimeSpan(18, 1, 0));
        max.Should().Be(new TimeSpan(23, 59, 0));
    }

    [Fact]
    public void Una_jornada_desconocida_no_tiene_rango()
    {
        JornadaPorHoraCierre.TryRangoHoraFin("Especial", out _, out _).Should().BeFalse();
    }
}
