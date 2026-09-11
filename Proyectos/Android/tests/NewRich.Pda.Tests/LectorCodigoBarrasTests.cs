using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class LectorCodigoBarrasTests
{
    [Theory]
    [InlineData("scannerdata", "AOL-6661571")]
    [InlineData("barcode_string", "AOL-6661571")]
    [InlineData("data", "1.qr.payload")]
    [InlineData("value", "4839190")]
    [InlineData("barcode", "4839190")]
    public void Extrae_el_codigo_de_los_extras_del_lector(string clave, string valor)
    {
        var extras = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [clave] = valor
        };

        LectorCodigoBarras.CodigoDe(extras).Should().Be(valor);
    }

    [Fact]
    public void Ignora_extras_vacios()
    {
        var extras = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["scannerdata"] = "  "
        };

        LectorCodigoBarras.CodigoDe(extras).Should().BeNull();
    }
}
