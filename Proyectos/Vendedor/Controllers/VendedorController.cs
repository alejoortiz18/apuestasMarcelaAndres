using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Vendedor.Constants;
using NewRich.Vendedor.Models;
using NewRich.Vendedor.Services;

namespace NewRich.Vendedor.Controllers;

[Authorize(AuthenticationSchemes = AuthCookieNames.Scheme, Roles = "Vendedor")]
public abstract class VendedorController : Controller
{
    protected void Nav(string clave, string titulo)
    {
        ViewData["Nav"] = clave;
        ViewData["Title"] = titulo;
    }

    protected void Aviso(string? texto)
    {
        if (!string.IsNullOrWhiteSpace(texto))
        {
            TempData["AvisoModal"] = texto;
        }
    }

    protected IActionResult? SiNoAutorizado<T>(ApiCallResult<T> result)
    {
        if (!result.Unauthorized)
        {
            return null;
        }

        return RedirectToAction("Ingresar", "Cuenta", new { expired = true });
    }

    protected static bool HorarioCerrado(ConfiguracionOperativaResponse? operativa) => false;

    protected static int MaximoLineas(ConfiguracionOperativaResponse? operativa, bool combo) =>
        operativa is null
            ? 1
            : combo
                ? Math.Max(1, operativa.MaxJuegosCombinado)
                : Math.Max(1, operativa.MaxLineasIndividual);
}
