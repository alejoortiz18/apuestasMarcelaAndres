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
    public async Task Cubrir_desde_resumen_sin_descuento_guardado_avisa_y_no_envia_nada()
    {
        var api = Api(comision: 20m);
        var sut = Crear(api.Object);

        await sut.Cubrir(["fila:1"], new DateTime(2026, 10, 21), new DateTime(2026, 10, 21), cancellationToken: CancellationToken.None);

        sut.TempData["AvisoModal"].Should().Be(UiTexts.PorcentajesSinConfigurar);
        api.Verify(a => a.RevisadaCubrirDesdeResumenAsync(It.IsAny<CubrirDesdeResumenRevisadaManualRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cubrir_desde_resumen_envia_el_grupo_con_la_comision_y_el_descuento_guardados()
    {
        CubrirDesdeResumenRevisadaManualRequest? enviado = null;
        var api = Api(comision: 30m, descuento: 10m);
        api.Setup(a => a.RevisadaCubrirDesdeResumenAsync(It.IsAny<CubrirDesdeResumenRevisadaManualRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CubrirDesdeResumenRevisadaManualRequest, CancellationToken>((r, _) => enviado = r)
            .ReturnsAsync(ApiCallResult<GrupoRevisadaManualResponse>.Ok(new(), "El grupo quedó cubierto."));
        var sut = Crear(api.Object);

        var result = await sut.Cubrir(["fila:1", "fila:2"], new DateTime(2026, 10, 21), new DateTime(2026, 10, 21), cancellationToken: CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Resumen");
        enviado!.Claves.Should().Equal("fila:1", "fila:2");
        enviado.ComisionPorcentaje.Should().Be(30m);
        enviado.DescuentoPorcentaje.Should().Be(10m);
        sut.TempData["AvisoModal"].Should().Be("El grupo quedó cubierto.");
    }

    [Fact]
    public async Task Guardar_porcentajes_guarda_comision_y_descuento_en_la_base()
    {
        var api = Api(comision: null);
        api.Setup(a => a.GuardarComisionRevisadaManualAsync(25m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<ParametrosRevisadaManualResponse>.Ok(new() { ComisionPorcentaje = 25m }, "Guardado."));
        api.Setup(a => a.GuardarDescuentoRevisadaManualAsync(10m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<ParametrosRevisadaManualResponse>.Ok(new() { DescuentoPorcentaje = 10m }, "Guardado."));
        var sut = Crear(api.Object);

        var result = await sut.GuardarPorcentajes("25", "10", new DateTime(2026, 10, 21), new DateTime(2026, 10, 21), cancellationToken: CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Cubierto");
        api.Verify(a => a.GuardarComisionRevisadaManualAsync(25m, It.IsAny<CancellationToken>()), Times.Once);
        api.Verify(a => a.GuardarDescuentoRevisadaManualAsync(10m, It.IsAny<CancellationToken>()), Times.Once);
        sut.TempData["AvisoModal"].Should().Be(UiTexts.PorcentajesGuardados);
    }

    [Fact]
    public async Task Abrir_cubierto_muestra_los_porcentajes_guardados_sin_volver_a_guardarlos()
    {
        var api = Api(comision: 25m);
        ConCubierto(api);
        var sut = Crear(api.Object);

        var result = await sut.Cubierto(null, null, null, null, cancellationToken: CancellationToken.None);

        result.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<RevisadaPaginaViewModel>().Which.Comision.Should().Be("25");
        api.Verify(a => a.GuardarComisionRevisadaManualAsync(It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cubierto_con_rango_que_incluye_hoy_muestra_lo_cubierto_hoy_y_el_historico()
    {
        var api = Api(comision: 20m, descuento: 10m);
        ConCubierto(api);
        var sut = Crear(api.Object);

        var result = await sut.Cubierto(DateTime.Today.AddDays(-3), DateTime.Today, null, null, cancellationToken: CancellationToken.None);

        var modelo = result.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<RevisadaPaginaViewModel>().Subject;
        modelo.IncluyeHoy.Should().BeTrue();
        modelo.Historico.Should().NotBeNull();
        api.Verify(a => a.RevisadaCubiertoHoyAsync(It.IsAny<CancellationToken>()), Times.Once);
        api.Verify(a => a.RevisadaCubiertoHistoricoAsync(DateTime.Today.AddDays(-3), DateTime.Today, "fecha", true, 1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cubierto_con_rango_sin_hoy_muestra_solo_el_historico()
    {
        var api = Api(comision: 20m, descuento: 10m);
        ConCubierto(api);
        var sut = Crear(api.Object);

        var result = await sut.Cubierto(DateTime.Today.AddDays(-10), DateTime.Today.AddDays(-5), null, null, cancellationToken: CancellationToken.None);

        result.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<RevisadaPaginaViewModel>().Which.IncluyeHoy.Should().BeFalse();
        api.Verify(a => a.RevisadaCubiertoHoyAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Resumen_con_dias_anteriores_y_hoy_muestra_lo_de_hoy_y_el_historico_hasta_ayer()
    {
        var api = Api(comision: 20m, descuento: 10m);
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
        var api = Api(comision: 20m, descuento: 10m);
        ConResumenHistorico(api);
        var sut = Crear(api.Object);

        var result = await sut.Resumen(DateTime.Today, DateTime.Today, null, cancellationToken: CancellationToken.None);

        result.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<RevisadaPaginaViewModel>().Which.HistoricoResumen.Should().BeNull();
        api.Verify(a => a.RevisadaResumenHistoricoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Resumen_solo_de_dias_anteriores_muestra_solo_el_historico()
    {
        var api = Api(comision: 20m, descuento: 10m);
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
    public async Task Recubrir_previa_sin_porcentajes_guardados_avisa_en_el_modal()
    {
        var api = Api(comision: 20m);
        var sut = Crear(api.Object);

        var result = await sut.RecubrirPrevia(new DateTime(2026, 10, 21), new DateTime(2026, 10, 21), null, cancellationToken: CancellationToken.None);

        result.Should().BeOfType<PartialViewResult>().Subject.Model.Should().BeOfType<RecubrirPreviaViewModel>()
            .Which.Error.Should().Be(UiTexts.PorcentajesSinConfigurar);
        api.Verify(a => a.RevisadaRecubrirPreviaAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<decimal?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Recubrir_envia_las_claves_con_comision_y_descuento_guardados()
    {
        RecubrirRevisadaManualRequest? enviado = null;
        var api = Api(comision: 20m, descuento: 10m);
        api.Setup(a => a.RevisadaRecubrirAsync(It.IsAny<RecubrirRevisadaManualRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RecubrirRevisadaManualRequest, CancellationToken>((r, _) => enviado = r)
            .ReturnsAsync(ApiCallResult<VistaRevisadaManualResponse>.Ok(new() { Jugadas = 2, Grupos = [new(), new()] }, "Los números quedaron cubiertos."));
        var sut = Crear(api.Object);

        var result = await sut.Recubrir(["fila:1", "fila:2"], new DateTime(2026, 10, 21), new DateTime(2026, 10, 21), cancellationToken: CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Resumen");
        enviado!.Claves.Should().Equal("fila:1", "fila:2");
        enviado.ComisionPorcentaje.Should().Be(20m);
        enviado.DescuentoPorcentaje.Should().Be(10m);
        sut.TempData["AvisoModal"].Should().Be("Los números quedaron cubiertos. Números: 2. Registros: 2.");
    }

    private static Mock<IAdminApiClient> Api(decimal? comision, decimal? descuento = null)
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(a => a.ParametrosRevisadaManualAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<ParametrosRevisadaManualResponse>.Ok(new() { ComisionPorcentaje = comision, DescuentoPorcentaje = descuento }, "Listo."));
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
