using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NewRich.Admin.Constants;
using NewRich.Admin.Services;

namespace NewRich.Admin.Filters;

/// <summary>
/// El ingreso valida el formulario, pero un token de una publicación anterior no debe
/// dejar la pantalla en blanco con HTTP 400. Vuelve a mostrar el ingreso.
/// </summary>
public sealed class ValidarFormularioIngresoAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new RedirectToActionResult("Ingresar", "Cuenta", null);
        }
    }
}

public sealed class DebeCambiarPasswordFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            await next();
            return;
        }

        var controller = context.RouteData.Values["controller"]?.ToString();
        var action = context.RouteData.Values["action"]?.ToString();
        var permitido = string.Equals(controller, "Cuenta", StringComparison.OrdinalIgnoreCase)
                        && (string.Equals(action, "CambiarPassword", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(action, "Salir", StringComparison.OrdinalIgnoreCase));

        if (user.DebeCambiarPassword() && !permitido)
        {
            context.Result = new RedirectToActionResult("CambiarPassword", "Cuenta", null);
            return;
        }

        await next();
    }
}

public sealed class ApiUnauthorizedFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();
        if (executed.Result is ViewResult view
            && view.ViewData["ApiUnauthorized"] is true)
        {
            executed.Result = new RedirectToActionResult("Ingresar", "Cuenta", new { expired = true });
        }
    }
}
