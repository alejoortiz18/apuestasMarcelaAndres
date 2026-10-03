using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Services;
using NewRich.Domain.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Super")]
public sealed class DetalleRecaudoController : ApiControllerBase
{
    private readonly IRecaudoService _recaudo;

    public DetalleRecaudoController(IRecaudoService recaudo)
    {
        _recaudo = recaudo;
    }

    [HttpGet("/api/Recaudo/Detalle/{recaudadorId:guid}")]
    public async Task<IActionResult> Obtener(Guid recaudadorId, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        return From(await _recaudo.DetalleAsync(recaudadorId, desde ?? hoy, hasta ?? hoy, cancellationToken));
    }
}
