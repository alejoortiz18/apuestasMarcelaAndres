using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NewRich.Admin.Constants;
using NewRich.Admin.Controllers;
using NewRich.Admin.Services;
using NewRich.Admin.Services.Usb;

namespace NewRich.Admin.Tests;

public sealed class InstalacionesControllerTests
{
    [Fact]
    public void Index_marca_el_menu_de_instalaciones()
    {
        var sut = Crear();

        var result = sut.Index();

        result.Should().BeOfType<ViewResult>();
        sut.ViewData["Nav"].Should().Be("instalaciones");
        sut.ViewData["Crumb"].Should().Be(UiTexts.NavInstalaciones);
    }

    [Fact]
    public void Instalar_envia_al_asistente_de_registrar_pda()
    {
        var sut = Crear();

        var result = sut.Instalar().Should().BeOfType<RedirectToActionResult>().Subject;

        result.ActionName.Should().Be("Crear");
        result.ControllerName.Should().Be("Dispositivos");
        result.RouteValues!["desde"].Should().Be("instalaciones");
    }

    private static InstalacionesController Crear() =>
        new(
            Mock.Of<IAdminApiClient>(),
            Mock.Of<IInventarioUsb>(),
            Mock.Of<IPreparadorLlaveUsb>());
}
