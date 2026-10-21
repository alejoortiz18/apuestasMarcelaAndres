using System.Text.Json;
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
using NewRich.Application.Contracts.Informes;
using NewRich.Application.Contracts.Kpi;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Constants;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Tests;

public sealed class InformesControllerTests
{
    [Fact]
    public async Task Index_marca_el_menu_y_refleja_la_configuracion_guardada_de_vendedores()
    {
        var api = Api();
        api.Setup(a => a.ListarConfiguracionesInformeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<ConfiguracionInformeResponse>>.Ok(
                [new() { Categoria = CategoriasInforme.Vendedores, Filtros = """{"tipos":[2]}""" }], "Listo."));
        var sut = Crear(api.Object);

        var result = await sut.Index(null, null, cancellationToken: CancellationToken.None);

        sut.ViewData["Nav"].Should().Be("informes");
        sut.ViewData["Title"].Should().Be(UiTexts.NavInformes);
        var modelo = result.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<InformesViewModel>().Subject;
        modelo.Tarjetas.Select(t => (t.Clave, t.Habilitada)).Should().Equal(
            (CategoriasInforme.Vendedores, true), (CategoriasInforme.Ventas, true),
            (CategoriasInforme.Egresos, true), (CategoriasInforme.Recaudo, true));
        modelo.Tarjetas[0].Configurada.Should().BeTrue();
        modelo.Vendedores.Filtros.Tipos.Should().Equal(RolUsuario.Vendedor);
        modelo.Vendedores.Resumen.Cumplen.Should().Be(1);
        modelo.Informe.Should().NotBeNull();
        modelo.Informe!.Incluidos.Should().Be(1);
    }

    [Fact]
    public async Task Index_sin_vendedores_guardado_no_muestra_informe()
    {
        var sut = Crear(Api().Object);

        var result = await sut.Index(null, null, cancellationToken: CancellationToken.None);

        result.Should().BeOfType<ViewResult>().Subject.Model.As<InformesViewModel>().Informe.Should().BeNull();
    }

