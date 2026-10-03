using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NewRich.Admin.Services;
using NewRich.Constants.Messages;

namespace NewRich.Admin.Tests;

public sealed class AvisoPaginaTests
{
    [Fact]
    public void Prefiere_el_aviso_modal_sobre_el_estado_del_modelo()
    {
        var temp = Temp();
        temp["AvisoModal"] = "Guardado";
        var vista = Vista();
        vista.ModelState.AddModelError(string.Empty, ConfiguracionMessages.HorarioPdaViolaLoterias);

        AvisoPagina.Texto(vista, temp).Should().Be("Guardado");
    }

    [Fact]
    public void Muestra_el_aviso_de_ingreso_cuando_vence_la_sesion()
    {
        var temp = Temp();
        temp["AvisoIngreso"] = "Tu sesión venció.";

        AvisoPagina.Texto(Vista(), temp).Should().Be("Tu sesión venció.");
    }

    [Fact]
    public void Usa_el_aviso_de_la_vista_antes_que_los_errores_del_modelo()
    {
        var vista = Vista();
        vista["AvisoPagina"] = "Usuario o contraseña incorrectos.";
        vista.ModelState.AddModelError(string.Empty, ConfiguracionMessages.HorarioPdaViolaLoterias);

        AvisoPagina.Texto(vista, Temp()).Should().Be("Usuario o contraseña incorrectos.");
    }

    [Fact]
    public void Usa_solo_los_errores_generales_del_modelo_y_no_los_de_cada_campo()
    {
        var vista = Vista();
        vista.ModelState.AddModelError(string.Empty, ConfiguracionMessages.HorarioPdaViolaLoterias);
        vista.ModelState.AddModelError("Form.HoraCierre", "campo");

        AvisoPagina.Texto(vista, Temp()).Should().Be(ConfiguracionMessages.HorarioPdaViolaLoterias);
    }

    [Fact]
    public void Sin_avisos_no_abre_nada()
    {
        AvisoPagina.Texto(Vista(), Temp()).Should().BeNull();
    }

    [Fact]
    public void Los_dos_layouts_abren_el_dialogo_con_el_aviso_de_la_pagina()
    {
        foreach (var nombre in new[] { "_Layout.cshtml", "_LoginLayout.cshtml" })
        {
            var layout = File.ReadAllText(Ruta("Administrador", "Views", "Shared", nombre));
            layout.Should().Contain("AvisoPagina.Texto(ViewData, TempData)", nombre);
            layout.Should().Contain("id=\"avisoDialog\"", nombre);
            layout.Should().Contain("@UiTexts.Aceptar", nombre);
            layout.Should().NotContain("FlashOk", nombre);
            layout.Should().NotContain("FlashError", nombre);
        }

        File.ReadAllText(Ruta("Administrador", "Views", "Shared", "_LoginLayout.cshtml"))
            .Should().Contain("~/js/site.js");
    }

    [Fact]
    public void Las_vistas_no_ponen_franjas_de_aviso_en_la_pagina()
    {
        var vistas = Directory.GetFiles(Ruta("Administrador", "Views"), "*.cshtml", SearchOption.AllDirectories);
        foreach (var archivo in vistas)
        {
            var texto = File.ReadAllText(archivo);
            var nombre = Path.GetFileName(archivo);
            if (nombre == "_FormModal.cshtml")
            {
                continue;
            }

            texto.Should().NotContain("asp-validation-summary", nombre);
            if (nombre is "Ingresar.cshtml" or "CambiarPassword.cshtml" or "Llave.cshtml")
            {
                texto.Should().NotContain("class=\"notice error\"", nombre);
                texto.Should().Contain("ViewData[\"AvisoPagina\"]", nombre);
            }
        }

        var config = File.ReadAllText(Ruta("Administrador", "Views", "Configuracion", "Index.cshtml"));
        config.Should().NotContain("role=\"alert\">@Model.AvisoVersiones");
        config.Should().Contain("ViewData[\"AvisoPagina\"] = Model.AvisoVersiones");
        var registro = File.ReadAllText(Ruta("Administrador", "Views", "Dispositivos", "Crear.cshtml"));
        registro.Should().NotContain("data-verificacion-error");
        registro.Should().NotContain("data-registro-error");
    }

    [Fact]
    public void Los_scripts_avisan_los_errores_en_el_dialogo()
    {
        File.ReadAllText(Ruta("Administrador", "wwwroot", "js", "site.js")).Should().Contain("window.openAviso = openAviso");
        var pda = File.ReadAllText(Ruta("Administrador", "wwwroot", "js", "registro-pda.js"));
        pda.Should().Contain("window.openAviso");
        pda.Should().NotContain("errorVerificacion");
        pda.Should().NotContain("errorRegistro");
    }

    private static ViewDataDictionary Vista() =>
        new(new EmptyModelMetadataProvider(), new ModelStateDictionary());

    private static TempDataDictionary Temp() =>
        new(new DefaultHttpContext(), new MemoriaTemp());

    private static string Ruta(params string[] partes) => Path.GetFullPath(Path.Combine(
        new[] { AppContext.BaseDirectory, "..", "..", "..", ".." }.Concat(partes).ToArray()));

    private sealed class MemoriaTemp : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();

        public void SaveTempData(HttpContext context, IDictionary<string, object?> values)
        {
        }
    }
}
