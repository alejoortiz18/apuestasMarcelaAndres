using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NewRich.Admin.Controllers;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Loterias;
using NewRich.Constants.Messages;

namespace NewRich.Admin.Tests;

public sealed class ConfiguracionHorariosControllerTests
{
    private const string FueraDelPda = "El horario de la lotería Bogotá debe estar dentro del horario de actividad del PDA (06:00 a 20:00).";

    [Fact]
    public async Task GuardarHorarios_sin_cambios_avisa_y_no_llama_a_la_api()
    {
        var api = new Mock<IAdminApiClient>(MockBehavior.Strict);
        var sut = Crear(api.Object);

        var resultado = await sut.GuardarHorarios([], "cal", "Noche", "horaFin", "desc", 2, 10);

        sut.TempData["AvisoModal"].Should().Be(SuccessMessages.HorariosSinCambios);
        RutaDe(resultado).Should().BeEquivalentTo(new Dictionary<string, object?>
        {
            ["q"] = "cal", ["jornada"] = "Noche", ["orden"] = "horaFin", ["dir"] = "desc", ["page"] = 2, ["pageSize"] = 10
        });
    }

    [Fact]
    public async Task GuardarHorarios_envia_solo_las_loterias_con_identificador()
    {
        var loteriaId = Guid.NewGuid();
        ActualizarHorariosLoteriasRequest? enviado = null;
        var api = new Mock<IAdminApiClient>(MockBehavior.Strict);
        api.Setup(x => x.ActualizarHorariosLoteriasAsync(It.IsAny<ActualizarHorariosLoteriasRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ActualizarHorariosLoteriasRequest, CancellationToken>((request, _) => enviado = request)
            .ReturnsAsync(ApiCallResult<List<LoteriaResponse>>.Ok([], SuccessMessages.OperacionExitosa));
        var sut = Crear(api.Object);

        await sut.GuardarHorarios(
            [
                new HorarioLoteriaRequest { LoteriaId = Guid.Empty, HoraInicio = "06:00", HoraFin = "09:30" },
                new HorarioLoteriaRequest { LoteriaId = loteriaId, HoraInicio = "06:20", HoraFin = "09:30" }
            ],
            null, null, null, null);

        enviado!.Loterias.Should().ContainSingle(x => x.LoteriaId == loteriaId && x.HoraInicio == "06:20");
        sut.TempData["AvisoModal"].Should().Be(SuccessMessages.RegistroActualizado);
        sut.TempData["HorariosGuardados"].Should().Be(true);
    }

    [Fact]
    public async Task GuardarHorarios_si_la_api_rechaza_muestra_el_error_y_conserva_los_cambios_pendientes()
    {
        var api = new Mock<IAdminApiClient>(MockBehavior.Strict);
        api.Setup(x => x.ActualizarHorariosLoteriasAsync(It.IsAny<ActualizarHorariosLoteriasRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<LoteriaResponse>>.Fail(FueraDelPda, 400));
        var sut = Crear(api.Object);

        await sut.GuardarHorarios(
            [new HorarioLoteriaRequest { LoteriaId = Guid.NewGuid(), HoraInicio = "06:00", HoraFin = "21:00" }],
            null, null, null, null);

        sut.TempData["AvisoModal"].Should().Be(FueraDelPda);
        sut.TempData.ContainsKey("HorariosGuardados").Should().BeFalse();
    }

    [Fact]
    public async Task GuardarHorarios_si_la_api_no_cambio_nada_avisa_sin_decir_que_actualizo()
    {
        var api = new Mock<IAdminApiClient>(MockBehavior.Strict);
        api.Setup(x => x.ActualizarHorariosLoteriasAsync(It.IsAny<ActualizarHorariosLoteriasRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<LoteriaResponse>>.Ok([], SuccessMessages.HorariosSinCambios));
        var sut = Crear(api.Object);

        await sut.GuardarHorarios(
            [new HorarioLoteriaRequest { LoteriaId = Guid.NewGuid(), HoraInicio = "06:00", HoraFin = "09:30" }],
            null, null, null, null);

        sut.TempData["AvisoModal"].Should().Be(SuccessMessages.HorariosSinCambios);
        sut.TempData["HorariosGuardados"].Should().Be(true);
    }

    private static IDictionary<string, object?> RutaDe(IActionResult resultado) =>
        resultado.Should().BeOfType<RedirectToActionResult>().Subject.RouteValues!;

    private static ConfiguracionController Crear(IAdminApiClient api)
    {
        var http = new DefaultHttpContext();
        return new ConfiguracionController(api, new MinutosInactividadSesion(api, new MemoryCache(new MemoryCacheOptions())))
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        };
    }
}
