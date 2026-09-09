using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Models;
using NewRich.Admin.Services;

namespace NewRich.Admin.ViewComponents;

public sealed class CampanaNotificacionesViewComponent : ViewComponent
{
    private const int MaximoEnLista = 8;
    private readonly IAdminApiClient _api;

    public CampanaNotificacionesViewComponent(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var result = await _api.ListarNotificacionesAsync(HttpContext.RequestAborted);
        var items = result.Data?.Items ?? [];
        return View(new CampanaNotificacionesViewModel
        {
            Pendientes = result.Data?.Pendientes ?? 0,
            Nuevas = items.Where(n => !n.Leida).Take(MaximoEnLista).ToList()
        });
    }
}
