using FluentAssertions;
using NewRich.Admin.Constants;
using NewRich.Admin.Services;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Tests;

public sealed class UsuarioVendedorWebTests
{
    [Fact]
    public void El_rol_vendedor_web_se_muestra_con_nombre_legible()
    {
        RolUi.Nombre(RolUsuario.VendedorWeb).Should().Be(UiTexts.RolVendedorWeb);
        UiTexts.RolVendedorWeb.Should().Be("Vendedor web");
        RolUi.Nombre(RolUsuario.Vendedor).Should().Be("Vendedor");
    }

    [Fact]
    public void Las_vistas_de_usuarios_muestran_el_rol_con_su_nombre_legible()
    {
        foreach (var vista in new[] { "_CamposUsuario.cshtml", "Index.cshtml", "Detalle.cshtml", "_DetalleModal.cshtml" })
        {
            File.ReadAllText(Ruta("Views", "Usuarios", vista)).Should().Contain("RolUi.Nombre(", vista);
        }
    }

    [Fact]
    public void El_formulario_oculta_el_pda_cuando_el_rol_es_vendedor_web()
    {
        var vista = File.ReadAllText(Ruta("Views", "Usuarios", "_CamposUsuario.cshtml"));

        vista.Should().Contain("data-usuario-sin-dispositivo=\"@((int)RolUsuario.VendedorWeb)\"");
        vista.Should().Contain("hidden=\"@(RolConsola.SoloSitioVendedor(Model.Rol))\"");
        vista.Should().Contain("UiTexts.VendedorWebSoloSitio");
    }

    [Fact]
    public void El_script_limpia_y_deshabilita_el_pda_del_vendedor_web()
    {
        var script = File.ReadAllText(Ruta("wwwroot", "js", "site.js"));

        script.Should().Contain("[data-usuario-sin-dispositivo]");
        script.Should().Contain("function bindUsuarioDispositivo(form)");
        script.Should().Contain("bindUsuarioDispositivo(form);");
    }

    [Fact]
    public void Asociar_pda_no_ofrece_vendedores_web()
    {
        var vista = File.ReadAllText(Ruta("Views", "Dispositivos", "Index.cshtml"));

        vista.Should().Contain("!RolConsola.SoloSitioVendedor(x.Rol)");
    }

    private static string Ruta(params string[] partes)
    {
        var ruta = Path.GetFullPath(Path.Combine(
            [AppContext.BaseDirectory, "..", "..", "..", "..", "Administrador", .. partes]));
        File.Exists(ruta).Should().BeTrue($"se esperaba el archivo en {ruta}");
        return ruta;
    }
}
