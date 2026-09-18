using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class Hora12Tests
{
    [Theory]
    [InlineData("10:00 AM", "10:00:00")]
    [InlineData("10:00 PM", "22:00:00")]
    [InlineData("12:00 AM", "00:00:00")]
    [InlineData("12:30 PM", "12:30:00")]
    [InlineData("18:45", "18:45:00")]
    public void Parsea_am_pm_y_tambien_formato_24h(string texto, string esperado)
    {
        Hora12.TryParse(texto, out var hora).Should().BeTrue();
        hora.Should().Be(TimeSpan.Parse(esperado));
    }

    [Fact]
    public void Formatea_en_12_horas_con_am_pm()
    {
        Hora12.Formatear(TimeSpan.Parse("10:00:00")).Should().Be("10:00 AM");
        Hora12.Formatear(TimeSpan.Parse("20:00:00")).Should().Be("8:00 PM");
    }
}
