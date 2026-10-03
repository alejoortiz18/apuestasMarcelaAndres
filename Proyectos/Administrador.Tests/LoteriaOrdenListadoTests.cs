using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NewRich.Admin.Controllers;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Application.Contracts.Loterias;
using NewRich.Application.Contracts.Versiones;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Tests;

public sealed class LoteriaOrdenListadoTests
{
    [Fact]
    public async Task Configuracion_filtra_el_catalogo_por_jornada_y_lo_ordena()
    {
        var api = ApiConLoterias(
            Loteria("Cafetero", "11:30", 0, 0),
            Loteria("Boyacá", "10:00", 0, 0),
            Loteria("Valle", "22:00", 0, 0));
        var sut = new ConfiguracionController(api.Object, new MinutosInactividadSesion(api.Object, new MemoryCache(new MemoryCacheOptions())))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var resultado = await sut.Index(null, "Mañana", "horaFin", "desc");

        var modelo = resultado.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<ConfiguracionIndexViewModel>().Subject;
        modelo.Pagina.Items.Select(l => l.Nombre).Should().Equal("Cafetero", "Boyacá");
        modelo.Jornada.Should().Be("Mañana");
        modelo.Orden.Should().Be("horafin");
        modelo.Direccion.Should().Be("desc");
        modelo.Topes.Should().HaveCount(3);
    }

    [Fact]
    public async Task Configuracion_sin_orden_muestra_el_catalogo_por_nombre_ascendente()
    {
        var api = ApiConLoterias(Loteria("valle", "22:00", 0, 0), Loteria("Bogotá", "22:30", 0, 0));
        var sut = new ConfiguracionController(api.Object, new MinutosInactividadSesion(api.Object, new MemoryCache(new MemoryCacheOptions())))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var resultado = await sut.Index(null, null, null, null);

        var modelo = resultado.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<ConfiguracionIndexViewModel>().Subject;
        modelo.Pagina.Items.Select(l => l.Nombre).Should().Equal("Bogotá", "valle");
        modelo.Orden.Should().Be("nombre");
        modelo.Direccion.Should().Be("asc");
    }

    [Fact]
    public async Task Loterias_ordena_el_resumen_por_la_columna_elegida()
    {
        var api = ApiConLoterias(
            Loteria("A", "10:00", 1, 300),
            Loteria("B", "11:00", 5, 100),
            Loteria("C", "12:00", 2, 200));
        var http = new DefaultHttpContext();
        var sut = new LoteriasController(api.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>()),
            Url = Mock.Of<IUrlHelper>()
        };

        var resultado = await sut.Index(null, null, "boletos", "desc");

        var modelo = resultado.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<LoteriasIndexViewModel>().Subject;
        modelo.Pagina.Items.Select(l => l.Nombre).Should().Equal("B", "C", "A");
        modelo.Orden.Should().Be("boletos");
        modelo.Direccion.Should().Be("desc");
    }

    private static Mock<IAdminApiClient> ApiConLoterias(params LoteriaResponse[] loterias)
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.ListarLoteriasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<LoteriaResponse>>.Ok([.. loterias], "ok"));
        api.Setup(x => x.ObtenerConfiguracionOperativaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<ConfiguracionOperativaResponse>.Ok(null, "ok"));
        api.Setup(x => x.ListarNumerosRestringidosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<NumeroRestringidoResponse>>.Ok([], "ok"));
        api.Setup(x => x.ListarVersionesAplicacionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<VersionAplicacionResponse>>.Ok([], "ok"));
        return api;
    }

    private static LoteriaResponse Loteria(string nombre, string horaFin, int boletos, decimal total) => new()
    {
        LoteriaId = Guid.NewGuid(),
        Nombre = nombre,
        Estado = EstadoGeneral.Activo,
        HoraInicio = "06:00",
        HoraFin = horaFin,
        BoletosVendidos = boletos,
        TotalVendido = total
    };
}
