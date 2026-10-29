using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Notificaciones;
using NewRich.Vendedor.Services;

namespace NewRich.Vendedor.ViewComponents;

public sealed class ChatAvisosViewComponent : ViewComponent
{
    private readonly IVendedorApiClient _api;

    public ChatAvisosViewComponent(IVendedorApiClient api)
    {
        _api = api;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var result = await _api.AvisosChatAsync(HttpContext.RequestAborted);
        return View(result.Data ?? new AvisosChatResponse());
    }
}
