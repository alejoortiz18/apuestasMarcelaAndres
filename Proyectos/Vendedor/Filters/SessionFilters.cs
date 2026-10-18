using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NewRich.Vendedor.Services;

namespace NewRich.Vendedor.Filters;

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
