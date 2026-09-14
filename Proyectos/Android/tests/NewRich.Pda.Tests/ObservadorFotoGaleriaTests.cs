using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class ObservadorFotoGaleriaTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(6, 90)]
    [InlineData(3, 180)]
    [InlineData(8, 270)]
    [InlineData(0, 0)]
    [InlineData(99, 0)]
    public void Traduce_la_orientacion_exif_a_grados(int orientacion, int esperado)
    {
        ObservadorFotoGaleria.GradosDeExif(orientacion).Should().Be(esperado);
    }

    [Fact]
    public void El_primer_intento_es_la_foto_completa()
    {
        var primero = ObservadorFotoGaleria.Intentos[0];

        primero.Relativo.Should().Be(1f);
        primero.Escala.Should().Be(1);
    }

    [Fact]
    public void Los_intentos_siguientes_acercan_el_centro_de_la_foto()
    {
        ObservadorFotoGaleria.Intentos.Should().HaveCountGreaterThan(2);
        ObservadorFotoGaleria.Intentos.Skip(1).Should().OnlyContain(i => i.Relativo > 0f && i.Relativo < 1f);
        ObservadorFotoGaleria.Intentos.Skip(1).Should().OnlyContain(i => i.Escala > 1);
        ObservadorFotoGaleria.Intentos.Should().BeInDescendingOrder(i => i.Relativo);
    }

    [Fact]
    public void Recorta_el_centro_de_la_foto()
    {
        var zona = ObservadorFotoGaleria.Zona(1000, 800, 0.5f);

        zona.Ancho.Should().Be(500);
        zona.Alto.Should().Be(400);
        zona.X.Should().Be(250);
        zona.Y.Should().Be(200);
    }

    [Fact]
    public void La_zona_nunca_sale_de_la_foto()
    {
        var zona = ObservadorFotoGaleria.Zona(120, 90, 0.1f);

        zona.X.Should().BeGreaterThanOrEqualTo(0);
        zona.Y.Should().BeGreaterThanOrEqualTo(0);
        (zona.X + zona.Ancho).Should().BeLessThanOrEqualTo(120);
        (zona.Y + zona.Alto).Should().BeLessThanOrEqualTo(90);
        zona.Ancho.Should().BeGreaterThan(0);
        zona.Alto.Should().BeGreaterThan(0);
    }

    [Fact]
    public void No_amplia_mas_alla_del_lado_maximo()
    {
        ObservadorFotoGaleria.EscalaSegura(2000, 1500, 3).Should().Be(1);
        ObservadorFotoGaleria.EscalaSegura(400, 300, 3).Should().Be(3);
        ObservadorFotoGaleria.EscalaSegura(0, 0, 3).Should().Be(1);
    }

    [Fact]
    public void Prefiere_el_jpeg_normalizado_que_sale_de_android()
    {
        var original = new byte[] { 1, 2, 3 };
        var jpeg = new byte[] { 10, 20, 30, 40 };

        ObservadorFotoGaleria.BytesParaLeer(original, jpeg).Should().Equal(jpeg);
    }

    [Fact]
    public void Si_android_no_pudo_normalizar_usa_los_bytes_de_la_galeria()
    {
        var original = new byte[] { 1, 2, 3 };

        ObservadorFotoGaleria.BytesParaLeer(original, []).Should().Equal(original);
        ObservadorFotoGaleria.BytesParaLeer(original, null).Should().Equal(original);
        ObservadorFotoGaleria.BytesParaLeer(null, null).Should().BeEmpty();
    }
}
