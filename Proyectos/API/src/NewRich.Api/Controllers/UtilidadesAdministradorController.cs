using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Utilidades;
using NewRich.Application.Services;
using NewRich.Domain.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador")]
[Route("api/utilidades")]
public sealed class UtilidadesAdministradorController : ApiControllerBase
{
    private readonly IGastosUtilidadesService _gastos;

    public UtilidadesAdministradorController(IGastosUtilidadesService gastos) => _gastos = gastos;

    [HttpGet("informe")]
    public async Task<IActionResult> Indicadores(
        [FromQuery] string? periodo,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        CancellationToken cancellationToken = default)
    {
        var hoy = Hoy();
        return From(await _gastos.ConsultarInformeAsync(periodo, desde, hasta, hoy, cancellationToken));
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(
        [FromQuery] string? periodo,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        CancellationToken cancellationToken = default)
    {
        var hoy = Hoy();
        return From(await _gastos.ConsultarResumenAsync(periodo, desde, hasta, hoy, cancellationToken));
    }

    [HttpGet("movimientos")]
    public async Task<IActionResult> Movimientos(
        [FromQuery] string? periodo,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] Guid? usuarioId,
        [FromQuery] Guid? tipoGastoId,
        [FromQuery] string? estado,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var hoy = Hoy();
        return From(await _gastos.ConsultarMovimientosAsync(periodo, desde, hasta, hoy, usuarioId, tipoGastoId, estado, page, pageSize, cancellationToken));
    }

    [HttpPost("gastos/{id:guid}/corregir")]
    public async Task<IActionResult> Corregir(Guid id, [FromBody] CorregirGastoRequest request, CancellationToken cancellationToken) =>
        From(await _gastos.CorregirAsync(UsuarioId, id, request, cancellationToken));

    [HttpPost("gastos/{id:guid}/anular")]
    public async Task<IActionResult> Anular(Guid id, [FromBody] AnularGastoRequest request, CancellationToken cancellationToken) =>
        From(await _gastos.AnularAsync(UsuarioId, id, request, cancellationToken));

    [HttpGet("catalogo")]
    public async Task<IActionResult> Catalogo(CancellationToken cancellationToken) =>
        From(await _gastos.ListarCatalogoAsync(cancellationToken));

    [HttpPost("tipos-gasto")]
    public async Task<IActionResult> CrearTipo([FromBody] GuardarTipoGastoRequest request, CancellationToken cancellationToken) =>
        From(await _gastos.CrearTipoAsync(UsuarioId, request, cancellationToken));

    [HttpPut("tipos-gasto/{id:guid}")]
    public async Task<IActionResult> ActualizarTipo(Guid id, [FromBody] ActualizarTipoGastoRequest request, CancellationToken cancellationToken) =>
        From(await _gastos.ActualizarTipoAsync(
            UsuarioId,
            id,
            new GuardarTipoGastoRequest(request.Nombre, request.Descripcion, request.RequiereObservacion),
            request.Activo,
            cancellationToken));

    private static DateOnly Hoy() => DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));
}
