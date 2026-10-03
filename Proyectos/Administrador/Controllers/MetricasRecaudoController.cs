using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;

namespace NewRich.Admin.Controllers;

public sealed class MetricasRecaudoController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public MetricasRecaudoController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? desde, string? hasta, CancellationToken cancellationToken = default)
    {
        SetNav("recaudo-metricas", UiTexts.NavMetricasRecaudo);
        var inicio = RecaudoFechas.Leer(desde, RecaudoFechas.Hoy());
        var fin = RecaudoFechas.Leer(hasta, inicio);
        var result = await _api.MetricasRecaudoAsync(inicio, fin, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!result.Success)
        {
            SetFlash(result.Message, false);
        }

        var datos = result.Data;
        return View(new MetricasRecaudoViewModel
        {
            Desde = inicio,
            Hasta = fin,
            TotalVendido = datos?.TotalVendido ?? 0m,
            TotalPorRecaudar = datos?.TotalPorRecaudar ?? 0m,
            TotalRecaudado = datos?.TotalRecaudado ?? 0m,
            TotalPendiente = datos?.TotalPendiente ?? 0m,
            PorcentajeRecaudo = datos?.PorcentajeRecaudo ?? 0,
            VendedoresAlDia = datos?.VendedoresAlDia ?? 0,
            VendedoresEnDeuda = datos?.VendedoresEnDeuda ?? 0,
            GruposConPendiente = datos?.GruposConPendiente ?? 0
        });
    }
}
