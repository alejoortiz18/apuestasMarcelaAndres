using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NewRich.Admin.Controllers;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Admin.Services.Usb;

namespace NewRich.Admin.Tests;

public sealed class InstalacionesControllerTests
{
    [Fact]
    public async Task Llave_marca_el_subitem_instalaciones_del_menu()
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(a => a.ListarAdministradoresLlaveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<NewRich.Application.Services.UsuarioResponseMini>>.Ok([], "Listo."));
        var sut = new InstalacionesController(api.Object, Mock.Of<IInventarioUsb>(), Mock.Of<IPreparadorLlaveUsb>());

        var result = await sut.Llave(CancellationToken.None);

        result.Should().BeOfType<ViewResult>();
        sut.ViewData["Nav"].Should().Be("pda");
    }
}
