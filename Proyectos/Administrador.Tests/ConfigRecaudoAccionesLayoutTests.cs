using FluentAssertions;

namespace NewRich.Admin.Tests;

public sealed class ConfigRecaudoAccionesLayoutTests
{
    [Fact]
    public void La_columna_acciones_mantiene_ver_y_registrar_pago_en_una_sola_linea()
    {
        var css = File.ReadAllText(Ruta("wwwroot", "css", "site.css"));
        var vista = File.ReadAllText(Ruta("Views", "ConfigRecaudo", "Index.cshtml"));
        var bloque = css[css.IndexOf(".recaudo-col-acciones", StringComparison.Ordinal)..];

        bloque.Should().Contain("flex-wrap: nowrap");
        bloque.Should().Contain("white-space: nowrap");
        bloque.Should().Contain("min-width:");
        vista.Should().Contain("recaudo-col-acciones");
        vista.Should().Contain("UiTexts.Ver");
        vista.Should().Contain("UiTexts.RegistrarPago");
        vista.Should().Contain("class=\"table-wrap\"");
        var indiceTabla = vista.IndexOf("data-table-select=\"grupos-recaudo\"", StringComparison.Ordinal);
        indiceTabla.Should().BeGreaterThanOrEqualTo(0);
        var inicioBloque = Math.Max(0, indiceTabla - 80);
        var tablaGrupos = vista.Substring(inicioBloque, Math.Min(160, vista.Length - inicioBloque));
        tablaGrupos.Should().NotContain("table-wrap-dropdown");
    }

    private static string Ruta(params string[] partes)
    {
        return Path.GetFullPath(Path.Combine(
            [AppContext.BaseDirectory, "..", "..", "..", "..", "Administrador", ..partes]));
    }
}
