using FluentAssertions;
using NewRich.Application.Services;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class QrDesdeFotoTests
{
    [Fact]
    public void Lee_el_contenido_de_un_qr_generado()
    {
        var contenido = "AOL-6661571";
        var png = QrImagen.Png(contenido);

        QrDesdeFoto.Leer(png).Should().Be(contenido);
    }

    [Fact]
    public void Devuelve_nulo_si_la_imagen_no_tiene_qr()
    {
        QrDesdeFoto.Leer([1, 2, 3, 4]).Should().BeNull();
    }
}
