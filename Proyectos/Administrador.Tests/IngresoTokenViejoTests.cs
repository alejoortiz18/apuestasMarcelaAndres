using FluentAssertions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NewRich.Admin.Filters;

namespace NewRich.Admin.Tests;

public sealed class IngresoTokenViejoTests
{
    [Fact]
    public async Task Un_token_de_una_publicacion_anterior_vuelve_al_formulario()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery
            .Setup(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .ThrowsAsync(new AntiforgeryValidationException("The antiforgery token could not be decrypted."));

        var http = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(antiforgery.Object).BuildServiceProvider()
        };
        var context = new AuthorizationFilterContext(
            new ActionContext(http, new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>());

        await new ValidarFormularioIngresoAttribute().OnAuthorizationAsync(context);

        var redireccion = context.Result.Should().BeOfType<RedirectToActionResult>().Subject;
        redireccion.ControllerName.Should().Be("Cuenta");
        redireccion.ActionName.Should().Be("Ingresar");
    }
}
