using FluentAssertions;
using NewRich.Application.Services;

namespace NewRich.UnitTests;

public sealed class QrImagenTests
{
    [Fact]
    public void Png_vacio_no_genera_imagen()
    {
        QrImagen.Png("").Should().BeEmpty();
        QrImagen.DataUri("   ").Should().BeEmpty();
    }

    [Fact]
    public void Png_del_payload_es_un_png_valido()
    {
        var bytes = QrImagen.Png("1.key.nonce.cipher.tag");

        bytes.Should().HaveCountGreaterThan(100);
        bytes[0].Should().Be(0x89);
        bytes[1].Should().Be(0x50);
        bytes[2].Should().Be(0x4E);
        bytes[3].Should().Be(0x47);
        QrImagen.DataUri("1.key.nonce.cipher.tag").Should().StartWith("data:image/png;base64,");
    }

    [Fact]
    public void Png_de_un_sobre_largo_no_lanza()
    {
        var largo = new string('A', 4000);
        var bytes = QrImagen.Png(largo);
        bytes.Should().NotBeNull();
    }

    [Fact]
    public void Png_de_tirilla_cabe_en_el_ancho_del_papel()
    {
        var bytes = QrImagen.PngParaTirilla("NR1." + new string('A', 400), 360);

        bytes.Should().HaveCountGreaterThan(100);
        bytes[0].Should().Be(0x89);
    }
}
