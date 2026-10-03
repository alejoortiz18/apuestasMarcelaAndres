using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;

namespace NewRich.Admin.Controllers;

public sealed class PanelRecaudoController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public PanelRecaudoController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? desde, string? hasta, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        SetNav("recaudo-panel", UiTexts.NavPanelRecaudo);
        var inicio = RecaudoFechas.Leer(desde, RecaudoFechas.Hoy());
        var fin = RecaudoFechas.Leer(hasta, inicio);
        var result = await _api.PanelRecaudoAsync(inicio, fin, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!result.Success)
        {
            SetFlash(result.Message, false);
        }

        var filas = (result.Data ?? []).Select(r => new FilaPanelRecaudo
        {
            RecaudadorId = r.RecaudadorId,
            Nombre = r.Nombre,
            Grupos = r.Grupos,
            PersonasAsignadas = r.PersonasAsignadas,
            TotalPorRecaudar = r.TotalPorRecaudar,
            TotalRecaudado = r.TotalRecaudado,
            SaldoPendiente = r.SaldoPendiente,
            PorcentajeRecaudado = r.PorcentajeRecaudado
        }).ToList();

        return View(new PanelRecaudoViewModel
        {
            Desde = inicio,
            Hasta = fin,
            Pagina = PagingHelper.Paginate(filas, page, pageSize)
        });
    }
}
