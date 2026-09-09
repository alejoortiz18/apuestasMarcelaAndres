using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;

namespace NewRich.Admin.Controllers;

[Authorize(Roles = "Administrador")]
[Authorize(AuthenticationSchemes = AuthCookieNames.Scheme)]
public abstract class AdminControllerBase : Controller
{
    protected IActionResult? RedirectIfUnauthorized<T>(ApiCallResult<T> result)
    {
        if (result.Unauthorized)
        {
            return RedirectToAction("Ingresar", "Cuenta", new { expired = true });
        }

        return null;
    }

    protected void SetFlash(string message, bool success = true)
    {
        TempData[success ? "FlashOk" : "FlashError"] = message;
    }

    protected void SetNav(string key, string crumb)
    {
        ViewData["Nav"] = key;
        ViewData["Crumb"] = crumb;
        ViewData["Title"] = crumb;
    }
}
