using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;

namespace NewRich.Admin.Controllers;

public sealed class InstalacionesController : AdminControllerBase
{
    public IActionResult Index()
    {
        SetNav("instalaciones", UiTexts.NavInstalaciones);
        return View();
    }

    public IActionResult Instalar()
    {
        return RedirectToAction("Crear", "Dispositivos", new { desde = "instalaciones" });
    }
}
