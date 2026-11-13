using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using NewRich.Admin.Constants;
using NewRich.Admin.Controllers;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Admin.Services.Usb;
using NewRich.Application.Contracts.Auth;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Tests;

public sealed class UtilidadesMenuRutaTests
{
    [Fact]
    public void El_menu_de_contabilidad_abre_utilidades_en_indicadores_y_no_en_resumen()
    {
        var layout = File.ReadAllText(RutaLayout());
        var bloque = Extraer(layout, "data-nav-group=\"utilidades\"", "</div>\r\n            <a class=\"nav-parent @NavClass(\"kpi\")\"");
        if (bloque.Length == 0)
        {
            bloque = Extraer(layout, "data-nav-group=\"utilidades\"", "</div>\n            <a class=\"nav-parent @NavClass(\"kpi\")\"");
        }

        bloque.Should().Contain("href=\"/Utilidades/Indicadores\"");
        bloque.Should().Contain("NavUtilidadesIndicadores");
        bloque.Should().NotContain("asp-action=\"Resumen\"");
        bloque.Should().NotContain("href=\"/Utilidades/Resumen\"");
    }

    [Fact]
    public async Task La_ruta_vieja_de_resumen_abre_la_misma_vista_de_utilidades()
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.ListarGruposAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<NewRich.Application.Contracts.Grupos.GrupoResponse>>.Ok([], "Listo."));
        api.Setup(x => x.ConsultarKpiAsync(It.IsAny<NewRich.Application.Contracts.Kpi.KpiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<NewRich.Application.Contracts.Kpi.KpiResponse>.Ok(new NewRich.Application.Contracts.Kpi.KpiResponse(), "Listo."));
        api.Setup(x => x.TotalVentasRegistradasWebAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<NewRich.Application.Contracts.VentasRegistradasWeb.TotalVentasRegistradasWebResponse>.Ok(new NewRich.Application.Contracts.VentasRegistradasWeb.TotalVentasRegistradasWebResponse(0, 0, 0, 0), "Listo."));
        api.Setup(x => x.ListadoPremiosEntregadosWebAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<NewRich.Application.Contracts.PremiosEntregadosWeb.PremioEntregadoWebResponse>>.Ok([], "Listo."));
        api.Setup(x => x.UtilidadesIndicadoresAsync(It.IsAny<string>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<NewRich.Application.Contracts.Utilidades.InformeGastosPropiosResponse>.Ok(
                new NewRich.Application.Contracts.Utilidades.InformeGastosPropiosResponse(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 7), 0m, 0, 0m, null, null, null, 0m, 0, [], [], 0m, 0m), "Listo."));
        api.Setup(x => x.RevisadaCubiertoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<NewRich.Application.Contracts.RevisadaManual.VistaRevisadaManualResponse>.Ok(new NewRich.Application.Contracts.RevisadaManual.VistaRevisadaManualResponse(), "Listo."));
        api.Setup(x => x.CubrirVentaCubiertoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<Guid>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<NewRich.Application.Contracts.RevisadaManual.VistaRevisadaManualResponse>.Ok(new NewRich.Application.Contracts.RevisadaManual.VistaRevisadaManualResponse(), "Listo."));
        var http = new DefaultHttpContext();
        var sut = new UtilidadesController(api.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(http, Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>())
        };

        var resultado = await sut.Resumen(null, null, null);

        var vista = resultado.Should().BeOfType<ViewResult>().Subject;
        vista.ViewName.Should().Be("Indicadores");
        vista.Model.Should().BeOfType<UtilidadesVentasViewModel>();
        sut.ViewData["Nav"].Should().Be("utilidades-indicadores");
    }

    [Fact]
    public async Task Tras_ingresar_con_return_url_de_utilidades_vuelve_a_utilidades_y_no_al_resumen()
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(a => a.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<LoginResponse>.Ok(new LoginResponse
            {
                Token = "t",
                NombreUsuario = "super",
                Rol = RolUsuario.Administrador,
                DebeCambiarPassword = false
            }, "Listo."));
        var sesion = new Mock<IAdminSessionService>();
        var inventario = new Mock<IInventarioUsb>();
        inventario.Setup(i => i.Listar()).Returns([]);
        var controlador = new CuentaController(api.Object, sesion.Object, inventario.Object, Mock.Of<ILectorLlaveUsb>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.IsLocalUrl("/Utilidades/Indicadores")).Returns(true);
        controlador.Url = url.Object;

        var resultado = await controlador.Ingresar(new LoginViewModel
        {
            Usuario = "super",
            Password = "Secreta1!",
            ReturnUrl = "/Utilidades/Indicadores"
        }, CancellationToken.None);

        var redireccion = resultado.Should().BeOfType<LocalRedirectResult>().Subject;
        redireccion.Url.Should().Be("/Utilidades/Indicadores");
    }

    [Fact]
    public async Task Un_administrador_con_sesion_y_return_url_de_utilidades_no_cae_en_resumen()
    {
        var sesion = new Mock<IAdminSessionService>();
        var inventario = new Mock<IInventarioUsb>();
        inventario.Setup(i => i.Listar()).Returns([]);
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "super"),
            new Claim(ClaimTypes.Role, nameof(RolUsuario.Administrador)),
            new Claim("debeCambiarPassword", "false")
        ], AuthCookieNames.Scheme));
        var controlador = new CuentaController(
            Mock.Of<IAdminApiClient>(),
            sesion.Object,
            inventario.Object,
            Mock.Of<ILectorLlaveUsb>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = usuario } }
        };
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.IsLocalUrl("/Utilidades/Indicadores")).Returns(true);
        controlador.Url = url.Object;

        var resultado = await controlador.Ingresar(returnUrl: "/Utilidades/Indicadores");

        var redireccion = resultado.Should().BeOfType<LocalRedirectResult>().Subject;
        redireccion.Url.Should().Be("/Utilidades/Indicadores");
    }

    private static string RutaLayout() => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "Administrador", "Views", "Shared", "_Layout.cshtml"));

    private static string Extraer(string texto, string desde, string hasta)
    {
        var i = texto.IndexOf(desde, StringComparison.Ordinal);
        if (i < 0)
        {
            return string.Empty;
        }

        var j = texto.IndexOf(hasta, i, StringComparison.Ordinal);
        return j < 0 ? texto[i..] : texto[i..j];
    }
}
