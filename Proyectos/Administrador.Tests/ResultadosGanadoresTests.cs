using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NewRich.Admin.Controllers;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Loterias;
using NewRich.Application.Contracts.Resultados;

namespace NewRich.Admin.Tests;

/// <summary>
/// La cantidad de ganadores la calcula la API sobre la ventana horaria real de la venta.
/// El administrador debe mostrar ese número tal cual: una venta de las 7 de la noche en
/// Colombia queda registrada en UTC al día siguiente, y cualquier recuento propio del
/// administrador que compare fechas sin ese desfase la deja por fuera.
/// </summary>
public sealed class ResultadosGanadoresTests
{
    private static readonly Guid Armenia = Guid.NewGuid();

    [Fact]
    public async Task Index_muestra_los_ganadores_que_reporta_la_api()
    {
        var api = ApiConResultado(ganadoresSegunApi: 1);
        var sut = Crear(api.Object);

        var vista = await sut.Index(null, null, cancellationToken: CancellationToken.None) as ViewResult;

        var model = vista!.Model.Should().BeOfType<ResultadosIndexViewModel>().Subject;
        var resultado = model.Pagina.Items.Should().ContainSingle().Subject;
        resultado.CantidadGanadores.Should().Be(1);
        resultado.TieneGanadores.Should().BeTrue();
    }

    [Fact]
    public async Task Index_no_recalcula_ganadores_con_filtros_de_fecha_propios()
    {
        var api = ApiConResultado(ganadoresSegunApi: 1);
        var sut = Crear(api.Object);

        await sut.Index(null, null, cancellationToken: CancellationToken.None);

        api.Verify(
            x => x.FiltrarBoletosAsync(It.IsAny<FiltroBoletosRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Ganadores_pide_el_detalle_al_endpoint_del_resultado()
    {
        var api = ApiConResultado(ganadoresSegunApi: 1);
        var resultadoId = (await api.Object.ListarResultadosAsync(null, null, CancellationToken.None))
            .Data![0].NumeroGanadorId;
        api.Setup(x => x.ListarGanadoresResultadoAsync(resultadoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<BoletoListaResponse>>.Ok(
                [new BoletoListaResponse { CodigoPublico = "1699284", Estado = "Ganador" }],
                "Listo."));
        var sut = Crear(api.Object);

        var vista = await sut.Ganadores(resultadoId, CancellationToken.None) as ViewResult;

        var model = vista!.Model.Should().BeOfType<ResultadoGanadoresViewModel>().Subject;
        model.Ganadores.Should().ContainSingle(b => b.CodigoPublico == "1699284");
        api.Verify(
            x => x.FiltrarBoletosAsync(It.IsAny<FiltroBoletosRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Mock<IAdminApiClient> ApiConResultado(int ganadoresSegunApi)
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.ListarLoteriasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<LoteriaResponse>>.Ok(
                [new LoteriaResponse { LoteriaId = Armenia, Nombre = "Armenia" }],
                "Listo."));
        api.Setup(x => x.ListarResultadosAsync(
                It.IsAny<DateOnly?>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<ResultadoResponse>>.Ok(
                [
                    new ResultadoResponse
                    {
                        NumeroGanadorId = Guid.NewGuid(),
                        LoteriaId = Armenia,
                        Loteria = "Armenia",
                        FechaJuego = new DateOnly(2026, 9, 20),
                        Numero = "3221",
                        CantidadGanadores = ganadoresSegunApi
                    }
                ],
                "Listo."));

        // El recuento por fechas planas pierde la venta nocturna: así se veía el error.
        api.Setup(x => x.FiltrarBoletosAsync(It.IsAny<FiltroBoletosRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<BoletoListaResponse>>.Ok([], "Listo."));
        return api;
    }

    private static ResultadosController Crear(IAdminApiClient api) => new(api)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
        }
    };
}
