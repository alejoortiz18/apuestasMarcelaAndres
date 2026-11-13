using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NewRich.Admin.Constants;
using NewRich.Admin.Controllers;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Grupos;
using NewRich.Application.Contracts.Kpi;
using NewRich.Application.Contracts.PremiosEntregadosWeb;
using NewRich.Application.Contracts.RevisadaManual;
using NewRich.Application.Contracts.Utilidades;
using NewRich.Application.Contracts.VentasRegistradasWeb;

namespace NewRich.Admin.Tests;

public sealed class UtilidadesInformeVentasTests
{
    private static readonly Guid Norte = Guid.NewGuid();

    [Fact]
    public async Task Utilidades_muestra_el_informe_de_ventas_del_periodo_y_el_grupo_elegidos()
    {
        var api = Api();
        var sut = Crear(api.Object);

        var vista = (ViewResult)await sut.Indicadores(VentasInforme.Periodo15, null, null, Norte, cancellationToken: CancellationToken.None);

        var modelo = vista.Model.Should().BeOfType<UtilidadesVentasViewModel>().Subject;
        modelo.Informe.Should().NotBeNull();
        modelo.Informe!.Resumen.TotalIngresos.Should().Be(1_080_000m);
        modelo.Informe.Hasta.DayNumber.Should().Be(modelo.Informe.Desde.DayNumber + 14);
        modelo.Configuracion.Detalle.Select(d => d.Valor).Should().Contain("Norte");
        api.Verify(x => x.ConsultarKpiAsync(It.Is<KpiRequest>(k => k.GrupoId == Norte), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Utilidades_con_rango_mayor_a_31_dias_avisa_y_muestra_los_ultimos_7_dias()
    {
        var sut = Crear(Api().Object);

        var vista = (ViewResult)await sut.Indicadores(VentasInforme.PeriodoRango, new DateOnly(2026, 8, 1), new DateOnly(2026, 10, 30), null, cancellationToken: CancellationToken.None);

        var modelo = vista.Model.Should().BeOfType<UtilidadesVentasViewModel>().Subject;
        sut.TempData["AvisoModal"].Should().Be(UiTexts.InformeRangoMaximo);
        modelo.Informe!.Hasta.DayNumber.Should().Be(modelo.Informe.Desde.DayNumber + 6);
    }

    private static Mock<IAdminApiClient> Api()
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.ListarGruposAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<GrupoResponse>>.Ok([new GrupoResponse { GrupoId = Norte, Nombre = "Norte" }], "Listo."));
        api.Setup(x => x.ConsultarKpiAsync(It.IsAny<KpiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<KpiResponse>.Ok(new KpiResponse { Ingresos = 1_000_000m }, "Listo."));
        api.Setup(x => x.TotalVentasRegistradasWebAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<TotalVentasRegistradasWebResponse>.Ok(new TotalVentasRegistradasWebResponse(50_000m, 1, 30_000m, 2), "Listo."));
        api.Setup(x => x.ListadoPremiosEntregadosWebAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<PremioEntregadoWebResponse>>.Ok([], "Listo."));
        api.Setup(x => x.UtilidadesIndicadoresAsync(It.IsAny<string>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<InformeGastosPropiosResponse>.Ok(
                new InformeGastosPropiosResponse(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 7), 0m, 0, 0m, null, null, null, 0m, 0, [], [], 0m, 0m), "Listo."));
        api.Setup(x => x.RevisadaCubiertoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<VistaRevisadaManualResponse>.Ok(new VistaRevisadaManualResponse(), "Listo."));
        api.Setup(x => x.CubrirVentaCubiertoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<Guid>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<VistaRevisadaManualResponse>.Ok(new VistaRevisadaManualResponse(), "Listo."));
        return api;
    }

    private static UtilidadesController Crear(IAdminApiClient api)
    {
        var http = new DefaultHttpContext();
        return new UtilidadesController(api)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        };
    }
}
