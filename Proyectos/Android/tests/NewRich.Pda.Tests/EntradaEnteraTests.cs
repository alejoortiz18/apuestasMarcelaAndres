using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class EntradaEnteraTests
{
    [Fact]
    public void SoloDigitos_quita_decimales_y_separadores()
    {
        EntradaEntera.SoloDigitos("12.50").Should().Be("1250");
        EntradaEntera.SoloDigitos("1.000").Should().Be("1000");
        EntradaEntera.SoloDigitos("12a34").Should().Be("1234");
        EntradaEntera.SoloDigitos(null).Should().BeEmpty();
    }
}
