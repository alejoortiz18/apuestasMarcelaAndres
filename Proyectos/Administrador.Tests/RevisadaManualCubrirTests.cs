using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NewRich.Admin.Constants;
using NewRich.Admin.Controllers;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.RevisadaManual;

namespace NewRich.Admin.Tests;

public sealed class RevisadaManualCubrirTests
{
    [Fact]
    public async Task Cubrir_desde_resumen_sin_comision_configurada_avisa_y_no_envia_nada()
    {
        var api = Api(comision: null);
        var sut = Crear(api.Object);

        await sut.Cubrir(["fila:1"], new DateTime(2026, 10, 21), new DateTime(2026, 10, 21), cancellationToken: CancellationToken.None);

        sut.TempData["AvisoModal"].Should().Be(UiTexts.ComisionSinConfigurar);
        api.Verify(a => a.RevisadaCubrirDesdeResumenAsync(It.IsAny<CubrirDesdeResumenRevisadaManualRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cubrir_desde_resumen_envia_el_grupo_con_la_comision_guardada()
    {
        CubrirDesdeResumenRevisadaManualRequest? enviado = null;
        var api = Api(comision: 30m);
        api.Setup(a => a.RevisadaCubrirDesdeResumenAsync(It.IsAny<CubrirDesdeResumenRevisadaManualRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CubrirDesdeResumenRevisadaManualRequest, CancellationToken>((r, _) => enviado = r)
            .ReturnsAsync(ApiCallResult<GrupoRevisadaManualResponse>.Ok(new(), "El grupo quedó cubierto."));
        var sut = Crear(api.Object);

        var result = await sut.Cubrir(["fila:1", "fila:2"], new DateTime(2026, 10, 21), new DateTime(2026, 10, 21), cancellationToken: CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Resumen");
        enviado!.Claves.Should().Equal("fila:1", "fila:2");
        enviado.ComisionPorcentaje.Should().Be(30m);
        sut.TempData["AvisoModal"].Should().Be("El grupo quedó cubierto.");
    }

    private static Mock<IAdminApiClient> Api(decimal? comision)
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(a => a.ParametrosRevisadaManualAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<ParametrosRevisadaManualResponse>.Ok(new() { ComisionPorcentaje = comision }, "Listo."));
        api.Setup(a => a.RevisadaResumenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<decimal?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<VistaRevisadaManualResponse>.Ok(new(), "Listo."));
        return api;
    }

    private static RevisadaManualController Crear(IAdminApiClient api)
    {
        var http = new DefaultHttpContext();
        return new RevisadaManualController(api)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        };
    }
}
