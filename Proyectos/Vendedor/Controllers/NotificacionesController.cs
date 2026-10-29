using Microsoft.AspNetCore.Mvc;
using NewRich.Shared;
using NewRich.Vendedor.Constants;
using NewRich.Vendedor.Services;

namespace NewRich.Vendedor.Controllers;

/// <summary>Avisos de chat del encabezado y conexión del navegador a los hubs del API.</summary>
public sealed class NotificacionesController : VendedorController
{
    private readonly IVendedorApiClient _api;
    private readonly IConfiguration _config;

    public NotificacionesController(IVendedorApiClient api, IConfiguration config)
    {
        _api = api;
        _config = config;
    }

    [HttpGet]
    public IActionResult AvisosChat() => ViewComponent("ChatAvisos");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChatLeido(Guid id, CancellationToken cancellationToken)
    {
        var result = await _api.MarcarChatLeidoAsync(id, cancellationToken);
        return result.Unauthorized ? Unauthorized() : NoContent();
    }

    [HttpGet]
    public IActionResult Conexion()
    {
        var token = Request.Cookies[AuthCookieNames.AccessToken];
        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized();
        }

        var baseUrl = _config["Api:BaseUrl"] ?? "http://localhost:8090/";
        return Json(new
        {
            token,
            hubUrl = HubNotificacionesUrl.Resolver(baseUrl, Request.Host.Host, HubRutas.Notificaciones),
            chatHubUrl = HubNotificacionesUrl.Resolver(baseUrl, Request.Host.Host, HubRutas.Chat)
        });
    }
}
