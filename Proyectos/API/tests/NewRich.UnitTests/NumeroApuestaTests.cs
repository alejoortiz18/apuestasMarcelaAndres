using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class NumeroApuestaTests
{
    [Theory]
    [InlineData("123")]
    [InlineData("012")]
    [InlineData("1234")]
    [InlineData("0001")]
    public void Acepta_tres_o_cuatro_digitos(string numero)
    {
        NumeroApuesta.EsValido(numero).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("12345")]
    [InlineData("12a")]
    [InlineData("12 3")]
    public void Rechaza_otro_formato(string numero)
    {
        NumeroApuesta.EsValido(numero).Should().BeFalse();
    }
}
