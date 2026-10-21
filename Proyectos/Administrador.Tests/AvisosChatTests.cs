using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NewRich.Admin.Controllers;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Admin.ViewComponents;
using NewRich.Application.Contracts.Chat;
using NewRich.Application.Contracts.Notificaciones;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Constants.Messages;

namespace NewRich.Admin.Tests;

public sealed class AvisosChatTests
{
    private static readonly DateTime Fecha = new(2026, 10, 21, 9, 0, 0);

    [Fact]
    public async Task La_campana_no_cuenta_ni_muestra_los_avisos_de_chat()
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(a => a.ListarNotificacionesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<NotificacionesResponse>.Ok(new NotificacionesResponse
            {
                Pendientes = 3,
                Items =
                [
                    Aviso(ChatMessages.TipoAvisoSoporte, "Hugo escribió en soporte."),
                    Aviso(ChatMessages.TipoAvisoSoporteTecnico, "Laura envió un reporte de soporte técnico."),
                    Aviso("ValorAlto", "Venta de valor alto")
                ]
            }, "Listo."));
        var sut = new CampanaNotificacionesViewComponent(api.Object) { ViewComponentContext = Contexto() };

        var result = await sut.InvokeAsync();

        var modelo = result.Should().BeOfType<ViewViewComponentResult>().Subject.ViewData!.Model.Should().BeOfType<CampanaNotificacionesViewModel>().Subject;
        modelo.Pendientes.Should().Be(1);
        modelo.Nuevas.Should().ContainSingle(n => n.Tipo == "ValorAlto");
    }

    [Fact]
    public async Task Abrir_el_chat_de_una_persona_limpia_sus_avisos()
    {
        var conversacionId = Guid.NewGuid();
        var api = new Mock<IAdminApiClient>();
        api.Setup(a => a.ListarConversacionesAsync(It.IsAny<CancellationToken>(), "AtencionCliente"))
            .ReturnsAsync(ApiCallResult<List<ConversacionResponse>>.Ok([new() { ConversacionId = conversacionId }], "Listo."));
        api.Setup(a => a.ListarUsuariosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<UsuarioResponse>>.Ok([], "Listo."));
        api.Setup(a => a.ObtenerConversacionAsync(conversacionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<ConversacionDetalleResponse>.Ok(new() { Conversacion = new() { ConversacionId = conversacionId } }, "Listo."));
        api.Setup(a => a.MarcarAvisosChatLeidosAsync(conversacionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<object>.Ok(new object(), "Listo."));
        var sut = Controlador(new SoporteController(api.Object));

        await sut.Index(conversacionId, CancellationToken.None);

        api.Verify(a => a.MarcarAvisosChatLeidosAsync(conversacionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ver_un_aviso_de_chat_abre_la_conversacion_de_esa_persona()
    {
        var conversacionId = Guid.NewGuid();
        var avisoId = Guid.NewGuid();
        var api = new Mock<IAdminApiClient>();
        api.Setup(a => a.ObtenerNotificacionAsync(avisoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<NotificacionDetalleResponse>.Ok(new()
            {
                NotificacionId = avisoId,
                Tipo = ChatMessages.TipoAvisoSoporte,
                ConversacionId = conversacionId
            }, "Listo."));
        var sut = Controlador(new NotificacionesController(api.Object, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build()));

        var result = await sut.Ver(avisoId, CancellationToken.None);

        var destino = result.Should().BeOfType<RedirectToActionResult>().Subject;
        destino.ControllerName.Should().Be("Soporte");
        destino.RouteValues!["id"].Should().Be(conversacionId);
    }

    private static NotificacionItemResponse Aviso(string tipo, string mensaje) => new()
    {
        NotificacionId = Guid.NewGuid(),
        Tipo = tipo,
        Mensaje = mensaje,
        FechaCreacion = Fecha
    };

    private static ViewComponentContext Contexto()
    {
        var http = new DefaultHttpContext();
        return new ViewComponentContext
        {
            ViewContext = new ViewContext
            {
                HttpContext = http,
                ViewData = new ViewDataDictionary(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
            }
        };
    }

    private static T Controlador<T>(T controlador) where T : Controller
    {
        var http = new DefaultHttpContext();
        controlador.ControllerContext = new ControllerContext { HttpContext = http };
        controlador.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        return controlador;
    }
}
