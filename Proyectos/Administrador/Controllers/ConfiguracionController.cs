using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Constants.Messages;

namespace NewRich.Admin.Controllers;

public sealed class ConfiguracionController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public ConfiguracionController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        SetNav("configuracion", UiTexts.NavConfiguracion);
        var result = await _api.ListarConfiguracionesAsync(cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        return View(new ConfiguracionIndexViewModel { Items = result.Data ?? [] });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(string clave, string valor, CancellationToken cancellationToken)
    {
        var result = await _api.ActualizarConfiguracionAsync(clave, valor, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        SetFlash(result.Success ? SuccessMessages.RegistroActualizado : result.Message, result.Success);
        return RedirectToAction(nameof(Index));
    }
}
