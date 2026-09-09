using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Kpi;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador")]
public sealed class KpiController : ApiControllerBase
{
    private readonly IKpiService _kpiService;

    public KpiController(IKpiService kpiService)
    {
        _kpiService = kpiService;
    }

    [HttpGet]
    public async Task<IActionResult> Consultar([FromQuery] KpiRequest request, CancellationToken cancellationToken)
    {
        return From(await _kpiService.ConsultarAsync(request, cancellationToken));
    }
}
