using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.RevisadaManual;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador")]
public sealed class CubrirVentaController : ApiControllerBase
{
    private readonly ICubrirVentaService _cubrir;

    public CubrirVentaController(ICubrirVentaService cubrir) => _cubrir = cubrir;

    [HttpGet("Resumen")]
    public async Task<IActionResult> Resumen([FromQuery] DateTime desde, [FromQuery] DateTime hasta, [FromQuery] decimal? valorMinimo, CancellationToken cancellationToken) =>
        From(await _cubrir.ResumenAsync(desde, hasta, valorMinimo, cancellationToken));

    [HttpPost("CubrirDesdeResumen")]
    public async Task<IActionResult> CubrirDesdeResumen([FromBody] CubrirDesdeResumenRevisadaManualRequest request, CancellationToken cancellationToken) =>
        From(await _cubrir.CubrirDesdeResumenAsync(UsuarioId, request.Claves, request.ComisionPorcentaje, request.DescuentoPorcentaje, cancellationToken));

    [HttpGet("RecubrirPrevia")]
    public async Task<IActionResult> RecubrirPrevia([FromQuery] DateTime desde, [FromQuery] DateTime hasta, [FromQuery] decimal? valorMinimo, CancellationToken cancellationToken) =>
        From(await _cubrir.RecubrirPreviaAsync(desde, hasta, valorMinimo, cancellationToken));

    [HttpPost("Recubrir")]
    public async Task<IActionResult> Recubrir([FromBody] RecubrirRevisadaManualRequest request, CancellationToken cancellationToken) =>
        From(await _cubrir.RecubrirAsync(UsuarioId, request.Claves, request.ComisionPorcentaje, request.DescuentoPorcentaje, cancellationToken));

    [HttpGet("ResumenHistorico")]
    public async Task<IActionResult> ResumenHistorico([FromQuery] DateTime desde, [FromQuery] DateTime hasta, [FromQuery] string? orden, [FromQuery] bool descendente = false, [FromQuery] int pagina = 1, [FromQuery] int tamano = 20, CancellationToken cancellationToken = default) =>
        From(await _cubrir.ResumenHistoricoAsync(desde, hasta, orden, descendente, pagina, tamano, cancellationToken));

    [HttpGet("Cubierto")]
    public async Task<IActionResult> Cubierto([FromQuery] DateTime desde, [FromQuery] DateTime hasta, [FromQuery] List<Guid>? vendedores, CancellationToken cancellationToken) =>
        From(await _cubrir.CubiertoAsync(desde, hasta, vendedores is { Count: > 0 } ? vendedores : null, cancellationToken));

    [HttpGet("CubiertoHoy")]
    public async Task<IActionResult> CubiertoHoy(CancellationToken cancellationToken) =>
        From(await _cubrir.CubiertoHoyAsync(cancellationToken));

    [HttpGet("CubiertoHistorico")]
    public async Task<IActionResult> CubiertoHistorico([FromQuery] DateTime desde, [FromQuery] DateTime hasta, [FromQuery] string? orden, [FromQuery] bool descendente = false, [FromQuery] int pagina = 1, [FromQuery] int tamano = 20, CancellationToken cancellationToken = default) =>
        From(await _cubrir.CubiertoHistoricoAsync(desde, hasta, orden, descendente, pagina, tamano, cancellationToken));
}
