using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NewRich.Admin.Controllers;
using NewRich.Admin.Models;
using NewRich.Admin.Services;

namespace NewRich.Admin.Tests;

public sealed class ConfigRecaudoVendedoresSinGrupoTests
{
    [Fact]
    public async Task Guardar_grupos_asigna_solo_los_vendedores_con_grupo_elegido()
    {
        var ana = Guid.NewGuid();
        var beto = Guid.NewGuid();
        var centro = Guid.NewGuid();
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.AsignarVendedorGrupoAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<object>.Ok(new object(), "Listo."));
        var sut = Crear(api.Object);

        var resultado = await sut.GuardarGruposVendedores(
            [
                new AsignacionGrupoVendedor { VendedorId = ana, GrupoId = centro },
                new AsignacionGrupoVendedor { VendedorId = beto, GrupoId = null }
            ],
            "2026-09-28",
            "2026-09-28",
            CancellationToken.None);

        api.Verify(x => x.AsignarVendedorGrupoAsync(centro, ana, It.IsAny<CancellationToken>()), Times.Once);
        api.Verify(x => x.AsignarVendedorGrupoAsync(It.IsAny<Guid>(), beto, It.IsAny<CancellationToken>()), Times.Never);
        resultado.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Index");
        sut.TempData.Should().ContainKey("AvisoModal");
    }

    [Fact]
    public async Task Guardar_grupos_muestra_el_error_de_la_api()
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.AsignarVendedorGrupoAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<object>.Fail("Confirma la contraseña.", 403));
        var sut = Crear(api.Object);

        await sut.GuardarGruposVendedores(
            [new AsignacionGrupoVendedor { VendedorId = Guid.NewGuid(), GrupoId = Guid.NewGuid() }],
            null,
            null,
            CancellationToken.None);

        sut.TempData["AvisoModal"].Should().Be("Confirma la contraseña.");
    }

    [Fact]
    public void La_tabla_de_vendedores_sin_grupo_asigna_grupo_y_no_retira_asignacion()
    {
        var vista = File.ReadAllText(RutaVista());
        var tabla = vista.IndexOf("data-table-select=\"vendedores-recaudo\"", StringComparison.Ordinal);
        var seccion = vista[tabla..];

        seccion.Should().NotContain("RetirarVendedor");
        seccion.Should().NotContain("UiTexts.RetirarAsignacion");
        seccion.Should().Contain(">@UiTexts.AsignarUnGrupo</th>");
        seccion.Should().Contain("name=\"asignaciones[@j].GrupoId\"");
        seccion.Should().Contain("class=\"searchable\"");
        seccion.Should().Contain("asp-action=\"GuardarGruposVendedores\"");
        seccion.Should().Contain("data-protected-action=\"@AccionesProtegidas.GruposAsignar\"");
        seccion.Should().Contain("data-guardar-grupos");
        seccion.Should().Contain("data-cancelar-grupos");
        seccion.Should().MatchRegex(@"@\{\s*ViewData\[""PagerPageKey""\] = ""pageV"";");
    }

    [Fact]
    public async Task Asignar_grupo_solo_ofrece_grupos_sin_recaudador()
    {
        var centro = Guid.NewGuid();
        var norte = Guid.NewGuid();
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.ConfiguracionRecaudoAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<NewRich.Application.Contracts.Recaudo.ConfiguracionRecaudoResponse>.Ok(new()
            {
                Grupos =
                [
                    new() { GrupoId = centro, Nombre = "Grupo Centro", RecaudadorId = Guid.NewGuid(), RecaudadorNombre = "recardo1" },
                    new() { GrupoId = norte, Nombre = "Grupo Norte", SinConfigurar = true }
                ]
            }, "Listo."));
        api.Setup(x => x.ListarUsuariosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<NewRich.Application.Contracts.Usuarios.UsuarioResponse>>.Ok([], "Listo."));
        var sut = Crear(api.Object);

        var vista = await sut.Index(null, null, cancellationToken: CancellationToken.None) as ViewResult;

        var modelo = vista!.Model.Should().BeOfType<ConfigRecaudoViewModel>().Subject;
        modelo.GruposSinRecaudador.Should().ContainSingle(g => g.Id == norte);
        modelo.CatalogoGrupos.Should().HaveCount(2);
        modelo.Grupos.Items.Should().Contain(g => g.GrupoId == centro && g.RecaudadorNombre == "recardo1");
    }

    [Fact]
    public void El_formulario_asignar_grupo_usa_los_grupos_sin_recaudador()
    {
        var vista = File.ReadAllText(RutaVista());
        var formulario = vista[vista.IndexOf("asp-action=\"AsignarGrupo\"", StringComparison.Ordinal)..vista.IndexOf("@UiTexts.AyudaPorcentaje", StringComparison.Ordinal)];

        formulario.Should().Contain("Model.GruposSinRecaudador");
        formulario.Should().NotContain("Model.CatalogoGrupos");
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
            "Administrador", "Views", "ConfigRecaudo", "Index.cshtml"));
        File.Exists(ruta).Should().BeTrue($"se esperaba la vista en {ruta}");
        return ruta;
    }
}
