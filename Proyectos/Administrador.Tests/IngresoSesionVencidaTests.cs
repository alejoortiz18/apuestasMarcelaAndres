using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NewRich.Admin.Constants;
using NewRich.Admin.Controllers;
using NewRich.Admin.Services;
using NewRich.Admin.Services.Usb;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Tests;

public sealed class IngresoSesionVencidaTests
{
    [Fact]
    public async Task Una_sesion_que_la_api_rechazo_muestra_el_ingreso_y_no_vuelve_al_inicio()
    {
        var sesion = new Mock<IAdminSessionService>();
        var controlador = CrearControlador(sesion);

        var resultado = await controlador.Ingresar(expired: true);

        resultado.Should().BeOfType<ViewResult>();
        sesion.Verify(s => s.SignOutAsync(It.IsAny<HttpContext>()), Times.Once);
    }

    [Fact]
    public async Task Un_administrador_con_sesion_vigente_sigue_entrando_al_inicio()
    {
        var sesion = new Mock<IAdminSessionService>();
        var controlador = CrearControlador(sesion);

        var resultado = await controlador.Ingresar();

        var redireccion = resultado.Should().BeOfType<RedirectToActionResult>().Subject;
        redireccion.ControllerName.Should().Be("Inicio");
        sesion.Verify(s => s.SignOutAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    private static CuentaController CrearControlador(Mock<IAdminSessionService> sesion)
    {
        var inventario = new Mock<IInventarioUsb>();
        inventario.Setup(i => i.Listar()).Returns([]);
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "super"),
            new Claim(ClaimTypes.Role, nameof(RolUsuario.Administrador)),
            new Claim("debeCambiarPassword", "false")
        ], AuthCookieNames.Scheme));

        return new CuentaController(
            Mock.Of<IAdminApiClient>(),
            sesion.Object,
            inventario.Object,
            Mock.Of<ILectorLlaveUsb>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = usuario } }
        };
    }
}
