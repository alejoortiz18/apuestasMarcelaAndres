using FluentAssertions;
using NewRich.Admin.Constants;

namespace NewRich.Admin.Tests;

public sealed class LoteriasVistaTests
{
    [Fact]
    public void El_resumen_por_loteria_ordena_cada_columna_de_datos()
    {
        var vista = File.ReadAllText(RutaVista());
        var tabla = vista[vista.IndexOf("<table>", StringComparison.Ordinal)..];
        var encabezado = tabla[..tabla.IndexOf("</thead>", StringComparison.Ordinal)];

        encabezado.Should().Contain("table-sort");
        encabezado.Should().Contain("UrlOrden(\"nombre\")");
        encabezado.Should().Contain("UrlOrden(\"numero\")");
        encabezado.Should().Contain("UrlOrden(\"horacierre\")");
        encabezado.Should().Contain("UrlOrden(\"boletos\")");
        encabezado.Should().Contain("UrlOrden(\"total\")");
        encabezado.Should().Contain("UrlOrden(\"tipo\")");
        encabezado.Should().Contain("UrlOrden(\"estado\")");
        encabezado.Should().Contain("MarcaOrden");
        encabezado.IndexOf("UiTexts.Accion", StringComparison.Ordinal)
            .Should().BeGreaterThan(encabezado.LastIndexOf("table-sort", StringComparison.Ordinal));
        UiTexts.OrdenarColumnaAsc.Should().Contain("Ordenar");
        var cuerpo = tabla[tabla.IndexOf("<tbody>", StringComparison.Ordinal)..];
        cuerpo.Should().Contain("item.HoraFin");
        cuerpo.Should().NotContain("item.HoraCierre");
    }

    private static string RutaVista() => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..",
        "Administrador", "Views", "Loterias", "Index.cshtml"));
}
