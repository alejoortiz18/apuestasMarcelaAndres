using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NewRich.Admin.Controllers;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Recaudo;

namespace NewRich.Admin.Tests;

public sealed class ConfigRecaudoPorcentajesTests
{
    [Fact]
    public async Task Guardar_porcentajes_envia_todos_los_grupos_en_una_sola_llamada()
    {
        var norte = Guid.NewGuid();
        var sur = Guid.NewGuid();
        ActualizarPorcentajesGruposRecaudoRequest? enviado = null;
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.ActualizarPorcentajesGruposRecaudoAsync(It.IsAny<ActualizarPorcentajesGruposRecaudoRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ActualizarPorcentajesGruposRecaudoRequest, CancellationToken>((r, _) => enviado = r)
            .ReturnsAsync(ApiCallResult<object>.Ok(new object(), "Listo."));
        var sut = Crear(api.Object);

        var resultado = await sut.GuardarPorcentajesGrupos(
            [
                new PorcentajeGrupoRecaudoRequest { GrupoId = norte, Porcentaje = 25 },
                new PorcentajeGrupoRecaudoRequest { GrupoId = sur, Porcentaje = 40 }
            ],
            "2026-09-28",
            "2026-09-28",
            CancellationToken.None);

        api.Verify(x => x.ActualizarPorcentajesGruposRecaudoAsync(It.IsAny<ActualizarPorcentajesGruposRecaudoRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        enviado!.Grupos.Should().BeEquivalentTo(
        [
            new PorcentajeGrupoRecaudoRequest { GrupoId = norte, Porcentaje = 25 },
            new PorcentajeGrupoRecaudoRequest { GrupoId = sur, Porcentaje = 40 }
        ]);
        var redireccion = resultado.Should().BeOfType<RedirectToActionResult>().Subject;
        redireccion.ActionName.Should().Be("Index");
        sut.TempData["FlashOk"].Should().Be("Listo.");
    }

    [Fact]
    public async Task Guardar_porcentajes_muestra_el_error_de_la_api()
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.ActualizarPorcentajesGruposRecaudoAsync(It.IsAny<ActualizarPorcentajesGruposRecaudoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<object>.Fail("El porcentaje debe estar entre 1 y 100.", 400));
        var sut = Crear(api.Object);

        await sut.GuardarPorcentajesGrupos(
            [new PorcentajeGrupoRecaudoRequest { GrupoId = Guid.NewGuid(), Porcentaje = 0 }],
            null,
            null,
            CancellationToken.None);

        sut.TempData["FlashError"].Should().Be("El porcentaje debe estar entre 1 y 100.");
    }

    [Fact]
    public void La_tabla_de_grupos_tiene_editar_guardar_y_cancelar_junto_a_retirar()
    {
        var vista = File.ReadAllText(RutaVista());
        var panel = vista.IndexOf("data-table-panel=\"grupos-recaudo\"", StringComparison.Ordinal);
        var cierrePanel = vista.IndexOf("data-asignar-grupos", StringComparison.Ordinal);
        var acciones = vista[panel..cierrePanel];

        acciones.Should().Contain("UiTexts.RetirarAsignacion");
        acciones.Should().Contain("data-editar-porcentajes");
        acciones.Should().Contain("data-guardar-porcentajes");
        acciones.Should().Contain("data-cancelar-porcentajes");
        acciones.Should().Contain("asp-action=\"GuardarPorcentajesGrupos\"");
        vista.Should().Contain("name=\"grupos[@i].Porcentaje\"");
        vista.Should().Contain("name=\"grupos[@i].GrupoId\"");
        vista.Should().NotContain("AsignaRecaudadorParaPorcentaje");
    }

    [Fact]
    public void El_campo_de_porcentaje_muestra_el_valor_actual_y_admite_vacio_o_cero()
    {
        var vista = File.ReadAllText(RutaVista());
        var inicio = vista.IndexOf("name=\"grupos[@i].Porcentaje\"", StringComparison.Ordinal);
        var apertura = vista.LastIndexOf("<input", inicio, StringComparison.Ordinal);
        var cierre = vista.IndexOf("/>", inicio, StringComparison.Ordinal);
        var campo = vista[apertura..cierre];

        campo.Should().Contain("value=\"@fila.Porcentaje\"");
        campo.Should().Contain("min=\"0\"");
        campo.Should().Contain("max=\"100\"");
        campo.Should().NotContain("required");
    }

    private static ConfigRecaudoController Crear(IAdminApiClient api)
    {
        var http = new DefaultHttpContext();
        return new ConfigRecaudoController(api)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        };
    }

    private static string RutaVista()
    {
        var ruta = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Administrador", "Views", "ConfigRecaudo", "Index.cshtml"));
        File.Exists(ruta).Should().BeTrue($"se esperaba la vista en {ruta}");
        return ruta;
    }
}
