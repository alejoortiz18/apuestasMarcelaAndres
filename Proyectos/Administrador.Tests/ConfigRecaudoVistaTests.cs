using FluentAssertions;
using NewRich.Admin.Constants;

namespace NewRich.Admin.Tests;

public sealed class ConfigRecaudoVistaTests
{
    [Fact]
    public void La_columna_recaudador_es_una_lista_con_buscador_que_no_guarda_al_cambiar()
    {
        var vista = File.ReadAllText(RutaVista("Index.cshtml"));
        var tabla = vista.IndexOf("data-table-select=\"grupos-recaudo\"", StringComparison.Ordinal);
        var panel = vista.IndexOf("data-table-panel=\"grupos-recaudo\"", StringComparison.Ordinal);
        var grupos = vista[tabla..panel];

        grupos.Should().NotContain("asp-action=\"ActualizarRecaudadorGrupo\"");
        grupos.Should().Contain("class=\"searchable\"");
        grupos.Should().Contain("data-recaudador-grupo");
        grupos.Should().Contain("data-recaudador-inicial");
        grupos.Should().Contain("UiTexts.SinAsignar");
        grupos.Should().Contain("name=\"asignaciones[@i].RecaudadorId\"");
        grupos.Should().Contain("name=\"asignaciones[@i].GrupoId\"");
        grupos.Should().Contain("name=\"asignaciones[@i].Porcentaje\"");
        grupos.Should().Contain("name=\"asignaciones[@i].RecaudadorActualId\"");
        grupos.Should().Contain("fila.RecaudadorId == item.Id");
        grupos.Should().NotContain("@fila.RecaudadorNombre");
        var celda = grupos[grupos.IndexOf("recaudo-col-recaudador", StringComparison.Ordinal)..grupos.IndexOf("table-ver", StringComparison.Ordinal)];
        celda.Should().NotContain("disabled");
        vista[..tabla].Should().Contain("table-wrap-dropdown");
    }

    [Fact]
    public void Guardar_recaudadores_esta_junto_a_editar_porcentajes_deshabilitado_y_pide_contrasena()
    {
        var vista = File.ReadAllText(RutaVista("Index.cshtml"));
        var panel = vista.IndexOf("data-table-panel=\"grupos-recaudo\"", StringComparison.Ordinal);
        var acciones = vista[panel..vista.IndexOf("data-asignar-grupos", StringComparison.Ordinal)];
        var editar = acciones.IndexOf("data-editar-porcentajes", StringComparison.Ordinal);
        var guardar = acciones.IndexOf("data-guardar-recaudadores", StringComparison.Ordinal);

        guardar.Should().BeGreaterThan(editar);
        acciones.Should().Contain("asp-action=\"GuardarRecaudadoresGrupos\"");
        acciones.Should().Contain("data-protected-action=\"@AccionesProtegidas.GruposAsignar\"");
        acciones.Should().Contain("UiTexts.Guardar");
        acciones.Should().Contain("disabled");
        var script = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "Administrador", "wwwroot", "js", "site.js")));
        script.Should().Contain("data-guardar-recaudadores");
        script.Should().NotContain("select.form.submit()");
    }

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
