using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NewRich.Admin.Controllers;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Chat;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Tests;

public sealed class SoporteElegirPersonaTests
{
    private static readonly Guid Laura = Guid.NewGuid();

    [Fact]
    public async Task Elegir_una_persona_con_chat_abre_su_conversacion()
    {
        var conversacionId = Guid.NewGuid();
        var api = Api([new ConversacionResponse { ConversacionId = conversacionId, UsuarioIniciadorId = Laura, UsuarioDestinoId = Guid.NewGuid() }]);
        var sut = Controlador(api);

        var resultado = await sut.Index(null, CancellationToken.None, Laura);

        var destino = resultado.Should().BeOfType<RedirectToActionResult>().Subject;
        destino.ActionName.Should().Be("Index");
        destino.RouteValues!["id"].Should().Be(conversacionId);
    }

    [Fact]
    public async Task Elegir_una_persona_sin_chat_la_deja_lista_para_escribir()
    {
        var api = Api([new ConversacionResponse { ConversacionId = Guid.NewGuid(), UsuarioIniciadorId = Guid.NewGuid(), UsuarioDestinoId = Guid.NewGuid() }]);
        var sut = Controlador(api);

        var resultado = await sut.Index(null, CancellationToken.None, Laura);

        var vista = (SoporteIndexViewModel)((ViewResult)resultado).Model!;
        vista.NuevoDestino!.UsuarioId.Should().Be(Laura);
        vista.Detalle.Should().BeNull();
        vista.ConversacionId.Should().BeNull();
        api.Verify(a => a.ObtenerConversacionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IAdminApiClient> Api(List<ConversacionResponse> conversaciones)
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(a => a.ListarConversacionesAsync(It.IsAny<CancellationToken>(), "AtencionCliente"))
            .ReturnsAsync(ApiCallResult<List<ConversacionResponse>>.Ok(conversaciones, "Listo."));
        api.Setup(a => a.ListarUsuariosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<UsuarioResponse>>.Ok(
                [new UsuarioResponse { UsuarioId = Laura, NombreCompleto = "Laura Gil", Rol = RolUsuario.VendedorWeb }], "Listo."));
        return api;
    }

    private static SoporteController Controlador(Mock<IAdminApiClient> api)
    {
        var http = new DefaultHttpContext();
        return new SoporteController(api.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        };
    }
}
