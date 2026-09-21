using FluentAssertions;

namespace NewRich.Admin.Tests;

public sealed class ChatSoporteVistaTests
{
    [Fact]
    public void El_chat_admin_no_muestra_buscador_ni_limpiar_filtros()
    {
        var vista = File.ReadAllText(RutaVista());

        vista.Should().NotContain("chat-search");
        vista.Should().NotContain("BuscarChat");
        vista.Should().NotContain("LimpiarFiltros");
        vista.Should().NotContain("asp-route-q");
        vista.Should().NotContain("Busqueda");
    }

    [Fact]
    public void El_inicio_de_conversacion_no_quita_alto_del_hilo()
    {
        var vista = File.ReadAllText(RutaVista());
        var pagina = vista.IndexOf("page-soporte", StringComparison.Ordinal);
        var inicio = vista.IndexOf("IniciarConversacion", StringComparison.Ordinal);
        var shell = vista.IndexOf("chat-shell", StringComparison.Ordinal);

        pagina.Should().BePositive();
        inicio.Should().BePositive();
        inicio.Should().BeGreaterThan(pagina);
        inicio.Should().BeLessThan(shell);
        vista.Should().NotContain("class=\"heading\"");
    }

    private static string RutaVista()
    {
        var ruta = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Administrador", "Views", "Soporte", "Index.cshtml"));
        File.Exists(ruta).Should().BeTrue($"se esperaba la vista en {ruta}");
        return ruta;
    }
}
