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
    [Fact]
    public async Task GuardarHorarios_sin_cambios_no_dice_que_actualizo()
    {
        var api = new Mock<IAdminApiClient>(MockBehavior.Strict);
        var sut = Crear(api.Object);

        await sut.GuardarHorarios([], null, null, null, null);

        sut.TempData["AvisoModal"].Should().Be(SuccessMessages.HorariosSinCambios);
        api.Verify(
            x => x.ActualizarHorariosLoteriasAsync(It.IsAny<ActualizarHorariosLoteriasRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GuardarHorarios_con_minuto_nuevo_lo_envia_a_la_api()
    {
        var loteriaId = Guid.NewGuid();
        ActualizarHorariosLoteriasRequest? enviado = null;
        var api = new Mock<IAdminApiClient>(MockBehavior.Strict);
        api.Setup(x => x.ActualizarHorariosLoteriasAsync(It.IsAny<ActualizarHorariosLoteriasRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ActualizarHorariosLoteriasRequest, CancellationToken>((request, _) => enviado = request)
            .ReturnsAsync(ApiCallResult<List<LoteriaResponse>>.Ok(
                [new LoteriaResponse { LoteriaId = loteriaId, Nombre = "Antioqueña Día", HoraInicio = "06:20", HoraFin = "09:30" }],
                SuccessMessages.OperacionExitosa));
        var sut = Crear(api.Object);

        await sut.GuardarHorarios(
            [
                new HorarioLoteriaRequest { LoteriaId = Guid.Empty, HoraInicio = "06:00", HoraFin = "09:30" },
                new HorarioLoteriaRequest { LoteriaId = loteriaId, HoraInicio = "06:20", HoraFin = "09:30" }
            ],
            null,
            null,
            null,
            null);

        enviado.Should().NotBeNull();
        enviado!.Loterias.Should().ContainSingle(x => x.LoteriaId == loteriaId && x.HoraInicio == "06:20");
        sut.TempData["AvisoModal"].Should().Be(SuccessMessages.RegistroActualizado);
        sut.TempData["HorariosGuardados"].Should().Be(true);
    }

    [Fact]
    public async Task GuardarHorarios_si_la_api_rechaza_conserva_los_cambios_pendientes()
    {
        var api = new Mock<IAdminApiClient>(MockBehavior.Strict);
        api.Setup(x => x.ActualizarHorariosLoteriasAsync(It.IsAny<ActualizarHorariosLoteriasRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<LoteriaResponse>>.Fail("El horario de la lotería Bogotá debe estar dentro del horario de actividad del PDA (06:00 a 20:00).", 400));
        var sut = Crear(api.Object);

        await sut.GuardarHorarios(
            [new HorarioLoteriaRequest { LoteriaId = Guid.NewGuid(), HoraInicio = "06:00", HoraFin = "21:00" }],
            null,
            null,
            null,
            null);

        sut.TempData["AvisoModal"].Should().Be("El horario de la lotería Bogotá debe estar dentro del horario de actividad del PDA (06:00 a 20:00).");
        sut.TempData.ContainsKey("HorariosGuardados").Should().BeFalse();
    }

    [Fact]
    public async Task GuardarHorarios_si_la_api_no_cambio_nada_no_dice_que_actualizo()
    {
        var api = new Mock<IAdminApiClient>(MockBehavior.Strict);
        api.Setup(x => x.ActualizarHorariosLoteriasAsync(It.IsAny<ActualizarHorariosLoteriasRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<LoteriaResponse>>.Ok([], SuccessMessages.HorariosSinCambios));
        var sut = Crear(api.Object);

        await sut.GuardarHorarios(
            [new HorarioLoteriaRequest { LoteriaId = Guid.NewGuid(), HoraInicio = "06:00", HoraFin = "09:30" }],
            null,
            null,
            null,
            null);

        sut.TempData["AvisoModal"].Should().Be(SuccessMessages.HorariosSinCambios);
    }

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
