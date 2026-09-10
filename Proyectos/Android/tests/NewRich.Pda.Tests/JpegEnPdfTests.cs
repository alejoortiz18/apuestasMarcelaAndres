using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class JpegEnPdfTests
{
    [Fact]
    public void Crear_envuelve_un_jpeg_en_pdf()
    {
        var jpeg = Convert.FromBase64String(
            "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAP//////////////////////////////////////////////////////////////////////////////////////2wBDAf//////////////////////////////////////////////////////////////////////////////////////wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAb/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAAX/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIQAxAAAAGfAD//2Q==");

        var pdf = JpegEnPdf.Crear(jpeg, 1, 1);

        System.Text.Encoding.ASCII.GetString(pdf[..5]).Should().Be("%PDF-");
        pdf.Length.Should().BeGreaterThan(jpeg.Length);
        System.Text.Encoding.ASCII.GetString(pdf).Should().Contain("/DCTDecode");
    }
}
