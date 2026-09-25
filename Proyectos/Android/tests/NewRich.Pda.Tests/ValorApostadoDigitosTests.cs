using FluentAssertions;
using NewRich.Pda.Core.Ventas;

namespace NewRich.Pda.Tests;

public sealed class ValorApostadoDigitosTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("0", "")]
    [InlineData("00", "")]
    [InlineData("0123", "123")]
    [InlineData("00056", "56")]
    public void Filtrar_impide_cero_como_primer_digito(string? texto, string esperado)
    {
        ValorApostadoDigitos.Filtrar(texto).Should().Be(esperado);
    }

    [Theory]
    [InlineData("1", "1")]
    [InlineData("12345", "12345")]
    [InlineData("123456", "12345")]
    [InlineData("234567", "23456")]
    [InlineData("5", "5")]
    [InlineData("56789", "56789")]
    [InlineData("567890", "56789")]
    public void Filtrar_con_primer_digito_1_a_5_permite_maximo_5(string texto, string esperado)
    {
        ValorApostadoDigitos.Filtrar(texto).Should().Be(esperado);
    }

    [Theory]
    [InlineData("6", "6")]
    [InlineData("6789", "6789")]
    [InlineData("67890", "6789")]
    [InlineData("7", "7")]
    [InlineData("7890", "7890")]
    [InlineData("78901", "7890")]
    [InlineData("9", "9")]
    [InlineData("9012", "9012")]
    [InlineData("90123", "9012")]
    public void Filtrar_con_primer_digito_6_a_9_permite_maximo_4(string texto, string esperado)
    {
        ValorApostadoDigitos.Filtrar(texto).Should().Be(esperado);
    }

    [Fact]
    public void Filtrar_quita_caracteres_no_numericos()
    {
        ValorApostadoDigitos.Filtrar("1a2b3").Should().Be("123");
        ValorApostadoDigitos.Filtrar("6.789").Should().Be("6789");
    }
}
