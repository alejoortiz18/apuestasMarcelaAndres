using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador")]
public sealed class GastosVendedoresWebController : ApiControllerBase
{
    private readonly IGastoVendedorWebService _gastos;

    public GastosVendedoresWebController(IGastoVendedorWebService gastos) => _gastos = gastos;

    [HttpGet("Kpi")]
    public async Task<IActionResult> Kpi(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta,
        [FromQuery] Guid? grupoId,
        [FromQuery] Guid? vendedorId,
        CancellationToken cancellationToken = default)
    {
        return From(await _gastos.ConsultarAsync(desde, hasta, grupoId, vendedorId, cancellationToken));
    }
}
