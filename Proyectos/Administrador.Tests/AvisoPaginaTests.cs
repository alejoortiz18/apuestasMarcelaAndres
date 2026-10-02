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
    public void Usa_los_errores_del_modelo_cuando_no_hay_popup()
    {
        var vista = Vista();
        vista.ModelState.AddModelError(string.Empty, ConfiguracionMessages.HorarioPdaViolaLoterias);
        vista.ModelState.AddModelError("Form.HoraCierre", "campo");

        AvisoPagina.Texto(vista, Temp()).Should().Be(ConfiguracionMessages.HorarioPdaViolaLoterias);
    }

    [Fact]
    public void El_layout_abre_el_dialogo_con_el_aviso_de_la_pagina()
    {
        var layout = File.ReadAllText(Ruta("Administrador", "Views", "Shared", "_Layout.cshtml"));
        layout.Should().Contain("AvisoPagina.Texto");
        layout.Should().Contain("id=\"avisoDialog\"");
        var login = File.ReadAllText(Ruta("Administrador", "Views", "Shared", "_LoginLayout.cshtml"));
        login.Should().Contain("AvisoPagina.Texto");
        login.Should().Contain("id=\"avisoDialog\"");
        var config = File.ReadAllText(Ruta("Administrador", "Views", "Configuracion", "Index.cshtml"));
        config.Should().NotContain("asp-validation-summary");
        config.Should().NotContain("role=\"alert\">@Model.AvisoVersiones");
        config.Should().Contain("ViewData[\"AvisoPagina\"]");
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
                texto.Should().Contain("asp-validation-summary");
                continue;
            }

            texto.Should().NotContain("asp-validation-summary", nombre);
            if (nombre is "Ingresar.cshtml" or "CambiarPassword.cshtml" or "Llave.cshtml")
            {
                texto.Should().NotContain("class=\"notice error\"", nombre);
                texto.Should().Contain("ViewData[\"AvisoPagina\"]", nombre);
            }
        }

        var sitio = File.ReadAllText(Ruta("Administrador", "wwwroot", "js", "site.js"));
        sitio.Should().Contain("window.openAviso");
        var pda = File.ReadAllText(Ruta("Administrador", "wwwroot", "js", "registro-pda.js"));
        pda.Should().Contain("window.openAviso");
        pda.Should().NotContain("errorVerificacion");
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
