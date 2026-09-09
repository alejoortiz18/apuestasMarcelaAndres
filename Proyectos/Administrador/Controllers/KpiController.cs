using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
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

    public async Task<IActionResult> Index(Guid? vendedorId, DateTime? fechaInicial, DateTime? fechaFinal, CancellationToken cancellationToken)
    {
        SetNav("kpi", UiTexts.NavKpi);
        var usuariosTask = _api.ListarUsuariosAsync(cancellationToken);
        var kpiTask = _api.ConsultarKpiAsync(new KpiRequest
        {
            VendedorId = vendedorId,
            FechaInicial = fechaInicial,
            FechaFinal = fechaFinal
        }, cancellationToken);
        await Task.WhenAll(usuariosTask, kpiTask);
        var unauthorized = RedirectIfUnauthorized(usuariosTask.Result) ?? RedirectIfUnauthorized(kpiTask.Result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        return View(new KpiIndexViewModel
        {
            VendedorId = vendedorId,
            FechaInicial = fechaInicial,
            FechaFinal = fechaFinal,
            Kpi = kpiTask.Result.Data,
            Vendedores = (usuariosTask.Result.Data ?? []).Where(u => u.Rol == RolUsuario.Vendedor).ToList()
        });
    }
}
