using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;

namespace NewRich.Admin.Controllers;

public sealed class OfflineController : AdminControllerBase
{
    public IActionResult Index()
    {
        SetNav("offline", UiTexts.NavOffline);
        return View();
    }
}

public sealed class PremiosController : AdminControllerBase
{
    public IActionResult Index()
    {
        SetNav("premios", UiTexts.NavPremios);
        return View();
    }
}
