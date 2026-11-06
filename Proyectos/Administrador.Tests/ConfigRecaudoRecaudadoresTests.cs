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

public sealed class ConfigRecaudoRecaudadoresTests
{
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
                new AsignacionRecaudadorGrupo { GrupoId = norte, RecaudadorId = recardo, RecaudadorActualId = null },
                new AsignacionRecaudadorGrupo { GrupoId = sur, RecaudadorId = null, RecaudadorActualId = recardo },
                new AsignacionRecaudadorGrupo { GrupoId = oeste, RecaudadorId = otro, RecaudadorActualId = otro }
            ],
            "2026-09-28",
            "2026-09-28",
            CancellationToken.None);

        enviado!.GrupoId.Should().Be(norte);
        enviado.RecaudadorId.Should().Be(recardo);
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
            [new AsignacionRecaudadorGrupo { GrupoId = grupo, RecaudadorId = recaudador, RecaudadorActualId = recaudador }],
            null,
            null,
            CancellationToken.None);

        api.Verify(x => x.AsignarGrupoRecaudoAsync(It.IsAny<AsignarGrupoRecaudoRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        api.Verify(x => x.RetirarGrupoRecaudoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        sut.TempData["AvisoModal"].Should().Be(UiTexts.EligeRecaudadorParaGuardar);
    }

    [Fact]
    public void Los_grupos_ya_no_tienen_porcentaje_colectivo()
    {
        var vista = File.ReadAllText(RutaVista());

        vista.Should().NotContain("data-editar-porcentajes");
        vista.Should().NotContain("GuardarPorcentajesGrupos");
        vista.Should().NotContain("name=\"porcentaje\"");
        vista.Should().NotContain(".Porcentaje");
        vista.Should().Contain("data-guardar-recaudadores");
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
