using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class LectorCodigoBarrasTests
{
    [Theory]
    [InlineData("scannerdata", "AOL-6661571")]
    [InlineData("barcode_string", "AOL-6661571")]
    [InlineData("data", "1.qr.payload")]
    [InlineData("result", "NR1.abcDEFghij")]
    [InlineData("codedContent", "AOL-6661571")]
    [InlineData("SCAN_RESULT", "0000153")]
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
    public void Extrae_el_codigo_desde_bytes_utf8()
    {
        var texto = "1.8f2c1a6e4b094d739e215a7c0b8d3f14.abc.def.ghi";
        LectorCodigoBarras.TextoDe(System.Text.Encoding.UTF8.GetBytes(texto)).Should().Be(texto);
    }

    [Fact]
    public void Ignora_el_tostring_de_un_arreglo_de_bytes()
    {
        LectorCodigoBarras.TextoDe("[B@1a2b3c").Should().BeNull();
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

    [Fact]
    public void No_toma_el_formato_qr_code_como_si_fuera_el_ticket()
    {
        var extras = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["SCAN_RESULT_FORMAT"] = "QR_CODE",
            ["SCAN_RESULT_ORIENTATION"] = "90",
            ["SCAN_RESULT"] = "  "
        };

        LectorCodigoBarras.CodigoDe(extras).Should().BeNull();
    }

    [Fact]
    public void Prefiere_el_payload_largo_del_qr_sobre_el_formato()
    {
        var extras = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["SCAN_RESULT_FORMAT"] = "QR_CODE",
            ["SCAN_RESULT"] = "NR2.OFF-000018.a1b2c3"
        };

        LectorCodigoBarras.CodigoDe(extras).Should().Be("NR2.OFF-000018.a1b2c3");
    }

    [Theory]
    [InlineData("barcode1", "OFF-000018")]
    [InlineData("barcode_value", "NR3.ABCDEFGHIJKLMN")]
    [InlineData("scanner_data", "{\"consecutivo\":\"OFF-000018\"}")]
    public void Extrae_claves_del_lector_del_pda(string clave, string valor)
    {
        var extras = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [clave] = valor
        };

        LectorCodigoBarras.CodigoDe(extras).Should().Be(valor);
    }
}
