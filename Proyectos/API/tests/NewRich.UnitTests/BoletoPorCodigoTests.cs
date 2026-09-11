using FluentAssertions;
using NewRich.Application.Services;

namespace NewRich.UnitTests;

public sealed class BoletoPorCodigoTests
{
    [Theory]
    [InlineData("6661571", "6661571")]
    [InlineData("AOL-6661571", "6661571")]
    [InlineData("aol-6661571", "6661571")]
    [InlineData("  AOL-6661571  ", "6661571")]
    [InlineData("4839190", "4839190")]
    [InlineData("AOL-4839190", "4839190")]
    public void Extrae_el_codigo_publico_del_formato_impreso(string entrada, string esperado)
    {
        BoletoPorCodigo.Normalizar(entrada).Should().Be(esperado);
    }

    [Fact]
    public void Conserva_el_payload_del_qr_cuando_no_es_codigo_impreso()
    {
        var qr = "1.8f2c1a6e4b094d739e215a7c0b8d3f14.y8VAbWKe6RRrnWQg.cipher.tag";

        BoletoPorCodigo.Normalizar(qr).Should().Be(qr);
    }
}
