using FluentAssertions;
using NewRich.Admin.Constants;

namespace NewRich.Admin.Tests;

public sealed class ConfigRecaudoVistaTests
{
    [Fact]
    public void Cada_fila_de_grupos_tiene_el_boton_ver_al_final()
    {
        var vista = File.ReadAllText(RutaVista("Index.cshtml"));

        vista.Should().Contain("UiTexts.Ver");
        vista.Should().Contain("asp-action=\"Ver\"");
        vista.Should().Contain("asp-route-id=\"@fila.GrupoId\"");
        var acciones = vista.IndexOf(">@UiTexts.Acciones</th>", StringComparison.Ordinal);
        var recaudador = vista.IndexOf(">@UiTexts.Recaudador</th>", StringComparison.Ordinal);
        acciones.Should().BeGreaterThan(recaudador);
    }

    [Fact]
    public void La_vista_de_integrantes_lista_a_cada_vendedor_del_grupo()
    {
        var vista = File.ReadAllText(RutaVista("Ver.cshtml"));

        vista.Should().Contain("UiTexts.IntegrantesDelGrupo");
        vista.Should().Contain("fila.Nombre");
        vista.Should().Contain("fila.TotalVendido");
        vista.Should().Contain("fila.ValorACobrar");
        vista.Should().Contain("fila.TotalPendiente");
        vista.Should().Contain("asp-action=\"Index\"");
    }

    private static string RutaVista(string archivo)
    {
        var ruta = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Administrador", "Views", "ConfigRecaudo", archivo));
        File.Exists(ruta).Should().BeTrue($"se esperaba la vista en {ruta}");
        return ruta;
    }
}
