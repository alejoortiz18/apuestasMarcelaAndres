using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Auth;
using NewRich.Admin.Services;
using NewRich.Constants.Messages;

namespace NewRich.Admin.Controllers;

public sealed class SeguridadController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public SeguridadController(IAdminApiClient api)
    {
        _api = api;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar(string password, string accion, int usos = 1, CancellationToken cancellationToken = default)
    {
        var result = await _api.ConfirmarAccionAsync(new ConfirmarAccionRequest
        {
            Password = password ?? string.Empty,
            Accion = accion ?? string.Empty,
            Usos = usos
        }, cancellationToken);

        if (result.Unauthorized)
        {
            return Unauthorized();
        }

        if (!result.Success || result.Data is null || string.IsNullOrWhiteSpace(result.Data.Token))
        {
            return Json(new
            {
                ok = false,
                mensaje = string.IsNullOrWhiteSpace(result.Message)
                    ? AuthMessages.ContrasenaAccionNoValidada
                    : result.Message
            });
        }

        return Json(new { ok = true, token = result.Data.Token });
    }
}
