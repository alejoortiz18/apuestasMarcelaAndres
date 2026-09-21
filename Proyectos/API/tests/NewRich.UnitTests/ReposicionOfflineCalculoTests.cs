using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class ReposicionOfflineCalculoTests
{
    [Theory]
    [InlineData(30, 25, 5)]
    [InlineData(30, 30, 0)]
    [InlineData(40, 45, 0)]
    [InlineData(20, 0, 20)]
    [InlineData(0, 10, 0)]
    public void CantidadAReponer_nivela_al_maximo_sin_invalidar_excedentes(
        int maximo,
        int disponibles,
        int esperado)
    {
        ReposicionOfflineCalculo.CantidadAReponer(maximo, disponibles).Should().Be(esperado);
    }

    [Fact]
    public void MarcaDiaria_usa_la_fecha_local_en_formato_corto()
    {
        ReposicionOfflineCalculo.MarcaDiaria(new DateTime(2026, 9, 20, 18, 45, 0)).Should().Be("2026-09-20");
    }

    [Theory]
    [InlineData(2026, 1, 1)]
    [InlineData(2026, 9, 20)]
    [InlineData(2099, 12, 31)]
    public void MarcaDiaria_cabe_en_la_columna_Resultado(int anio, int mes, int dia)
    {
        ReposicionOfflineCalculo.MarcaDiaria(new DateTime(anio, mes, dia))
            .Length.Should().BeLessThanOrEqualTo(ReposicionOfflineCalculo.LargoMaximoMarca);
    }
}
