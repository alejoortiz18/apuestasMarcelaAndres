using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Domain.Services;

namespace NewRich.Admin.Controllers;

public sealed class MetricasRecaudoController : AdminControllerBase
{
    public const string EncabezadoTablero = "X-Tablero";
    private const int DiasPorDefecto = 7;
    private static readonly int[] DiasRapidos = [1, 7, 15, 30];

    private readonly IAdminApiClient _api;

    public MetricasRecaudoController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? desde, string? hasta, Guid? recaudadorId, string? grupo, CancellationToken cancellationToken = default)
    {
        SetNav("recaudo-metricas", UiTexts.NavMetricasRecaudo);
        var hoy = RecaudoFechas.Hoy();
        var fin = RecaudoFechas.Leer(hasta, hoy);
        var inicio = RecaudoFechas.Leer(desde, fin.AddDays(1 - DiasPorDefecto));
        if (inicio > fin)
        {
            inicio = fin;
        }

        var grupoFiltro = string.IsNullOrWhiteSpace(grupo) ? null : grupo.Trim();
        var result = await _api.TableroRecaudoAsync(inicio, fin, recaudadorId, grupoFiltro, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var parcial = Request.Headers[EncabezadoTablero] == "1";
        if (!result.Success && !parcial)
        {
            SetFlash(result.Message, false);
        }

        var datos = result.Data;
        var puntos = (datos?.Dias ?? [])
            .Select(d => new PuntoLineaRecaudo(d.Fecha, d.Debia, d.Cobrado, d.Generado))
            .ToList();
        var modelo = new MetricasRecaudoViewModel
        {
            Desde = datos?.Desde ?? inicio,
            Hasta = datos?.Hasta ?? fin,
            PeriodoRecortado = datos?.PeriodoRecortado ?? false,
            RecaudadorId = recaudadorId,
            Grupo = grupoFiltro,
            Actualizado = ZonaHorariaColombia.ALocal(DateTime.UtcNow),
            Aviso = result.Success ? null : result.Message,
            TotalVendido = datos?.TotalVendido ?? 0m,
            TotalPorRecaudar = datos?.TotalPorRecaudar ?? 0m,
            TotalRecaudado = datos?.TotalRecaudado ?? 0m,
            TotalPendiente = datos?.TotalPendiente ?? 0m,
            DeudaAnterior = datos?.DeudaAnterior ?? 0m,
            PendienteDelDia = datos?.PendienteDelDia ?? 0m,
            PorcentajeRecaudo = datos?.PorcentajeRecaudo ?? 0,
            VendedoresAlDia = datos?.VendedoresAlDia ?? 0,
            VendedoresPorCobrar = datos?.VendedoresPorCobrar ?? 0,
            VendedoresEnDeuda = datos?.VendedoresEnDeuda ?? 0,
            GruposConPendiente = datos?.GruposConPendiente ?? 0,
            Grafico = GraficoLineaRecaudo.De(puntos, mostrarGenerado: true),
            Recaudadores = BarraRecaudo.De(datos?.Recaudadores ?? []),
            Grupos = BarraRecaudo.De(datos?.Grupos ?? []),
            MayoresSaldos = datos?.MayoresSaldos ?? [],
            OpcionesRecaudadores = datos?.OpcionesRecaudadores ?? [],
            OpcionesGrupos = datos?.OpcionesGrupos ?? [],
            Lecturas = LecturasRecaudo.De(puntos),
            Rangos = DiasRapidos
                .Select(dias => new RangoRapidoRecaudo(
                    dias,
                    dias == 1 ? UiTexts.RangoHoy : string.Format(UiTexts.RangoDiasFormato, dias),
                    hoy.AddDays(1 - dias),
                    hoy,
                    fin == hoy && inicio == hoy.AddDays(1 - dias)))
                .ToList()
        };

        return parcial ? PartialView("_TableroRecaudo", modelo) : View(modelo);
    }
}
