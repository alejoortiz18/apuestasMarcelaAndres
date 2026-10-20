using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NewRich.Admin.Controllers;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Utilidades;

namespace NewRich.Admin.Tests;

public sealed class UtilidadesRegistrarGastoTests
{
    private static readonly Guid Tipo = Guid.NewGuid();

    [Fact]
    public async Task Registrar_desde_movimientos_vuelve_a_movimientos_con_los_filtros()
    {
        var usuario = Guid.NewGuid();
        var filtro = Guid.NewGuid();
        var sut = Crear(Api().Object);

        var resultado = await sut.Registrar(
            Formulario("15.000"), "rango", new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 19),
            volver: "movimientos", usuarioId: usuario, filtroTipo: filtro, estado: "Activo", page: 2, pageSize: 25,
            cancellationToken: CancellationToken.None);

        var redireccion = resultado.Should().BeOfType<RedirectToActionResult>().Subject;
        redireccion.ActionName.Should().Be("Movimientos");
        redireccion.RouteValues!["periodo"].Should().Be("rango");
        redireccion.RouteValues["usuarioId"].Should().Be(usuario);
        redireccion.RouteValues["tipoGastoId"].Should().Be(filtro);
        redireccion.RouteValues["estado"].Should().Be("Activo");
        redireccion.RouteValues["page"].Should().Be(2);
        redireccion.RouteValues["pageSize"].Should().Be(25);
    }

    [Fact]
    public async Task Registrar_desde_movimientos_con_error_reabre_la_ventana_en_movimientos()
    {
        var sut = Crear(Api().Object);

        var resultado = await sut.Registrar(
            Formulario("abc"), "mes", null, null,
            volver: "movimientos", usuarioId: null, filtroTipo: null, estado: null,
            cancellationToken: CancellationToken.None);

        var vista = resultado.Should().BeOfType<ViewResult>().Subject;
        vista.ViewName.Should().Be("Movimientos");
        var modelo = vista.Model.Should().BeOfType<MovimientosUtilidadesViewModel>().Subject;
        modelo.AbrirModal.Should().BeTrue();
        modelo.Formulario.Error.Should().NotBeNullOrWhiteSpace();
        sut.ViewData["Nav"].Should().Be("utilidades-movimientos");
    }

    [Fact]
    public async Task Registrar_desde_resumen_sigue_volviendo_a_resumen()
    {
        var sut = Crear(Api().Object);

        var resultado = await sut.Registrar(
            Formulario("15.000"), "mes", null, null,
            volver: null, usuarioId: null, filtroTipo: null, estado: null,
            cancellationToken: CancellationToken.None);

        resultado.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Resumen");
    }

    [Fact]
    public void Movimientos_tiene_registrar_gasto_y_config_gastos_dorados()
    {
        var vista = File.ReadAllText(RutaVista("Movimientos.cshtml"));
        var inicio = vista.IndexOf("class=\"heading-actions\"", StringComparison.Ordinal);
        inicio.Should().BeGreaterThan(-1);
        var acciones = vista[inicio..vista.IndexOf("</div>", inicio, StringComparison.Ordinal)];

        var registrar = acciones.IndexOf("data-open-gasto", StringComparison.Ordinal);
        var config = acciones.IndexOf("asp-action=\"Configuracion\"", StringComparison.Ordinal);
        registrar.Should().BeGreaterThan(-1);
        config.Should().BeGreaterThan(registrar);
        acciones.Should().Contain("UiTexts.NavUtilidadesRegistrar");
        acciones.Should().Contain("UiTexts.ConfigGastos");
        acciones.Should().NotContain("class=\"secondary\"");
        vista.Should().Contain("<partial name=\"_RegistrarGasto\"");
    }

    private static RegistrarGastoAdminViewModel Formulario(string valor) => new()
    {
        TipoGastoId = Tipo,
        FechaGasto = new DateOnly(2026, 10, 19),
        Valor = valor
    };

    private static Mock<IAdminApiClient> Api()
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.UtilidadesTiposActivosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<TipoGastoResponse>>.Ok([new TipoGastoResponse(Tipo, "Papelería", false)], "Listo."));
        api.Setup(x => x.UtilidadesRegistrarAsync(It.IsAny<RegistrarGastoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<GastoRegistradoResponse>.Ok(
                new GastoRegistradoResponse(Guid.NewGuid(), Guid.NewGuid(), "Papelería", new DateOnly(2026, 10, 19), 15000m, null, "Activo"), "Listo."));
        api.Setup(x => x.UtilidadesMovimientosAsync(It.IsAny<string>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<MovimientosGastosResponse>.Ok(
                new MovimientosGastosResponse(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 19), 0m, 0, 0, []), "Listo."));
        api.Setup(x => x.ListarUsuariosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<NewRich.Application.Contracts.Usuarios.UsuarioResponse>>.Ok([], "Listo."));
        api.Setup(x => x.UtilidadesCatalogoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<TipoGastoCatalogoResponse>>.Ok([], "Listo."));
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

    private static string RutaVista(string archivo)
    {
        var ruta = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Administrador", "Views", "Utilidades", archivo));
        File.Exists(ruta).Should().BeTrue($"se esperaba la vista en {ruta}");
        return ruta;
    }
}
