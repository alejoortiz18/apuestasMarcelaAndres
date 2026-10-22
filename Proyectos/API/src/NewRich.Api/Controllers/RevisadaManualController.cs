using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Api.Filters;
using NewRich.Application.Contracts.RevisadaManual;
using NewRich.Application.Services;
using NewRich.Constants;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador")]
public sealed class RevisadaManualController : ApiControllerBase
{
    private readonly IRevisadaManualService _revisada;

    public RevisadaManualController(IRevisadaManualService revisada) => _revisada = revisada;

    [HttpGet("Resumen")]
    public async Task<IActionResult> Resumen([FromQuery] DateTime desde, [FromQuery] DateTime hasta, [FromQuery] decimal? valorMinimo, CancellationToken cancellationToken)
    {
        return From(await _revisada.ResumenAsync(desde, hasta, valorMinimo, cancellationToken));
    }

    [HttpPost("PorCubrir")]
    public async Task<IActionResult> PorCubrir([FromBody] MarcarPorCubrirRevisadaManualRequest request, CancellationToken cancellationToken)
    {
        return From(await _revisada.MarcarPorCubrirAsync(UsuarioId, request.Claves, cancellationToken));
    }

    [HttpGet("PorCubrir")]
    public async Task<IActionResult> ListarPorCubrir([FromQuery] DateTime desde, [FromQuery] DateTime hasta, CancellationToken cancellationToken)
    {
        return From(await _revisada.PorCubrirAsync(desde, hasta, cancellationToken));
    }

    [HttpPost("Cubrir")]
    [RequiereConfirmacion(AccionesProtegidas.RevisadaCubrir)]
    public async Task<IActionResult> Cubrir([FromBody] CubrirRevisadaManualRequest request, CancellationToken cancellationToken)
    {
        return From(await _revisada.CubrirAsync(UsuarioId, request.OperacionId, request.ComisionPorcentaje, cancellationToken));
    }

    [HttpPost("CubrirDesdeResumen")]
    public async Task<IActionResult> CubrirDesdeResumen([FromBody] CubrirDesdeResumenRevisadaManualRequest request, CancellationToken cancellationToken)
    {
        return From(await _revisada.CubrirDesdeResumenAsync(UsuarioId, request.Claves, request.ComisionPorcentaje, request.DescuentoPorcentaje, cancellationToken));
    }

    [HttpGet("RecubrirPrevia")]
    public async Task<IActionResult> RecubrirPrevia([FromQuery] DateTime desde, [FromQuery] DateTime hasta, [FromQuery] decimal? valorMinimo, CancellationToken cancellationToken)
    {
        return From(await _revisada.RecubrirPreviaAsync(desde, hasta, valorMinimo, cancellationToken));
    }

    [HttpPost("Recubrir")]
    public async Task<IActionResult> Recubrir([FromBody] RecubrirRevisadaManualRequest request, CancellationToken cancellationToken)
    {
        return From(await _revisada.RecubrirAsync(UsuarioId, request.Claves, request.ComisionPorcentaje, request.DescuentoPorcentaje, cancellationToken));
    }

    [HttpGet("ResumenHistorico")]
    public async Task<IActionResult> ResumenHistorico([FromQuery] DateTime desde, [FromQuery] DateTime hasta, [FromQuery] string? orden, [FromQuery] bool descendente = false, [FromQuery] int pagina = 1, [FromQuery] int tamano = 20, CancellationToken cancellationToken = default)
    {
        return From(await _revisada.ResumenHistoricoAsync(desde, hasta, orden, descendente, pagina, tamano, cancellationToken));
    }

    [HttpGet("CubiertoHoy")]
    public async Task<IActionResult> CubiertoHoy(CancellationToken cancellationToken)
    {
        return From(await _revisada.CubiertoHoyAsync(cancellationToken));
    }

    [HttpGet("CubiertoHistorico")]
    public async Task<IActionResult> CubiertoHistorico([FromQuery] DateTime desde, [FromQuery] DateTime hasta, [FromQuery] string? orden, [FromQuery] bool descendente = false, [FromQuery] int pagina = 1, [FromQuery] int tamano = 20, CancellationToken cancellationToken = default)
    {
        return From(await _revisada.CubiertoHistoricoAsync(desde, hasta, orden, descendente, pagina, tamano, cancellationToken));
    }

    [HttpGet("Cubierto")]
    public async Task<IActionResult> Cubierto([FromQuery] DateTime desde, [FromQuery] DateTime hasta, CancellationToken cancellationToken)
    {
        return From(await _revisada.CubiertoAsync(desde, hasta, cancellationToken));
    }

    [HttpPost("Descuento")]
    [RequiereConfirmacion(AccionesProtegidas.RevisadaDescuento)]
    public async Task<IActionResult> Descuento([FromBody] DescuentoRevisadaManualRequest request, CancellationToken cancellationToken)
    {
        return From(await _revisada.RegistrarDescuentoAsync(UsuarioId, request.OperacionId, request.DescuentoPorcentaje, cancellationToken));
    }
}
