using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Boletos;
using NewRich.Constants.Messages;

namespace NewRich.Admin.Controllers;

public sealed class VentasController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public VentasController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? q, string? estado, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        SetVentasNav(UiTexts.VentasVistaBoleto);
        var result = await _api.FiltrarBoletosAsync(new FiltroBoletosRequest
        {
            CodigoPublico = q,
            Estado = string.IsNullOrWhiteSpace(estado) ? null : estado
        }, cancellationToken);

        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        IReadOnlyList<BoletoListaResponse> items = result.Data ?? [];
        if (!string.IsNullOrWhiteSpace(q))
        {
            items = items.Where(b =>
                    b.CodigoPublico.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || b.Vendedor.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        ViewBag.Query = q;
        ViewBag.Estado = estado;
        return View(new VentasIndexViewModel
        {
            Busqueda = q,
            Estado = estado,
            Pagina = PagingHelper.Paginate(items, page, pageSize)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Tirilla(Guid id, string? volver = null, Guid? loteriaId = null, CancellationToken cancellationToken = default)
    {
        var desdeLoterias = volver is "loterias" or "ver";
        SetVentasNav(desdeLoterias ? UiTexts.VentasVistaLoteria : UiTexts.VentasVistaBoleto, UiTexts.VerTicket);
        var result = await _api.ObtenerTirillaAsync(id, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!result.Success || result.Data is null)
        {
            SetFlash(result.Message, false);
            if (volver == "ver" && loteriaId.HasValue)
            {
                return RedirectToAction("Ver", "Loterias", new { id = loteriaId.Value });
            }

            return RedirectToAction(desdeLoterias ? "Index" : nameof(Index), desdeLoterias ? "Loterias" : "Ventas");
        }

        return View(new TirillaViewModel
        {
            BoletoId = id,
            Tirilla = result.Data,
            VolverALoterias = desdeLoterias,
            VolverLoteriaId = volver == "ver" ? loteriaId : null
        });
    }

    [HttpGet]
    public async Task<IActionResult> Ticket(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _api.ObtenerTirillaAsync(id, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!result.Success || result.Data is null)
        {
            return BadRequest(result.Message);
        }

        return PartialView("_TicketModalContent", new TirillaViewModel
        {
            BoletoId = id,
            Tirilla = result.Data
        });
    }

    [HttpGet]
    public async Task<IActionResult> TirillaPdf(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _api.ObtenerTirillaAsync(id, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!result.Success || result.Data is null)
        {
            SetFlash(result.Message, false);
            return RedirectToAction(nameof(Index));
        }

        var nombre = $"ticket-{result.Data.CodigoImpreso}.pdf";
        return File(TicketPdf.Crear(result.Data), "application/pdf", nombre);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pagar(Guid id, CancellationToken cancellationToken)
    {
        var result = await _api.AutorizarPagoAsync(id, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        SetFlash(result.Success ? SuccessMessages.BoletoValidado : result.Message, result.Success);
        return RedirectToAction(nameof(Index));
    }
}
