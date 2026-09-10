using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class ResultadoImpresionTests
{
    [Fact]
    public void Fallo_no_cierra_la_tirilla_y_pide_pdf()
    {
        var resultado = ResultadoImpresion.Fallo(PdaTexts.ErrorImpresion);

        resultado.Ok.Should().BeFalse();
        resultado.Mensaje.Should().Be(PdaTexts.ErrorImpresion);
        resultado.Mensaje.Should().Contain("PDF");
    }

    [Fact]
    public void Fallo_de_pdf_no_cierra_la_tirilla()
    {
        var resultado = ResultadoImpresion.Fallo(PdaTexts.ErrorPdf);

        resultado.Ok.Should().BeFalse();
        resultado.Mensaje.Should().Be(PdaTexts.ErrorPdf);
    }
}
