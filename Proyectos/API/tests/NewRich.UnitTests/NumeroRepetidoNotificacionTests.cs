using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class NumeroRepetidoNotificacionTests
{
    [Fact]
    public void Extraer_toma_el_numero_de_cuatro_digitos_del_aviso()
    {
        var numero = NumeroRepetidoNotificacion.Extraer("El número 2684 superó el umbral de repeticiones configurado.");

        numero.Should().Be("2684");
    }

    [Fact]
    public void Extraer_conserva_los_ceros_a_la_izquierda()
    {
        var numero = NumeroRepetidoNotificacion.Extraer("El número 0042 superó el umbral de repeticiones configurado.");

        numero.Should().Be("0042");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("El número superó el umbral de repeticiones configurado.")]
    [InlineData("El número 268 superó el umbral.")]
    public void Extraer_sin_numero_de_cuatro_digitos_devuelve_nulo(string? mensaje)
    {
        NumeroRepetidoNotificacion.Extraer(mensaje).Should().BeNull();
    }
}
