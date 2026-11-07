using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Cierre;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador")]
public sealed class CierreController : ApiControllerBase
{
    private readonly ICierreService _cierres;

    public CierreController(ICierreService cierres) => _cierres = cierres;

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken) =>
        From(await _cierres.ListarAsync(cancellationToken));

    [HttpGet("previa")]
    public async Task<IActionResult> Previa(CancellationToken cancellationToken) =>
        From(await _cierres.PreviaAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Confirmar([FromBody] ConfirmarCierreRequest request, CancellationToken cancellationToken) =>
        From(await _cierres.ConfirmarAsync(UsuarioId, request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detalle(Guid id, [FromQuery] string? grupo, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? orden = null, [FromQuery] string? dir = null, CancellationToken cancellationToken = default) =>
        From(await _cierres.DetalleAsync(id, grupo, page, pageSize, orden, dir, cancellationToken));
}
