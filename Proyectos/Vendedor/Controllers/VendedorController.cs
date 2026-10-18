using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Domain.Services;
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

    protected static bool HorarioCerrado(ConfiguracionOperativaResponse? operativa)
    {
        if (operativa is null
            || !TimeSpan.TryParse(operativa.HoraApertura, out var apertura)
            || !TimeSpan.TryParse(operativa.HoraCierre, out var cierre))
        {
            return false;
        }

        var ahora = ZonaHorariaColombia.ALocal(DateTime.UtcNow).TimeOfDay;
        return HorarioOperacion.EstaFuera(ahora, apertura, cierre);
    }

    protected static int MaximoLineas(ConfiguracionOperativaResponse? operativa, bool combo) =>
        operativa is null
            ? 1
            : combo
                ? Math.Max(1, operativa.MaxJuegosCombinado)
                : Math.Max(1, operativa.MaxLineasIndividual);
}
