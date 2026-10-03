using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NewRich.Admin.Controllers;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Jornadas;
using NewRich.Application.Contracts.Loterias;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Tests;

public sealed class LoteriaJornadaFormularioTests
{
    private static readonly Guid Manana = Guid.NewGuid();
    private static readonly Guid Tarde = Guid.NewGuid();

    [Fact]
    public async Task Configuracion_agregar_loteria_muestra_las_jornadas()
    {
        var api = ApiConJornadas();
        var sut = CrearConfiguracion(api.Object);

        var resultado = await sut.CrearLoteria(CancellationToken.None);

        Modelo(resultado).Jornadas.Select(j => j.Nombre).Should().Equal("Mañana", "Tarde");
    }

    [Fact]
    public async Task Configuracion_agregar_loteria_envia_la_jornada_elegida()
    {
        var api = ApiConJornadas();
        CrearLoteriaRequest? enviado = null;
        api.Setup(x => x.CrearLoteriaAsync(It.IsAny<CrearLoteriaRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CrearLoteriaRequest, CancellationToken>((r, _) => enviado = r)
            .ReturnsAsync(ApiCallResult<LoteriaResponse>.Ok(new LoteriaResponse(), "Listo."));
        var sut = CrearConfiguracion(api.Object);

        await sut.CrearLoteria(FormularioValido(), CancellationToken.None);

        enviado!.JornadaId.Should().Be(Tarde);
    }

    [Fact]
    public async Task Configuracion_si_la_api_rechaza_vuelve_al_formulario_con_las_jornadas()
    {
        var api = ApiConJornadas();
        api.Setup(x => x.CrearLoteriaAsync(It.IsAny<CrearLoteriaRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<LoteriaResponse>.Fail("La hora de fin debe quedar dentro del horario de la jornada.", 400));
        var sut = CrearConfiguracion(api.Object);

        var resultado = await sut.CrearLoteria(FormularioValido(), CancellationToken.None);

        var modelo = Modelo(resultado);
        modelo.JornadaId.Should().Be(Tarde);
        modelo.Jornadas.Should().HaveCount(2);
    }

    [Fact]
    public async Task Configuracion_editar_loteria_precarga_su_jornada()
    {
        var id = Guid.NewGuid();
        var api = ApiConJornadas();
        api.Setup(x => x.ListarLoteriasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<LoteriaResponse>>.Ok([Loteria(id)], "Listo."));
        var sut = CrearConfiguracion(api.Object);

        var resultado = await sut.EditarLoteria(id, CancellationToken.None);

        var modelo = Modelo(resultado);
        modelo.JornadaId.Should().Be(Tarde);
        modelo.Jornadas.Should().HaveCount(2);
    }

    [Fact]
    public async Task Configuracion_editar_loteria_envia_la_jornada_elegida()
    {
        var id = Guid.NewGuid();
        var api = ApiConJornadas();
        ActualizarLoteriaRequest? enviado = null;
        api.Setup(x => x.ActualizarLoteriaAsync(id, It.IsAny<ActualizarLoteriaRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, ActualizarLoteriaRequest, CancellationToken>((_, r, _) => enviado = r)
            .ReturnsAsync(ApiCallResult<LoteriaResponse>.Ok(new LoteriaResponse(), "Listo."));
        var sut = CrearConfiguracion(api.Object);

        await sut.EditarLoteria(id, FormularioValido(), CancellationToken.None);

        enviado!.JornadaId.Should().Be(Tarde);
    }

    [Fact]
    public async Task Configuracion_habilitar_o_deshabilitar_conserva_la_jornada()
    {
        var id = Guid.NewGuid();
        var api = ApiConJornadas();
        api.Setup(x => x.ListarLoteriasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<LoteriaResponse>>.Ok([Loteria(id)], "Listo."));
        ActualizarLoteriaRequest? enviado = null;
        api.Setup(x => x.ActualizarLoteriaAsync(id, It.IsAny<ActualizarLoteriaRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, ActualizarLoteriaRequest, CancellationToken>((_, r, _) => enviado = r)
            .ReturnsAsync(ApiCallResult<LoteriaResponse>.Ok(new LoteriaResponse(), "Listo."));
        var sut = CrearConfiguracion(api.Object);

        await sut.CambiarEstadoLoteria(id, CancellationToken.None);

        enviado!.Estado.Should().Be(EstadoGeneral.Inactivo);
        enviado.JornadaId.Should().Be(Tarde);
    }

    [Fact]
    public async Task Loterias_nueva_loteria_muestra_las_jornadas_y_envia_la_elegida()
    {
        var api = ApiConJornadas();
        CrearLoteriaRequest? enviado = null;
        api.Setup(x => x.CrearLoteriaAsync(It.IsAny<CrearLoteriaRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CrearLoteriaRequest, CancellationToken>((r, _) => enviado = r)
            .ReturnsAsync(ApiCallResult<LoteriaResponse>.Ok(new LoteriaResponse(), "Listo."));
        var sut = CrearLoterias(api.Object);

        var formulario = await sut.Crear(CancellationToken.None);
        await sut.Crear(FormularioValido(), CancellationToken.None);

        Modelo(formulario).Jornadas.Should().HaveCount(2);
        enviado!.JornadaId.Should().Be(Tarde);
    }

    [Fact]
    public async Task Loterias_editar_precarga_y_envia_la_jornada()
    {
        var id = Guid.NewGuid();
        var api = ApiConJornadas();
        api.Setup(x => x.ListarLoteriasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<LoteriaResponse>>.Ok([Loteria(id)], "Listo."));
        ActualizarLoteriaRequest? enviado = null;
        api.Setup(x => x.ActualizarLoteriaAsync(id, It.IsAny<ActualizarLoteriaRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, ActualizarLoteriaRequest, CancellationToken>((_, r, _) => enviado = r)
            .ReturnsAsync(ApiCallResult<LoteriaResponse>.Ok(new LoteriaResponse(), "Listo."));
        var sut = CrearLoterias(api.Object);

        var formulario = await sut.Editar(id, CancellationToken.None);
        await sut.Editar(id, FormularioValido(), CancellationToken.None);

        Modelo(formulario).JornadaId.Should().Be(Tarde);
        Modelo(formulario).Jornadas.Should().HaveCount(2);
        enviado!.JornadaId.Should().Be(Tarde);
    }

    private static Mock<IAdminApiClient> ApiConJornadas()
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.ListarJornadasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<JornadaResponse>>.Ok(
                [
                    new JornadaResponse { JornadaId = Manana, Nombre = "Mañana" },
                    new JornadaResponse { JornadaId = Tarde, Nombre = "Tarde" }
                ],
                "Listo."));
        return api;
    }

    private static LoteriaFormViewModel FormularioValido() => new()
    {
        Nombre = "Cali",
        Tope = 1000,
        Estado = EstadoGeneral.Activo,
        HoraInicio = "10:00",
        HoraFin = "13:00",
        JornadaId = Tarde,
        DiasHabilitados = [DiaSemana.Lunes]
    };

    private static LoteriaResponse Loteria(Guid id) => new()
    {
        LoteriaId = id,
        Nombre = "Cali",
        Estado = EstadoGeneral.Activo,
        Tope = 1000,
        HoraInicio = "10:00",
        HoraFin = "13:00",
        JornadaId = Tarde,
        JornadaNombre = "Tarde"
    };

    private static LoteriaFormViewModel Modelo(IActionResult resultado) =>
        resultado.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<LoteriaFormViewModel>().Subject;

    private static ConfiguracionController CrearConfiguracion(IAdminApiClient api)
    {
        var http = new DefaultHttpContext();
        return new ConfiguracionController(api, new MinutosInactividadSesion(api, new MemoryCache(new MemoryCacheOptions())))
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        };
    }

    private static LoteriasController CrearLoterias(IAdminApiClient api)
    {
        var http = new DefaultHttpContext();
        return new LoteriasController(api)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>()),
            Url = Mock.Of<IUrlHelper>()
        };
    }
}
