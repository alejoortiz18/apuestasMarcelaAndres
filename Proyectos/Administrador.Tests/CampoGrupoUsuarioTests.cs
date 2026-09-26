using FluentAssertions;

namespace NewRich.Admin.Tests;

public sealed class CampoGrupoUsuarioTests
{
    [Fact]
    public void El_campo_oculto_de_grupo_no_queda_visible_por_el_grid_del_formulario()
    {
        var css = File.ReadAllText(Ruta("wwwroot", "css", "site.css"));

        css.Should().Contain(".field[hidden]");
        var oculto = Bloque(css, ".field[hidden]");
        oculto.Should().Contain("display: none");
    }

    [Fact]
    public void Al_elegir_un_rol_sin_grupo_el_script_limpia_y_deshabilita_el_select()
    {
        var script = File.ReadAllText(Ruta("wwwroot", "js", "site.js"));
        var sincronizar = Funcion(script, "function sincronizar()");

        sincronizar.Should().Contain("select.disabled");
        sincronizar.Should().Contain("select.value = \"\"");
    }

    [Fact]
    public void El_formulario_de_alta_solo_pide_grupo_al_vendedor()
    {
        var vista = File.ReadAllText(Ruta("Views", "Usuarios", "_CamposUsuario.cshtml"));

        vista.Should().Contain("data-usuario-grupo=\"@((int)RolUsuario.Vendedor)\"");
        vista.Should().Contain("hidden=\"@(Model.Rol != RolUsuario.Vendedor)\"");
    }

    [Fact]
    public void El_formulario_completo_tambien_sincroniza_el_campo_grupo()
    {
        var script = File.ReadAllText(Ruta("wwwroot", "js", "site.js"));
        var vista = File.ReadAllText(Ruta("Views", "Usuarios", "Form.cshtml"));

        script.Should().Contain("querySelectorAll(\"[data-usuario-form]\")");
        vista.Should().Contain("data-usuario-form");
    }

    private static string Funcion(string fuente, string firma)
    {
        var inicio = fuente.IndexOf(firma, StringComparison.Ordinal);
        inicio.Should().BeGreaterThanOrEqualTo(0, $"se esperaba {firma}");
        var llave = fuente.IndexOf('{', inicio);
        var nivel = 0;
        for (var i = llave; i < fuente.Length; i++)
        {
            if (fuente[i] == '{')
            {
                nivel++;
            }
            else if (fuente[i] == '}')
            {
                nivel--;
                if (nivel == 0)
                {
                    return fuente[llave..(i + 1)];
                }
            }
        }

        return string.Empty;
    }

    private static string Bloque(string css, string selector)
    {
        var inicio = css.IndexOf(selector, StringComparison.Ordinal);
        inicio.Should().BeGreaterThanOrEqualTo(0, $"se esperaba {selector}");
        var llave = css.IndexOf('{', inicio);
        var cierre = css.IndexOf('}', llave);
        return css[llave..(cierre + 1)];
    }

    private static string Ruta(params string[] partes)
    {
        var ruta = Path.GetFullPath(Path.Combine(
            [AppContext.BaseDirectory, "..", "..", "..", "..", "Administrador", .. partes]));
        File.Exists(ruta).Should().BeTrue($"se esperaba el archivo en {ruta}");
        return ruta;
    }
}
