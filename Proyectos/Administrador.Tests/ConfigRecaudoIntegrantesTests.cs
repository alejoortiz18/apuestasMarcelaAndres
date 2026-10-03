using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NewRich.Admin.Controllers;
using NewRich.Admin.Models;
using NewRich.Admin.Services;

namespace NewRich.Admin.Tests;

public sealed class ConfigRecaudoIntegrantesTests
{
    [Fact]
    public async Task Eliminar_del_grupo_quita_al_vendedor_y_vuelve_a_los_integrantes()
    {
        var grupo = Guid.NewGuid();
        var vendedor = Guid.NewGuid();
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.DesasignarVendedorGrupoAsync(vendedor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<object>.Ok(new object(), "Listo."));
        var sut = Crear(api.Object);

        var resultado = await sut.EliminarDelGrupo(grupo, vendedor, "2026-09-28", "2026-09-28", CancellationToken.None);

        api.Verify(x => x.DesasignarVendedorGrupoAsync(vendedor, It.IsAny<CancellationToken>()), Times.Once);
        var redireccion = resultado.Should().BeOfType<RedirectToActionResult>().Subject;
        redireccion.ActionName.Should().Be("Ver");
        redireccion.RouteValues!["id"].Should().Be(grupo);
        sut.TempData.Should().ContainKey("AvisoModal");
    }

    [Fact]
    public async Task Eliminar_del_grupo_muestra_el_error_de_la_api()
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.DesasignarVendedorGrupoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<object>.Fail("Confirma la contraseña.", 403));
        var sut = Crear(api.Object);

        await sut.EliminarDelGrupo(Guid.NewGuid(), Guid.NewGuid(), null, null, CancellationToken.None);

        sut.TempData["AvisoModal"].Should().Be("Confirma la contraseña.");
    }

    [Fact]
    public void La_vista_de_integrantes_tiene_eliminar_del_grupo_en_el_panel_inferior()
    {
        var vista = File.ReadAllText(RutaVista());
        var panel = vista.IndexOf("data-table-panel=\"integrantes-grupo-recaudo\"", StringComparison.Ordinal);

        panel.Should().BeGreaterThan(vista.IndexOf("<partial name=\"_Pager\"", StringComparison.Ordinal));
        var acciones = vista[panel..];
        acciones.Should().Contain("UiTexts.EliminarDelGrupo");
        acciones.Should().Contain("data-needs-selection");
        acciones.Should().Contain("Url.Action(\"EliminarDelGrupo\"");
        acciones.Should().Contain("data-protected-action=\"@AccionesProtegidas.GruposQuitar\"");
    }

    [Theory]
    [InlineData("NombreCompleto")]
    [InlineData("Identificador")]
    [InlineData("Recaudador")]
    [InlineData("Porcentaje")]
    [InlineData("TotalVendido")]
    [InlineData("TotalPorRecaudar")]
    [InlineData("TotalRecaudado")]
    [InlineData("TotalPendiente")]
    [InlineData("Estado")]
    public void Cada_columna_de_integrantes_tiene_un_tooltip_que_la_explica(string columna)
    {
        var vista = File.ReadAllText(RutaVista());

        vista.Should().Contain($"Encabezado(UiTexts.{columna}, UiTexts.AyudaColumna{columna})");
        typeof(NewRich.Admin.Constants.UiTexts).GetField($"AyudaColumna{columna}")!.GetValue(null)
            .Should().BeOfType<string>().Which.Should().NotBeNullOrWhiteSpace();
        vista.Should().Contain("role=\"tooltip\"");
    }

    [Theory]
    [InlineData("Recaudador")]
    [InlineData("Porcentaje")]
    [InlineData("TotalPorRecaudar")]
    [InlineData("TotalRecaudado")]
    [InlineData("TotalPendiente")]
    public void Cada_tarjeta_del_grupo_tiene_un_signo_que_explica_su_valor(string tarjeta)
    {
        var vista = File.ReadAllText(RutaVista());

        vista.Should().Contain($"Tarjeta(UiTexts.{tarjeta}, UiTexts.AyudaTarjeta{tarjeta},");
        typeof(NewRich.Admin.Constants.UiTexts).GetField($"AyudaTarjeta{tarjeta}")!.GetValue(null)
            .Should().BeOfType<string>().Which.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void La_tarjeta_total_pendiente_muestra_la_suma_del_grupo()
    {
        var vista = File.ReadAllText(RutaVista());

        vista.Should().Contain("FormatoPesos.De(Model.TotalPendiente)");
    }

    [Fact]
    public async Task Ver_lleva_el_total_pendiente_del_grupo_al_modelo()
    {
        var grupo = Guid.NewGuid();
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.IntegrantesGrupoRecaudoAsync(grupo, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<NewRich.Application.Contracts.Recaudo.IntegrantesGrupoRecaudoResponse>.Ok(new()
            {
                GrupoId = grupo,
                Nombre = "Grupo Centro",
                TotalPendiente = 95000m
            }, "Listo."));
        var sut = Crear(api.Object);

        var vista = await sut.Ver(grupo, null, null, cancellationToken: CancellationToken.None) as ViewResult;

        vista!.Model.Should().BeOfType<IntegrantesGrupoRecaudoViewModel>().Which.TotalPendiente.Should().Be(95000m);
    }

    private static ConfigRecaudoController Crear(IAdminApiClient api)
    {
        var http = new DefaultHttpContext();
        return new ConfigRecaudoController(api)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        };
    }

    private static string RutaVista()
    {
        var ruta = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Administrador", "Views", "ConfigRecaudo", "Ver.cshtml"));
        File.Exists(ruta).Should().BeTrue($"se esperaba la vista en {ruta}");
        return ruta;
    }
}
