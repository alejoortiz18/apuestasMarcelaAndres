using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Controllers;

namespace NewRich.Admin.Tests;

public sealed class InstalacionesControllerTests
{
    [Fact]
    public void Index_marca_el_menu_de_instalaciones()
    {
        var sut = new InstalacionesController();

        var result = sut.Index();

        result.Should().BeOfType<ViewResult>();
        sut.ViewData["Nav"].Should().Be("instalaciones");
        sut.ViewData["Crumb"].Should().Be(UiTexts.NavInstalaciones);
    }

    [Fact]
    public void Instalar_envia_al_asistente_de_registrar_pda()
    {
        var sut = new InstalacionesController();

        var result = sut.Instalar().Should().BeOfType<RedirectToActionResult>().Subject;

        result.ActionName.Should().Be("Crear");
        result.ControllerName.Should().Be("Dispositivos");
        result.RouteValues!["desde"].Should().Be("instalaciones");
    }
}
