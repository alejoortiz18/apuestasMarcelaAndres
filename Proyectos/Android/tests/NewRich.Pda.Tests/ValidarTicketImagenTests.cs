using FluentAssertions;
using NewRich.Application.Services;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class ValidarTicketImagenTests
{
    [Fact]
    public void Lee_el_qr_de_la_foto_y_deja_el_codigo_para_consultar()
    {
        var png = QrImagen.Png("NR3.ABCDEFG234567");

        var codigo = ValidarTicketImagen.CodigoDe(png, out var error);

        codigo.Should().Be("NR3.ABCDEFG234567");
        error.Should().BeNull();
    }

    [Fact]
    public void Avisa_si_la_foto_no_tiene_qr()
    {
        var codigo = ValidarTicketImagen.CodigoDe([1, 2, 3, 4], out var error);

        codigo.Should().BeNull();
        error.Should().Be(PdaTexts.QrNoEncontradoEnFoto);
    }
}
