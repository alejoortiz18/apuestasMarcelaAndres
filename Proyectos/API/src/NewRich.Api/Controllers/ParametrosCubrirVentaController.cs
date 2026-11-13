using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.RevisadaManual;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador")]
public sealed class ParametrosCubrirVentaController : ApiControllerBase
{
    private readonly IParametrosCubrirVentaService _parametros;

    public ParametrosCubrirVentaController(IParametrosCubrirVentaService parametros) => _parametros = parametros;

    [HttpGet]
    public async Task<IActionResult> Obtener(CancellationToken cancellationToken) =>
        From(await _parametros.ObtenerAsync(cancellationToken));

    [HttpPost("Comision")]
    public async Task<IActionResult> Comision([FromBody] GuardarParametroRevisadaManualRequest request, CancellationToken cancellationToken) =>
        From(await _parametros.GuardarComisionAsync(request.Porcentaje, cancellationToken));

    [HttpPost("Descuento")]
    public async Task<IActionResult> Descuento([FromBody] GuardarParametroRevisadaManualRequest request, CancellationToken cancellationToken) =>
        From(await _parametros.GuardarDescuentoAsync(request.Porcentaje, cancellationToken));
}
