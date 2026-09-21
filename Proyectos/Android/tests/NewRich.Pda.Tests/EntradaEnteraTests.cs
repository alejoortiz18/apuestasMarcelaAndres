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

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("1", "1")]
    [InlineData("12", "12")]
    [InlineData("123", "123")]
    [InlineData("1234", "1.234")]
    [InlineData("1250000", "1.250.000")]
    [InlineData("1.250.000", "1.250.000")]
    [InlineData("1250abc000", "1.250.000")]
    [InlineData("000", "0")]
    public void ConPuntosDeMil_agrupa_mientras_se_digita(string? texto, string esperado)
    {
        EntradaEntera.ConPuntosDeMil(texto).Should().Be(esperado);
    }

    [Fact]
    public void LeerMonto_quita_los_puntos_y_devuelve_el_entero()
    {
        EntradaEntera.LeerMonto("1.250.000").Should().Be(1_250_000m);
        EntradaEntera.LeerMonto("1.234").Should().Be(1234m);
        EntradaEntera.LeerMonto("").Should().BeNull();
        EntradaEntera.LeerMonto("abc").Should().BeNull();
    }

    [Fact]
    public void El_valor_que_viaja_al_servidor_es_el_entero_sin_puntos()
    {
        var enPantalla = EntradaEntera.ConPuntosDeMil("1250000");
        enPantalla.Should().Be("1.250.000");
        EntradaEntera.LeerMonto(enPantalla).Should().Be(1_250_000m);
    }
}
