using FluentAssertions;

namespace NewRich.Admin.Tests;

public sealed class CubiertoPdfOmitirHoyTests
{
    [Fact]
    public void El_pdf_de_cubierto_omite_las_tarjetas_de_hoy_y_totales()
    {
        var vista = File.ReadAllText(Ruta("Views", "RevisadaManual", "_Contenido.cshtml"));
        var columnas = Indice(vista, "class=\"revisada-columnas\"");
        var totales = Indice(vista, "revisada-totales");
        var pager = vista.LastIndexOf("@await Html.PartialAsync(\"_Pager\", Model.Pagina)", StringComparison.Ordinal);

        columnas.Should().BeGreaterThan(0);
        totales.Should().BeGreaterThan(columnas);
        pager.Should().BeGreaterThan(totales);

        BloqueAtributos(vista, columnas).Should().Contain("data-pdf-omitir");
        BloqueAtributos(vista, totales).Should().Contain("data-pdf-omitir");
        var inicioPager = Math.Max(0, pager - 40);
        vista.Substring(inicioPager, Math.Min(160, vista.Length - inicioPager)).Should().Contain("data-pdf-omitir");
    }

    private static int Indice(string texto, string marca)
    {
        var i = texto.IndexOf(marca, StringComparison.Ordinal);
        i.Should().BeGreaterThanOrEqualTo(0);
        return i;
    }

    private static string BloqueAtributos(string texto, int indiceMarca)
    {
        var inicio = texto.LastIndexOf('<', indiceMarca);
        inicio.Should().BeGreaterThanOrEqualTo(0);
        var fin = texto.IndexOf('>', indiceMarca);
        fin.Should().BeGreaterThan(inicio);
        return texto[inicio..(fin + 1)];
    }

    private static string Ruta(params string[] partes)
    {
        return Path.GetFullPath(Path.Combine(
            [AppContext.BaseDirectory, "..", "..", "..", "..", "Administrador", ..partes]));
    }
}
