using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Constants.Messages;
using NewRich.Shared.Results;

namespace NewRich.Api.Filters;

public sealed class SesionYPasswordActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (http.User.Identity?.IsAuthenticated != true)
        {
            await next();
            return;
        }

        var path = http.Request.Path.Value ?? string.Empty;
        var db = http.RequestServices.GetRequiredService<INewRichDbContext>();
        var sesionClaim = http.User.FindFirst("sesionId")?.Value;
        if (!Guid.TryParse(sesionClaim, out var sesionId))
        {
            context.Result = new ObjectResult(ApiResponse.From(Result.Fail(AuthMessages.SesionInvalida, 401))) { StatusCode = 401 };
            return;
        }

        var sesion = await db.Sesiones.FirstOrDefaultAsync(s => s.SesionId == sesionId);
        if (sesion is null || !sesion.Activa || sesion.FechaExpiracion < DateTime.UtcNow)
        {
            context.Result = new ObjectResult(ApiResponse.From(Result.Fail(AuthMessages.SesionInvalida, 401))) { StatusCode = 401 };
            return;
        }

        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == sesion.UsuarioId);
        if (usuario is null)
        {
            context.Result = new ObjectResult(ApiResponse.From(Result.Fail(AuthMessages.SesionInvalida, 401))) { StatusCode = 401 };
            return;
        }

        if (usuario.EstadoValidado &&
            !path.Contains("/api/auth/cambiar-password", StringComparison.OrdinalIgnoreCase) &&
            !path.Contains("/api/auth/logout", StringComparison.OrdinalIgnoreCase) &&
            !path.Contains("/api/authandroid/cambiarpasswordmob", StringComparison.OrdinalIgnoreCase) &&
            !path.Contains("/api/authandroid/logoutmob", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new ObjectResult(ApiResponse.From(Result.Fail(AuthMessages.DebeCambiarPassword, 403))) { StatusCode = 403 };
            return;
        }

        await next();
    }
}
