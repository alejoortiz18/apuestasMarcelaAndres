using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.GastosVendedoresWeb;
using NewRich.Application.Contracts.Kpi;
using NewRich.Domain.Enums;

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
        int pageGW = 1,
        string? ordenGW = null,
        string? dirGW = null,
        string? ordenV = null,
        string? dirV = null,
        string? ordenA = null,
        string? dirA = null,
        string? ordenD = null,
        string? dirD = null,
        string? ordenR = null,
        string? dirR = null,
        CancellationToken cancellationToken = default)
    {
        SetNav("kpi", UiTexts.NavKpi);
        var (inicio, fin, periodoUsado) = ResolverPeriodo(periodo, fechaInicial, fechaFinal);
        var ordenes = new OrdenesKpi(
            KpiOrden.EstadoVendedores(ordenV, dirV),
            KpiOrden.EstadoAlertas(ordenA, dirA),
            KpiOrden.EstadoDias(ordenD, dirD),
            KpiOrden.EstadoResultados(ordenR, dirR),
            KpiOrden.EstadoGastosWeb(ordenGW, dirGW));
        var model = await ConstruirAsync(grupoId, vendedorId, inicio, fin, periodoUsado, compararAnterior, pageV, pageD, pageR, pageA, pageSize, cancellationToken, pageGW, ordenes);
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

        var bytes = KpiPdf.Crear(model.view);
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
        CancellationToken cancellationToken,
        int pageGW = 1,
        OrdenesKpi? ordenes = null)
    {
        ordenes ??= new OrdenesKpi(
            KpiOrden.EstadoVendedores(null, null),
            KpiOrden.EstadoAlertas(null, null),
            KpiOrden.EstadoDias(null, null),
            KpiOrden.EstadoResultados(null, null),
            KpiOrden.EstadoGastosWeb(null, null));
        var usuariosTask = _api.ListarUsuariosAsync(cancellationToken);
        var gruposTask = _api.ListarGruposAsync(cancellationToken);
        await Task.WhenAll(usuariosTask, gruposTask);
        var unauthorized = RedirectIfUnauthorized(usuariosTask.Result) ?? RedirectIfUnauthorized(gruposTask.Result);
        if (unauthorized is not null)
        {
            return (null, unauthorized);
        }

        var vendedores = (usuariosTask.Result.Data ?? [])
            .Where(u => RolConsola.EsVendedor(u.Rol))
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

        var ventasWeb = await _api.TotalVentasRegistradasWebAsync(
            DateOnly.FromDateTime(inicio),
            DateOnly.FromDateTime(fin),
            grupoId,
            vendedorId,
            cancellationToken);
        unauthorized = RedirectIfUnauthorized(ventasWeb);
        if (unauthorized is not null)
        {
            return (null, unauthorized);
        }

        var premiosWeb = await _api.TotalPremiosEntregadosWebAsync(
            DateOnly.FromDateTime(inicio),
            DateOnly.FromDateTime(fin),
            grupoId,
            vendedorId,
            cancellationToken);
        unauthorized = RedirectIfUnauthorized(premiosWeb);
        if (unauthorized is not null)
        {
            return (null, unauthorized);
        }

        var premiosWebOk = premiosWeb.Success ? premiosWeb.Data : null;

        var gastosWeb = await _api.GastosVendedoresWebAsync(
            DateOnly.FromDateTime(inicio),
            DateOnly.FromDateTime(fin),
            grupoId,
            vendedorId,
            cancellationToken);
        unauthorized = RedirectIfUnauthorized(gastosWeb);
        if (unauthorized is not null)
        {
            return (null, unauthorized);
        }

        var gastosWebOk = gastosWeb.Success ? gastosWeb.Data : null;
        IReadOnlyList<GastoVendedorWebResponse> listaGastosWeb = gastosWebOk?.Items ?? [];
        var gastosOrdenados = KpiOrden.GastosWeb(listaGastosWeb, ordenes.GastosWeb);

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
            IngresosWeb = ventasWeb.Success ? ventasWeb.Data?.Total ?? 0m : 0m,
            PremiosWeb = premiosWebOk?.Total ?? 0m,
            EntregasWeb = premiosWebOk?.Cantidad ?? 0,
            GastosWebTotal = gastosWebOk?.TotalActivos ?? 0m,
            GastosWebCantidad = gastosWebOk?.CantidadActivos ?? 0,
            GastosWeb = listaGastosWeb,
            PaginaGastosWeb = PagingHelper.Paginate(gastosOrdenados, pageGW, pageSize),
            OrdenGastosWeb = ordenes.GastosWeb.Orden,
            DireccionGastosWeb = ordenes.GastosWeb.Direccion,
            OrdenVendedores = ordenes.Vendedores,
            OrdenAlertas = ordenes.Alertas,
            OrdenDias = ordenes.Dias,
            OrdenResultados = ordenes.Resultados,
            Vendedores = vendedores,
            Grupos = gruposTask.Result.Data ?? [],
            PaginaVendedores = PagingHelper.Paginate(KpiOrden.Vendedores(kpi?.IngresosPorVendedor ?? [], ordenes.Vendedores), pageV, pageSize),
            PaginaDias = PagingHelper.Paginate(KpiOrden.Dias(kpi?.VentasPorDiaDetalle ?? [], ordenes.Dias), pageD, pageSize),
            PaginaResultados = PagingHelper.Paginate(KpiOrden.Resultados(kpi?.Resultados ?? [], ordenes.Resultados), pageR, pageSize),
            PaginaAlertas = PagingHelper.Paginate(KpiOrden.Alertas(kpi?.Alertas ?? [], ordenes.Alertas), pageA, pageSize)
        }, null);
    }

    private sealed record OrdenesKpi(EstadoOrden Vendedores, EstadoOrden Alertas, EstadoOrden Dias, EstadoOrden Resultados, EstadoOrden GastosWeb);

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
