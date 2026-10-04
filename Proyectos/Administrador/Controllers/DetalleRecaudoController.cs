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

    public async Task<IActionResult> Index(Guid? id, string? desde, string? hasta, string? pestana = null, string? grafico = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
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

            return RedirectToAction(nameof(Index), new { id = primero.RecaudadorId, desde = inicio.ToString("yyyy-MM-dd"), hasta = fin.ToString("yyyy-MM-dd"), pestana });
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
        var recaudador = datos?.Nombre ?? UiTexts.NoAplica;
        var vendedores = (datos?.Vendedores ?? [])
            .OrderBy(v => v.Grupo == "Sin grupo")
            .ThenBy(v => v.Grupo)
            .ThenBy(v => v.NombreCompleto)
            .ToList();
        var cobrados = vendedores.Where(v => v.Lista == "Cobrados").Select(v => new FilaCobradoRecaudo
        {
            Grupo = v.Grupo,
            Nombre = v.NombreCompleto,
            Alias = v.Alias,
            SenalSinGrupo = v.SenalSinGrupo,
            Recaudador = recaudador,
            ValorQueDebia = v.TotalPendiente + v.PagosHoy,
            ValorRecibido = v.PagosHoy,
            SaldoPendiente = v.TotalPendiente,
            FechaPago = v.UltimoPago,
            Estado = v.Estado,
            Color = v.Color
        }).ToList();
        var pendientes = vendedores.Where(v => v.Lista != "Cobrados").Select(v => new FilaPendienteRecaudo
        {
            Grupo = v.Grupo,
            Nombre = v.NombreCompleto,
            Alias = v.Alias,
            SenalSinGrupo = v.SenalSinGrupo,
            Recaudador = recaudador,
            ValorQueDebe = v.ValorACobrar,
            DeudaAnterior = v.SaldoAnterior,
            TotalPendiente = v.TotalPendiente,
            Estado = v.Estado,
            Color = v.Color
        }).ToList();
        var activa = DetalleRecaudoPestanas.Leer(pestana);
        var grupoGrafico = (datos?.Grupos ?? []).Select(g => g.Nombre).FirstOrDefault(n => n == grafico);
        var puntos = (datos?.LineaDeTiempo ?? []).Select(d =>
        {
            if (grupoGrafico is null)
            {
                return new PuntoLineaRecaudo(d.Fecha, d.Debia, d.Cobrado);
            }

            var grupo = d.Grupos.FirstOrDefault(g => g.Grupo == grupoGrafico);
            return new PuntoLineaRecaudo(d.Fecha, grupo?.Debia ?? 0m, grupo?.Cobrado ?? 0m);
        }).ToList();

        return View(new DetalleRecaudoViewModel
        {
            RecaudadorId = id.Value,
            Nombre = recaudador,
            Usuario = datos?.Usuario ?? string.Empty,
            Documento = datos?.Documento,
            Desde = inicio,
            Hasta = fin,
            TotalPorRecaudar = datos?.TotalPorRecaudar ?? 0m,
            TotalRecaudado = datos?.TotalRecaudado ?? 0m,
            SaldoPendiente = datos?.SaldoPendiente ?? 0m,
            PorcentajeRecaudado = datos?.PorcentajeRecaudado ?? 0,
            Grupos = datos?.Grupos ?? [],
            GraficoGrupo = grupoGrafico,
            Grafico = GraficoLineaRecaudo.De(puntos),
            Pestana = activa,
            TotalCobrados = cobrados.Count,
            TotalPendientes = pendientes.Count,
            Cobrados = PagingHelper.Paginate(cobrados, activa == DetalleRecaudoPestanas.Cobrados ? page : 1, pageSize),
            Pendientes = PagingHelper.Paginate(pendientes, activa == DetalleRecaudoPestanas.Pendientes ? page : 1, pageSize)
        });
    }
}
