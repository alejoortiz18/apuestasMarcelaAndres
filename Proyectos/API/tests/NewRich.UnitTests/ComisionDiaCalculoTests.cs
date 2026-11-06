using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class ComisionDiaCalculoTests
{
    [Fact]
    public void Resta_el_porcentaje_del_vendedor_y_luego_el_del_recaudador()
    {
        var resultado = ComisionDiaCalculo.Calcular(100_000m, 10, 5);

        resultado.ComisionVendedor.Should().Be(10_000m);
        resultado.EntregarAlRecaudador.Should().Be(90_000m);
        resultado.ComisionRecaudador.Should().Be(4_500m);
        resultado.EntregarAAdministracion.Should().Be(85_500m);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(10.5)]
    public void Rechaza_porcentajes_que_no_son_enteros_entre_0_y_100(decimal porcentaje)
    {
        ComisionDiaCalculo.PorcentajeValido(porcentaje).Should().BeFalse();
    }
}
