using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;

namespace NewRich.Admin.Controllers;

public sealed class DetalleRecaudoController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public DetalleRecaudoController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(Guid? id, string? desde, string? hasta, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        SetNav("recaudo-detalle", UiTexts.NavDetalleRecaudo);
        var inicio = RecaudoFechas.Leer(desde, RecaudoFechas.Hoy());
        var fin = RecaudoFechas.Leer(hasta, inicio);
        if (id is null || id == Guid.Empty)
        {
            var panel = await _api.PanelRecaudoAsync(inicio, fin, cancellationToken);
            var unauthorizedPanel = RedirectIfUnauthorized(panel);
            if (unauthorizedPanel is not null)
            {
                return unauthorizedPanel;
            }

            var primero = panel.Data?.FirstOrDefault();
            if (primero is null)
            {
                return View(new DetalleRecaudoViewModel { Desde = inicio, Hasta = fin, Nombre = UiTexts.Vacio });
            }

            return RedirectToAction(nameof(Index), new { id = primero.RecaudadorId, desde = inicio.ToString("yyyy-MM-dd"), hasta = fin.ToString("yyyy-MM-dd") });
        }

        var result = await _api.DetalleRecaudoAsync(id.Value, inicio, fin, cancellationToken);
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
        var filas = (datos?.Vendedores ?? []).Select(v => new FilaDetalleVendedor
        {
            Nombre = v.NombreCompleto,
            Alias = v.Alias,
            Grupo = v.Grupo,
            TotalVendido = v.TotalVendido,
            ValorACobrar = v.ValorACobrar,
            SaldoAnterior = v.SaldoAnterior,
            TotalPendiente = v.TotalPendiente,
            PagosHoy = v.PagosHoy,
            Estado = v.Estado,
            Color = v.Color,
            SenalSinGrupo = v.SenalSinGrupo
        }).ToList();

        return View(new DetalleRecaudoViewModel
        {
            RecaudadorId = id.Value,
            Nombre = datos?.Nombre ?? UiTexts.NoAplica,
            Desde = inicio,
            Hasta = fin,
            TotalPorRecaudar = datos?.TotalPorRecaudar ?? 0m,
            TotalRecaudado = datos?.TotalRecaudado ?? 0m,
            SaldoPendiente = datos?.SaldoPendiente ?? 0m,
            PorcentajeRecaudado = datos?.PorcentajeRecaudado ?? 0,
            Vendedores = PagingHelper.Paginate(filas, page, pageSize)
        });
    }
}
