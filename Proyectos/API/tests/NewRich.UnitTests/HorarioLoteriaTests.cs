using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class HorarioLoteriaTests
{
    private static readonly TimeSpan AperturaPda = TimeSpan.Parse("08:00:00");
    private static readonly TimeSpan CierrePda = TimeSpan.Parse("18:00:00");

    [Fact]
    public void Acepta_horario_dentro_de_la_ventana_del_pda()
    {
        HorarioLoteria.EsValido(
            TimeSpan.Parse("09:00:00"),
            TimeSpan.Parse("11:00:00"),
            AperturaPda,
            CierrePda).Should().BeTrue();
    }

    [Theory]
    [InlineData("07:00:00", "11:00:00")]
    [InlineData("09:00:00", "19:00:00")]
    [InlineData("07:00:00", "19:00:00")]
    [InlineData("19:00:00", "20:00:00")]
    [InlineData("06:00:00", "07:00:00")]
    public void Rechaza_horario_fuera_de_la_ventana_del_pda(string inicio, string fin)
    {
        HorarioLoteria.EsValido(
            TimeSpan.Parse(inicio),
            TimeSpan.Parse(fin),
            AperturaPda,
            CierrePda).Should().BeFalse();
    }

    [Fact]
    public void Rechaza_cuando_el_inicio_no_es_menor_que_el_fin()
    {
        HorarioLoteria.EsValido(
            TimeSpan.Parse("14:00:00"),
            TimeSpan.Parse("10:00:00"),
            AperturaPda,
            CierrePda).Should().BeFalse();
        HorarioLoteria.EsValido(
            TimeSpan.Parse("11:00:00"),
            TimeSpan.Parse("11:00:00"),
            AperturaPda,
            CierrePda).Should().BeFalse();
    }

    [Theory]
    [InlineData("09:00:00", true)]
    [InlineData("11:00:00", true)]
    [InlineData("08:59:59", false)]
    [InlineData("11:00:01", false)]
    public void Esta_vigente_entre_inicio_y_fin_inclusive(string ahora, bool vigente)
    {
        HorarioLoteria.EstaVigente(
            TimeSpan.Parse(ahora),
            TimeSpan.Parse("09:00:00"),
            TimeSpan.Parse("11:00:00")).Should().Be(vigente);
    }
}
