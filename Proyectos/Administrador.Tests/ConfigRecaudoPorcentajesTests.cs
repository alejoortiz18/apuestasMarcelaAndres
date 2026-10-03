using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NewRich.Admin.Constants;
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
        sut.TempData["AvisoModal"].Should().Be("Listo.");
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

        sut.TempData["AvisoModal"].Should().Be("El porcentaje debe estar entre 1 y 100.");
    }

    [Fact]
    public async Task Guardar_recaudadores_asigna_y_retira_solo_los_grupos_cambiados()
    {
        var norte = Guid.NewGuid();
        var sur = Guid.NewGuid();
        var oeste = Guid.NewGuid();
        var recardo = Guid.NewGuid();
        var otro = Guid.NewGuid();
        AsignarGrupoRecaudoRequest? enviado = null;
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.AsignarGrupoRecaudoAsync(It.IsAny<AsignarGrupoRecaudoRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AsignarGrupoRecaudoRequest, CancellationToken>((r, _) => enviado = r)
            .ReturnsAsync(ApiCallResult<object>.Ok(new object(), "Listo."));
        api.Setup(x => x.RetirarGrupoRecaudoAsync(sur, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<object>.Ok(new object(), "Listo."));
        var sut = Crear(api.Object);

        var resultado = await sut.GuardarRecaudadoresGrupos(
            [
                new AsignacionRecaudadorGrupo { GrupoId = norte, RecaudadorId = recardo, RecaudadorActualId = null, Porcentaje = 20 },
                new AsignacionRecaudadorGrupo { GrupoId = sur, RecaudadorId = null, RecaudadorActualId = recardo, Porcentaje = 30 },
                new AsignacionRecaudadorGrupo { GrupoId = oeste, RecaudadorId = otro, RecaudadorActualId = otro, Porcentaje = 40 }
            ],
            "2026-09-28",
            "2026-09-28",
            CancellationToken.None);

        enviado!.GrupoId.Should().Be(norte);
        enviado.RecaudadorId.Should().Be(recardo);
        enviado.Porcentaje.Should().Be(20);
        api.Verify(x => x.AsignarGrupoRecaudoAsync(It.IsAny<AsignarGrupoRecaudoRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        api.Verify(x => x.RetirarGrupoRecaudoAsync(sur, It.IsAny<CancellationToken>()), Times.Once);
        api.Verify(x => x.RetirarGrupoRecaudoAsync(oeste, It.IsAny<CancellationToken>()), Times.Never);
        resultado.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Index");
        sut.TempData.Should().ContainKey("AvisoModal");
    }

    [Fact]
    public async Task Guardar_recaudadores_sin_cambios_no_llama_la_api()
    {
        var grupo = Guid.NewGuid();
        var recaudador = Guid.NewGuid();
        var api = new Mock<IAdminApiClient>();
        var sut = Crear(api.Object);

        await sut.GuardarRecaudadoresGrupos(
            [new AsignacionRecaudadorGrupo { GrupoId = grupo, RecaudadorId = recaudador, RecaudadorActualId = recaudador, Porcentaje = 10 }],
            null,
            null,
            CancellationToken.None);

        api.Verify(x => x.AsignarGrupoRecaudoAsync(It.IsAny<AsignarGrupoRecaudoRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        api.Verify(x => x.RetirarGrupoRecaudoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        sut.TempData["AvisoModal"].Should().Be(UiTexts.EligeRecaudadorParaGuardar);
    }

    [Fact]
    public void La_tabla_de_grupos_tiene_editar_guardar_y_cancelar_sin_retirar_asignacion()
    {
        var vista = File.ReadAllText(RutaVista());
        var panel = vista.IndexOf("data-table-panel=\"grupos-recaudo\"", StringComparison.Ordinal);
        var cierrePanel = vista.IndexOf("data-asignar-grupos", StringComparison.Ordinal);
        var acciones = vista[panel..cierrePanel];

        acciones.Should().NotContain("UiTexts.RetirarAsignacion");
        acciones.Should().NotContain("RetirarGrupo");
        acciones.Should().Contain("data-editar-porcentajes");
        acciones.Should().Contain("data-guardar-recaudadores");
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
