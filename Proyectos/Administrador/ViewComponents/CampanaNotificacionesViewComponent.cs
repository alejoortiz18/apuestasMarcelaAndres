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
        var nuevas = (result.Data?.Items ?? []).Where(n => !n.Leida && !AvisosChat.EsChat(n.Tipo)).ToList();
        return View(new CampanaNotificacionesViewModel
        {
            Pendientes = nuevas.Count,
            Nuevas = nuevas.Take(MaximoEnLista).ToList()
        });
    }
}