    [Fact]
    public async Task Guardar_vendedores_envia_los_filtros_elegidos_a_la_api()
    {
        string? enviado = null;
        var grupo = $"g:{Guid.NewGuid()}";
        var api = Api();
        api.Setup(a => a.GuardarConfiguracionInformeAsync(CategoriasInforme.Vendedores, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, filtros, _) => enviado = filtros)
            .ReturnsAsync(ApiCallResult<ConfiguracionInformeResponse>.Ok(new(), "Guardado."));
        var sut = Crear(api.Object);

        var result = await sut.GuardarVendedores("elegidos", [grupo], "todos", [Guid.NewGuid()], "elegidos", [RolUsuario.Vendedor], CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Index");
        var filtros = JsonSerializer.Deserialize<FiltrosVendedoresInforme>(enviado!, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        filtros.Grupos.Should().Equal(grupo);
        filtros.Usuarios.Should().BeNull();
        filtros.Tipos.Should().Equal(RolUsuario.Vendedor);
        sut.TempData["AvisoModal"].Should().Be("Guardado.");
    }

    [Fact]
    public async Task Guardar_vendedores_sin_elegir_ningun_grupo_no_llama_la_api()
    {
        var api = Api();
        var sut = Crear(api.Object);

        await sut.GuardarVendedores("elegidos", [], "todos", null, "todos", null, CancellationToken.None);

        api.Verify(a => a.GuardarConfiguracionInformeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        sut.TempData["AvisoModal"].Should().Be(UiTexts.InformeEligeGrupo);
    }

    [Theory]
    [InlineData(CategoriasInforme.Ventas)]
    [InlineData(CategoriasInforme.Egresos)]
    [InlineData(CategoriasInforme.Recaudo)]
    public async Task Guardar_periodo_envia_periodo_y_grupos_a_su_categoria(string categoria)
    {
        string? enviado = null;
        var grupo = Guid.NewGuid();
        var api = Api();
        api.Setup(a => a.GuardarConfiguracionInformeAsync(categoria, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, filtros, _) => enviado = filtros)
            .ReturnsAsync(ApiCallResult<ConfiguracionInformeResponse>.Ok(new(), "Guardado."));
        var sut = Crear(api.Object);

        await sut.GuardarPeriodo(categoria, "15", null, null, "elegidos", [grupo], CancellationToken.None);

        var filtros = JsonSerializer.Deserialize<FiltrosPeriodoInforme>(enviado!, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        filtros.Periodo.Should().Be("15");
        filtros.Grupos.Should().Equal(grupo);
        sut.TempData["AvisoModal"].Should().Be("Guardado.");
    }

    [Fact]
    public async Task Guardar_periodo_con_rango_mayor_a_31_dias_no_llama_la_api()
    {
        var api = Api();
        var sut = Crear(api.Object);

        await sut.GuardarPeriodo(CategoriasInforme.Egresos, "rango", new DateOnly(2026, 8, 1), new DateOnly(2026, 10, 1), "todos", null, CancellationToken.None);

        api.Verify(a => a.GuardarConfiguracionInformeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        sut.TempData["AvisoModal"].Should().Be(UiTexts.InformeRangoMaximo);
    }

    [Fact]
    public async Task Guardar_periodo_rechaza_una_categoria_sin_periodo()
    {
        var api = Api();
        var sut = Crear(api.Object);

        await sut.GuardarPeriodo(CategoriasInforme.Vendedores, "7", null, null, "todos", null, CancellationToken.None);

        api.Verify(a => a.GuardarConfiguracionInformeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        sut.TempData["AvisoModal"].Should().Be(UiTexts.InformeCategoriaInvalida);
    }

    [Fact]
    public async Task Index_con_recaudo_guardado_arma_solo_el_informe_de_recaudo()
    {
        var api = Api();
        api.Setup(a => a.ListarConfiguracionesInformeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<ConfiguracionInformeResponse>>.Ok(
                [new() { Categoria = CategoriasInforme.Recaudo, Filtros = """{"periodo":"hoy"}""" }], "Listo."));
        api.Setup(a => a.TableroRecaudoAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<TableroRecaudoResponse>.Ok(new() { TotalRecaudado = 80m, TotalPendiente = 20m }, "Listo."));
        var sut = Crear(api.Object);

        var result = await sut.Index(null, null, cancellationToken: CancellationToken.None);

        var modelo = result.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<InformesViewModel>().Subject;
        modelo.InformeRecaudo!.Recaudo!.Recaudado.Should().Be(80m);
        modelo.InformeVentas.Should().BeNull();
        modelo.InformeEgresos.Should().BeNull();
        modelo.Tarjetas.Single(t => t.Clave == CategoriasInforme.Recaudo).Configurada.Should().BeTrue();
        api.Verify(a => a.ConsultarKpiAsync(It.IsAny<KpiRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Quitar_saca_la_categoria_del_informe()
    {
        var api = Api();
        api.Setup(a => a.QuitarConfiguracionInformeAsync(CategoriasInforme.Vendedores, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<object>.Ok(new object(), "Quitado."));
        var sut = Crear(api.Object);

        var result = await sut.Quitar(CategoriasInforme.Vendedores, CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Index");
        api.Verify(a => a.QuitarConfiguracionInformeAsync(CategoriasInforme.Vendedores, It.IsAny<CancellationToken>()), Times.Once);
        sut.TempData["AvisoModal"].Should().Be("Quitado.");
    }

    private static Mock<IAdminApiClient> Api()
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(a => a.ListarConfiguracionesInformeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<ConfiguracionInformeResponse>>.Ok([], "Listo."));
        api.Setup(a => a.ListarUsuariosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<UsuarioResponse>>.Ok(
            [
                new() { UsuarioId = Guid.NewGuid(), NombreCompleto = "Ana", Usuario = "ana", Rol = RolUsuario.Vendedor },
                new() { UsuarioId = Guid.NewGuid(), NombreCompleto = "Dora", Usuario = "dora", Rol = RolUsuario.Administrador }
            ], "Listo."));
        api.Setup(a => a.ListarGruposAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<GrupoResponse>>.Ok([], "Listo."));
        return api;
    }

    private static InformesController Crear(IAdminApiClient api)
    {
        var http = new DefaultHttpContext();
        return new InformesController(api)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        };
    }
}
