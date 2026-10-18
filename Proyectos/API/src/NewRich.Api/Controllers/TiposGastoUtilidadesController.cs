using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Vendedor,Administrador")]
[Route("api/utilidades")]
public sealed class TiposGastoUtilidadesController : ApiControllerBase
{
    private readonly IGastosUtilidadesService _gastos;

    public TiposGastoUtilidadesController(IGastosUtilidadesService gastos) => _gastos = gastos;

    [HttpGet("tipos-gasto")]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        return From(await _gastos.ListarTiposActivosAsync(cancellationToken));
    }
}
