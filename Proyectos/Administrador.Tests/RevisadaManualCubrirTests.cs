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
    public async Task Cubrir_desde_resumen_envia_el_grupo_sin_comision_ni_descuento_general()
    {
        CubrirDesdeResumenRevisadaManualRequest? enviado = null;
        var api = Api();
        api.Setup(a => a.RevisadaCubrirDesdeResumenAsync(It.IsAny<CubrirDesdeResumenRevisadaManualRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CubrirDesdeResumenRevisadaManualRequest, CancellationToken>((r, _) => enviado = r)
            .ReturnsAsync(ApiCallResult<GrupoRevisadaManualResponse>.Ok(new(), "El grupo quedó cubierto."));
        var sut = Crear(api.Object);

        var result = await sut.Cubrir(["fila:1", "fila:2"], new DateTime(2026, 10, 21), new DateTime(2026, 10, 21), cancellationToken: CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Resumen");
        enviado!.Claves.Should().Equal("fila:1", "fila:2");
        enviado.ComisionPorcentaje.Should().Be(0m);
        enviado.DescuentoPorcentaje.Should().Be(0m);
        api.Verify(a => a.ParametrosRevisadaManualAsync(It.IsAny<CancellationToken>()), Times.Never);
        sut.TempData["AvisoModal"].Should().Be("El grupo quedó cubierto.");
    }

    [Fact]
    public void La_vista_de_cubierto_ya_no_tiene_comision_ni_descuento()
    {
        var vista = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "Administrador", "Views", "RevisadaManual", "_Contenido.cshtml")));

        vista.Should().NotContain("GuardarPorcentajes");
        vista.Should().NotContain("name=\"comision\"");
        vista.Should().NotContain("name=\"descuento\"");
    }

    [Fact]
    public async Task Cubierto_con_rango_que_incluye_hoy_muestra_lo_cubierto_hoy_y_el_historico()
    {
        var api = Api();
        ConCubierto(api);
        var sut = Crear(api.Object);

        var result = await sut.Cubierto(DateTime.Today.AddDays(-3), DateTime.Today, cancellationToken: CancellationToken.None);

        var modelo = result.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<RevisadaPaginaViewModel>().Subject;
        modelo.IncluyeHoy.Should().BeTrue();
        modelo.Historico.Should().NotBeNull();
        api.Verify(a => a.RevisadaCubiertoHoyAsync(It.IsAny<CancellationToken>()), Times.Once);
        api.Verify(a => a.RevisadaCubiertoHistoricoAsync(DateTime.Today.AddDays(-3), DateTime.Today, "fecha", true, 1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cubierto_con_rango_sin_hoy_muestra_solo_el_historico()
    {
        var api = Api();
        ConCubierto(api);
        var sut = Crear(api.Object);

        var result = await sut.Cubierto(DateTime.Today.AddDays(-10), DateTime.Today.AddDays(-5), cancellationToken: CancellationToken.None);

        result.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<RevisadaPaginaViewModel>().Which.IncluyeHoy.Should().BeFalse();
        api.Verify(a => a.RevisadaCubiertoHoyAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Resumen_con_dias_anteriores_y_hoy_muestra_lo_de_hoy_y_el_historico_hasta_ayer()
    {
        var api = Api();
        ConResumenHistorico(api);
        var sut = Crear(api.Object);

        var result = await sut.Resumen(DateTime.Today.AddDays(-5), DateTime.Today, null, cancellationToken: CancellationToken.None);

        var modelo = result.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<RevisadaPaginaViewModel>().Subject;
        modelo.IncluyeHoy.Should().BeTrue();
        modelo.HistoricoResumen.Should().NotBeNull();
        api.Verify(a => a.RevisadaResumenAsync(DateTime.Today, DateTime.Today.AddDays(1).AddTicks(-1), null, It.IsAny<CancellationToken>()), Times.Once);
        api.Verify(a => a.RevisadaResumenHistoricoAsync(DateTime.Today.AddDays(-5), DateTime.Today.AddDays(-1), "fecha", true, 1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Resumen_solo_de_hoy_no_muestra_historico()
    {
        var api = Api();
        ConResumenHistorico(api);
        var sut = Crear(api.Object);

        var result = await sut.Resumen(DateTime.Today, DateTime.Today, null, cancellationToken: CancellationToken.None);

        result.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<RevisadaPaginaViewModel>().Which.HistoricoResumen.Should().BeNull();
        api.Verify(a => a.RevisadaResumenHistoricoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Resumen_solo_de_dias_anteriores_muestra_solo_el_historico()
    {
        var api = Api();
        ConResumenHistorico(api);
        var sut = Crear(api.Object);

        var result = await sut.Resumen(DateTime.Today.AddDays(-7), DateTime.Today.AddDays(-2), null, cancellationToken: CancellationToken.None);

        var modelo = result.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<RevisadaPaginaViewModel>().Subject;
        modelo.IncluyeHoy.Should().BeFalse();
        modelo.HistoricoResumen.Should().NotBeNull();
        api.Verify(a => a.RevisadaResumenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<decimal?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static void ConResumenHistorico(Mock<IAdminApiClient> api)
    {
        api.Setup(a => a.RevisadaResumenHistoricoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<ResumenHistoricoResponse>.Ok(new(), "Listo."));
    }

    private static void ConCubierto(Mock<IAdminApiClient> api)
    {
        api.Setup(a => a.RevisadaCubiertoHoyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<VistaRevisadaManualResponse>.Ok(new(), "Listo."));
        api.Setup(a => a.RevisadaCubiertoHistoricoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<CubiertoHistoricoResponse>.Ok(new(), "Listo."));
    }

    [Fact]
    public async Task Recubrir_envia_las_claves_sin_comision_ni_descuento_general()
    {
        RecubrirRevisadaManualRequest? enviado = null;
        var api = Api();
        api.Setup(a => a.RevisadaRecubrirAsync(It.IsAny<RecubrirRevisadaManualRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RecubrirRevisadaManualRequest, CancellationToken>((r, _) => enviado = r)
            .ReturnsAsync(ApiCallResult<VistaRevisadaManualResponse>.Ok(new() { Jugadas = 2, Grupos = [new(), new()] }, "Los números quedaron cubiertos."));
        var sut = Crear(api.Object);

        var result = await sut.Recubrir(["fila:1", "fila:2"], new DateTime(2026, 10, 21), new DateTime(2026, 10, 21), cancellationToken: CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Resumen");
        enviado!.Claves.Should().Equal("fila:1", "fila:2");
        enviado.ComisionPorcentaje.Should().Be(0m);
        enviado.DescuentoPorcentaje.Should().Be(0m);
        sut.TempData["AvisoModal"].Should().Be("Los números quedaron cubiertos. Números: 2. Registros: 2.");
    }

    private static Mock<IAdminApiClient> Api()
    {
        var api = new Mock<IAdminApiClient>();
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
