using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NewRich.Admin.Controllers;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.RevisadaManual;

namespace NewRich.Admin.Tests;

public sealed class CubrirVentaResumenTests
{
    [Fact]
    public async Task Resumen_sin_fechas_consulta_solo_el_dia_de_hoy()
    {
        DateTime? desdePedido = null;
        DateTime? hastaPedido = null;
        var api = new Mock<IAdminApiClient>();
        api.Setup(a => a.CubrirVentaResumenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<decimal?>(), It.IsAny<CancellationToken>()))
            .Callback<DateTime, DateTime, decimal?, CancellationToken>((desde, hasta, _, _) =>
            {
                desdePedido = desde;
                hastaPedido = hasta;
            })
            .ReturnsAsync(ApiCallResult<VistaRevisadaManualResponse>.Ok(new VistaRevisadaManualResponse
            {
                Jugadas = 1,
                Loterias = 1,
                Total = 10_000,
                Grupos =
                [
                    new GrupoRevisadaManualResponse
                    {
                        Loteria = "Cali",
                        Numero = "1735",
                        Subtotal = 10_000,
                        Lineas = [new LineaRevisadaManualResponse { Clave = "juego", Valor = 10_000, Fecha = new DateTime(2026, 11, 3, 11, 20, 0) }]
                    }
                ]
            }, "Listo."));
        var sut = Crear(api.Object);

        var result = await sut.Resumen(null, null, null, cancellationToken: CancellationToken.None);

        desdePedido.Should().NotBeNull();
        desdePedido!.Value.Date.Should().Be(DateTime.Today);
        hastaPedido!.Value.Date.Should().Be(DateTime.Today);
        var modelo = result.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<RevisadaPaginaViewModel>().Subject;
        modelo.Desde.Should().Be(DateTime.Today);
        modelo.Hasta.Should().Be(DateTime.Today);
        modelo.Pagina.Items.Should().ContainSingle(l => l.Nombre == "Cali" && l.Grupos[0].Numero == "1735");
        modelo.HistoricoResumen.Should().BeNull();
        api.Verify(a => a.CubrirVentaResumenHistoricoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static CubrirVentaController Crear(IAdminApiClient api)
    {
        var http = new DefaultHttpContext();
        var sut = new CubrirVentaController(api)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        };
        sut.Url = Mock.Of<IUrlHelper>();
        return sut;
    }
}
