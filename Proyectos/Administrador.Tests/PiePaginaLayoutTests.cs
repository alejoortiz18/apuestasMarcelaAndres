using FluentAssertions;

namespace NewRich.Admin.Tests;

public sealed class PiePaginaLayoutTests
{
    [Fact]
    public void El_bloque_general_de_contenido_no_encoge_el_pie_sobre_las_filas()
    {
        var css = File.ReadAllText(RutaCss());
        var general = Bloque(css, ".content {");
        var chat = Bloque(css, ".content:has(.page-soporte) {");

        general.Should().NotBeNullOrWhiteSpace();
        general.Should().NotContain("min-height: 0");
        general.Should().Contain("flex: 1 0 auto");

        chat.Should().Contain("min-height: 0");
        chat.Should().Contain("overflow: hidden");
    }

    [Fact]
    public void El_pie_queda_en_el_flujo_del_layout_sin_superponerse()
    {
        var css = File.ReadAllText(RutaCss());
        var pie = Bloque(css, ".app-foot {");

        pie.Should().Contain("flex-shrink: 0");
        pie.Should().NotContain("position: absolute");
        pie.Should().NotContain("position: fixed");
    }

    private static string Bloque(string css, string selector)
    {
        var inicio = css.IndexOf(selector, StringComparison.Ordinal);
        inicio.Should().BeGreaterThanOrEqualTo(0, $"se esperaba {selector}");
        var llave = css.IndexOf('{', inicio);
        var cierre = css.IndexOf('}', llave);
        return css[llave..(cierre + 1)];
    }

    private static string RutaCss()
    {
        var ruta = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Administrador", "wwwroot", "css", "site.css"));
        File.Exists(ruta).Should().BeTrue($"se esperaba el css en {ruta}");
        return ruta;
    }
}
