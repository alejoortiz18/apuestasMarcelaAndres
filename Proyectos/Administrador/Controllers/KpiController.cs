using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Kpi;
using NewRich.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NewRich.Admin.Controllers;

public sealed class KpiController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public KpiController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(
        Guid? grupoId,
        Guid? vendedorId,
        DateTime? fechaInicial,
        DateTime? fechaFinal,
        string? periodo,
        bool compararAnterior = true,
        int pageV = 1,
        int pageD = 1,
        int pageR = 1,
        int pageA = 1,
        int pageSize = 5,
        CancellationToken cancellationToken = default)
    {
        SetNav("kpi", UiTexts.NavKpi);
        var (inicio, fin, periodoUsado) = ResolverPeriodo(periodo, fechaInicial, fechaFinal);
        var model = await ConstruirAsync(grupoId, vendedorId, inicio, fin, periodoUsado, compararAnterior, pageV, pageD, pageR, pageA, pageSize, cancellationToken);
        if (model.redirect is not null)
        {
            return model.redirect;
        }

        return View(model.view);
    }

    public async Task<IActionResult> Pdf(
        Guid? grupoId,
        Guid? vendedorId,
        DateTime? fechaInicial,
        DateTime? fechaFinal,
        string? periodo,
        bool compararAnterior = true,
        CancellationToken cancellationToken = default)
    {
        SetNav("kpi", UiTexts.NavKpi);
        var (inicio, fin, periodoUsado) = ResolverPeriodo(periodo, fechaInicial, fechaFinal);
        var model = await ConstruirAsync(grupoId, vendedorId, inicio, fin, periodoUsado, compararAnterior, 1, 1, 1, 1, 15, cancellationToken);
        if (model.redirect is not null)
        {
            return model.redirect;
        }

        if (model.view?.Kpi is null)
        {
            SetFlash(UiTexts.Vacio, false);
            return RedirectToAction(nameof(Index));
        }

        var bytes = KpiPdf.Crear(model.view.Kpi);
        var nombre = $"kpi-{inicio:yyyyMMdd}-{fin:yyyyMMdd}.pdf";
        return File(bytes, "application/pdf", nombre);
    }

    private async Task<(KpiIndexViewModel? view, IActionResult? redirect)> ConstruirAsync(
        Guid? grupoId,
        Guid? vendedorId,
        DateTime inicio,
        DateTime fin,
        string periodo,
        bool compararAnterior,
        int pageV,
        int pageD,
        int pageR,
        int pageA,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var usuariosTask = _api.ListarUsuariosAsync(cancellationToken);
        var gruposTask = _api.ListarGruposAsync(cancellationToken);
        await Task.WhenAll(usuariosTask, gruposTask);
        var unauthorized = RedirectIfUnauthorized(usuariosTask.Result) ?? RedirectIfUnauthorized(gruposTask.Result);
        if (unauthorized is not null)
        {
            return (null, unauthorized);
        }

        var vendedores = (usuariosTask.Result.Data ?? [])
            .Where(u => u.Rol == RolUsuario.Vendedor)
            .Where(u => !grupoId.HasValue || u.GrupoId == grupoId)
            .ToList();
        if (vendedorId.HasValue && vendedores.All(v => v.UsuarioId != vendedorId))
        {
            vendedorId = null;
        }

        var kpiTask = await _api.ConsultarKpiAsync(new KpiRequest
        {
            GrupoId = grupoId,
            VendedorId = vendedorId,
            FechaInicial = inicio,
            FechaFinal = fin,
            CompararAnterior = compararAnterior
        }, cancellationToken);
        unauthorized = RedirectIfUnauthorized(kpiTask);
        if (unauthorized is not null)
        {
            return (null, unauthorized);
        }

        var kpi = kpiTask.Data;
        return (new KpiIndexViewModel
        {
            GrupoId = grupoId,
            VendedorId = vendedorId,
            FechaInicial = inicio,
            FechaFinal = fin,
            Periodo = periodo,
            CompararAnterior = compararAnterior,
            Kpi = kpi,
            Vendedores = vendedores,
            Grupos = gruposTask.Result.Data ?? [],
            PaginaVendedores = PagingHelper.Paginate(kpi?.IngresosPorVendedor ?? [], pageV, pageSize),
            PaginaDias = PagingHelper.Paginate(kpi?.VentasPorDiaDetalle ?? [], pageD, pageSize),
            PaginaResultados = PagingHelper.Paginate(kpi?.Resultados ?? [], pageR, pageSize),
            PaginaAlertas = PagingHelper.Paginate(kpi?.Alertas ?? [], pageA, pageSize)
        }, null);
    }

    private static (DateTime Inicio, DateTime Fin, string Periodo) ResolverPeriodo(string? periodo, DateTime? fechaInicial, DateTime? fechaFinal)
    {
        var hoy = DateTime.Today;
        periodo = string.IsNullOrWhiteSpace(periodo) ? (fechaInicial.HasValue ? "personalizado" : "30") : periodo;
        return periodo switch
        {
            "7" => (hoy.AddDays(-6), hoy, "7"),
            "mes" => (new DateTime(hoy.Year, hoy.Month, 1), hoy, "mes"),
            "personalizado" => (fechaInicial?.Date ?? hoy.AddDays(-29), fechaFinal?.Date ?? hoy, "personalizado"),
            _ => (hoy.AddDays(-29), hoy, "30")
        };
    }
}

