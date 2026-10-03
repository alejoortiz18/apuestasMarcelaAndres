using FluentAssertions;

namespace NewRich.Admin.Tests;

public sealed class RegistroPdaVistaTests
{
    [Fact]
    public void El_dialogo_de_exito_muestra_el_nombre_registrado_resaltado()
    {
        var vista = File.ReadAllText(Ruta("Views", "Dispositivos", "Crear.cshtml"));

        vista.Should().Contain("UiTexts.PdaNombreRegistrado");
        vista.Should().Contain("class=\"registro-nombre\"");
        vista.Should().Contain("data-nombre-registrado");
    }

    [Fact]
    public void El_script_pinta_el_nombre_que_devuelve_el_registro()
    {
        var script = File.ReadAllText(Ruta("wwwroot", "js", "registro-pda.js"));

        script.Should().Contain("[data-nombre-registrado]");
        script.Should().Contain("datos.nombreRegistrado");
    }

    [Fact]
    public void El_nombre_se_resalta_en_negro_mayuscula_y_un_punto_mas_grande()
    {
        var estilos = File.ReadAllText(Ruta("wwwroot", "css", "site.css"));

        var inicio = estilos.IndexOf(".registro-nombre", StringComparison.Ordinal);
        inicio.Should().BeGreaterThan(-1);
        var bloque = estilos[inicio..estilos.IndexOf('}', inicio)];
        bloque.Should().Contain("text-transform: uppercase");
        bloque.Should().Contain("font-weight: 800");
        bloque.Should().Contain("color: #000");
        bloque.Should().Contain("font-size: 14px");
    }

    private static string Ruta(params string[] partes)
    {
        var ruta = Path.GetFullPath(Path.Combine(
            [AppContext.BaseDirectory, "..", "..", "..", "..", "Administrador", .. partes]));
        File.Exists(ruta).Should().BeTrue($"se esperaba el archivo en {ruta}");
        return ruta;
    }
}
