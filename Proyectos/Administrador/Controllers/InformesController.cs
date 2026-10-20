using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;

namespace NewRich.Admin.Controllers;

public sealed class InformesController : AdminControllerBase
{
    public IActionResult Index()
    {
        SetNav("informes", UiTexts.NavInformes);
        return View();
    }
}