internal static class KpiPdf
{
    public static byte[] Crear(KpiResponse kpi)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var cultura = CultureInfo.GetCultureInfo("es-CO");
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(28);
                page.Size(PageSizes.A4);
                page.Header().Text($"{UiTexts.KpiTitulo} · {kpi.FiltroAplicado}").Bold().FontSize(14);
                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text($"{UiTexts.CorteDeInformacion}: {kpi.Corte:dd/MM/yyyy HH:mm} · {UiTexts.FuenteSql}");
                    col.Item().Text($"{UiTexts.IngresosRegistrados}: {kpi.Ingresos.ToString("C0", cultura)} · {UiTexts.VentasConfirmadas}: {kpi.VentasConfirmadas}");
                    col.Item().Text($"{UiTexts.TicketPromedio}: {kpi.TicketPromedio.ToString("C0", cultura)} · {UiTexts.BoletosGanadores}: {kpi.BoletosGanadores}");
                    col.Item().Text($"{UiTexts.PersonasTotales}: {kpi.PersonasTotales} · {UiTexts.Vendedor}: {kpi.Vendedores} · {UiTexts.Observador}: {kpi.Observadores}");
                    col.Item().Text($"{UiTexts.ValorEntregado}: {kpi.ValorPremiosEntregados.ToString("C0", cultura)} · {UiTexts.CasosAbiertos}: {kpi.CasosAbiertos}");
                    col.Item().Text($"{UiTexts.PdasConectados}: {kpi.PdasConectados} / {kpi.PdasTotales} · {UiTexts.AlertasActivas}: {kpi.AlertasActivas}");
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });
                        t.Header(h =>
                        {
                            h.Cell().Text(UiTexts.Vendedor).Bold();
                            h.Cell().Text(UiTexts.Grupo).Bold();
                            h.Cell().Text(UiTexts.VentasConfirmadas).Bold();
                            h.Cell().Text(UiTexts.IngresosRegistrados).Bold();
                        });
                        foreach (var fila in kpi.IngresosPorVendedor)
                        {
                            t.Cell().Text(fila.Vendedor);
                            t.Cell().Text(fila.Grupo);
                            t.Cell().Text(fila.Ventas.ToString("N0", cultura));
                            t.Cell().Text(fila.Total.ToString("C0", cultura));
                        }
                    });
                });
            });
        }).GeneratePdf();
    }
}
