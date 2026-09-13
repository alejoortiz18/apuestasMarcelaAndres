using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Kpi;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Observador")]
public sealed class KpiAndroidController : ApiControllerBase
{
    private readonly IKpiService _kpi;

    public KpiAndroidController(IKpiService kpi)
    {
        _kpi = kpi;
    }

    [HttpGet("ConsultarMob")]
    public async Task<IActionResult> ConsultarMob([FromQuery] KpiRequest request, CancellationToken cancellationToken)
    {
        return From(await _kpi.ConsultarAsync(request, cancellationToken));
    }
}
