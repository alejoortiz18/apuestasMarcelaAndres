using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Controllers;

namespace NewRich.Admin.Tests;

public sealed class InformesControllerTests
{
    [Fact]
    public void Index_muestra_la_pagina_y_marca_el_menu_de_informes()
    {
        var sut = new InformesController();

        var result = sut.Index();

        result.Should().BeOfType<ViewResult>();
        sut.ViewData["Nav"].Should().Be("informes");
        sut.ViewData["Title"].Should().Be(UiTexts.NavInformes);
    }
}
