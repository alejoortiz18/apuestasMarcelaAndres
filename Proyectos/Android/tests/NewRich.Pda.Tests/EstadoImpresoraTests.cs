using FluentAssertions;
using NewRich.Pda.Core;
using Xunit;

namespace NewRich.Pda.Tests;

public class EstadoImpresoraTests
{
    [Fact]
    public void Mientras_imprime_avisa_que_va_a_la_impresora()
    {
        EstadoImpresora.Aviso(null).Should().Be(PdaTexts.ImprimiendoTirilla);
        EstadoImpresora.EsError(null).Should().BeFalse();
    }

    [Fact]
    public void Cuando_imprime_confirma_y_orienta_a_reportar()
    {
        EstadoImpresora.Aviso(true).Should().Be(PdaTexts.TirillaImpresa);
        EstadoImpresora.EsError(true).Should().BeFalse();
        PdaTexts.TirillaImpresa.Should().Contain("papel");
        PdaTexts.TirillaImpresa.Should().Contain(PdaTexts.Reportar);
        PdaTexts.TirillaImpresa.Should().NotContain("Reimprimir");
    }

    [Fact]
    public void Cuando_falla_muestra_el_error_de_impresion()
    {
        EstadoImpresora.Aviso(false).Should().Be(PdaTexts.ErrorImpresion);
        EstadoImpresora.EsError(false).Should().BeTrue();
    }
}
