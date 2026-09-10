using FluentAssertions;
using NewRich.Application.Chat;
using NewRich.Constants.Messages;

namespace NewRich.UnitTests;

public sealed class ChatAdjuntoTests
{
    [Theory]
    [InlineData("foto.jpg")]
    [InlineData("foto.JPEG")]
    [InlineData("captura.png")]
    [InlineData("a.webp")]
    [InlineData("manual.pdf")]
    public void Permite_imagenes_y_pdf(string nombre)
    {
        ChatAdjunto.EsPermitido(nombre).Should().BeTrue();
    }

    [Theory]
    [InlineData("virus.exe")]
    [InlineData("nota.txt")]
    [InlineData("video.mp4")]
    [InlineData("")]
    public void Rechaza_otros_tipos(string nombre)
    {
        ChatAdjunto.EsPermitido(nombre).Should().BeFalse();
    }

    [Fact]
    public void Validar_rechaza_si_pesa_mas_de_cinco_megabytes()
    {
        var bytes = new byte[ChatAdjunto.TamanoMaximoBytes + 1];

        var result = ChatAdjunto.Validar("foto.png", bytes);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(ChatMessages.AdjuntoDemasiadoGrande);
    }

    [Fact]
    public void EsImagen_distingue_pdf()
    {
        ChatAdjunto.EsImagen("a.png").Should().BeTrue();
        ChatAdjunto.EsPdf("manual.PDF").Should().BeTrue();
        ChatAdjunto.EsImagen("manual.pdf").Should().BeFalse();
    }
}
