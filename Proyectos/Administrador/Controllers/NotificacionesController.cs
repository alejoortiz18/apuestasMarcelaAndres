using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Notificaciones;
using NewRich.Constants.Messages;

namespace NewRich.Admin.Controllers;

public sealed class NotificacionesController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public NotificacionesController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? q, bool? leida, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        SetNav("notificaciones", UiTexts.NavNotificaciones);
        var result = await _api.ListarNotificacionesAsync(cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        IReadOnlyList<NotificacionItemResponse> items = result.Data?.Items ?? [];
        if (!string.IsNullOrWhiteSpace(q))
        {
            items = items.Where(n =>
                    n.Mensaje.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || n.Tipo.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (leida.HasValue)
        {
            items = items.Where(n => n.Leida == leida.Value).ToList();
        }

        ViewBag.Query = q;
        ViewBag.Leida = leida;
        return View(new NotificacionesIndexViewModel
        {
            Busqueda = q,
            Leida = leida,
            Pendientes = result.Data?.Pendientes ?? 0,
            Pagina = PagingHelper.Paginate(items, page, pageSize)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarLeidas(CancellationToken cancellationToken)
    {
        var result = await _api.MarcarNotificacionesLeidasAsync(cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        SetFlash(result.Success ? SuccessMessages.NotificacionesMarcadasLeidas : result.Message, result.Success);
        return RedirectToAction(nameof(Index));
    }
}
