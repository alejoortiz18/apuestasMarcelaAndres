using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Notificaciones;

namespace NewRich.Admin.ViewComponents;

public sealed class ChatAvisosViewComponent : ViewComponent
{
    private readonly IAdminApiClient _api;

    public ChatAvisosViewComponent(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var result = await _api.ListarAvisosChatAsync(HttpContext.RequestAborted);
        return View(result.Data ?? new AvisosChatResponse());
    }
}
