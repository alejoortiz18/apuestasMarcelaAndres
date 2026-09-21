using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NewRich.Application.Abstractions;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Shared.Results;

namespace NewRich.Api.Filters;

public sealed class RequiereConfirmacionFilter : IAsyncActionFilter
{
    public const string HeaderToken = ConfirmacionAccion.HeaderToken;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var meta = context.ActionDescriptor.EndpointMetadata.OfType<RequiereConfirmacionAttribute>().FirstOrDefault();
        if (meta is null)
        {
            await next();
            return;
        }

        var http = context.HttpContext;
        if (http.User.Identity?.IsAuthenticated != true || !http.User.IsInRole("Administrador"))
        {
            await next();
            return;
        }

        var usuarioClaim = http.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        if (!Guid.TryParse(usuarioClaim, out var usuarioId))
        {
            context.Result = new ObjectResult(ApiResponse.From(Result.Fail(AuthMessages.SesionInvalida, 401))) { StatusCode = 401 };
            return;
        }

        if (!http.Request.Headers.TryGetValue(HeaderToken, out var valores) || string.IsNullOrWhiteSpace(valores.ToString()))
        {
            context.Result = new ObjectResult(ApiResponse.From(Result.Fail(AuthMessages.ConfirmacionAccionRequerida, 403))) { StatusCode = 403 };
            return;
        }

        var store = http.RequestServices.GetRequiredService<IConfirmacionAccionStore>();
        if (!meta.Acciones.Any(accion => store.Consumir(valores.ToString(), usuarioId, accion)))
        {
            context.Result = new ObjectResult(ApiResponse.From(Result.Fail(AuthMessages.ConfirmacionAccionInvalida, 403))) { StatusCode = 403 };
            return;
        }

        await next();
    }
}
