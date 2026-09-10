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
}
