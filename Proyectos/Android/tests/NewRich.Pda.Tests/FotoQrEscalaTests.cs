using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class FotoQrEscalaTests
{
    [Fact]
    public void No_reduce_una_foto_que_ya_es_pequena()
    {
        FotoQrEscala.Muestra(1280, 960).Should().Be(1);
        FotoQrEscala.Excede(1280, 960).Should().BeFalse();
    }

    [Theory]
    [InlineData(4160, 3120, 4)]
    [InlineData(3264, 2448, 4)]
    [InlineData(2560, 1920, 2)]
    [InlineData(1920, 1080, 2)]
    public void Reduce_las_fotos_grandes_a_potencias_de_dos(int ancho, int alto, int esperado)
    {
        FotoQrEscala.Muestra(ancho, alto).Should().Be(esperado);
        FotoQrEscala.Excede(ancho, alto).Should().BeTrue();
    }

    [Fact]
    public void La_foto_reducida_queda_dentro_del_lado_objetivo()
    {
        var muestra = FotoQrEscala.Muestra(4160, 3120);

        (4160 / muestra).Should().BeLessThanOrEqualTo(FotoQrEscala.LadoObjetivo);
        (4160 / muestra).Should().BeGreaterThan(FotoQrEscala.LadoObjetivo / 2);
    }

    [Fact]
    public void Es_segura_con_medidas_invalidas()
    {
        FotoQrEscala.Muestra(0, 0).Should().Be(1);
        FotoQrEscala.Muestra(-10, 500).Should().Be(1);
        FotoQrEscala.Excede(0, 0).Should().BeFalse();
    }
}
