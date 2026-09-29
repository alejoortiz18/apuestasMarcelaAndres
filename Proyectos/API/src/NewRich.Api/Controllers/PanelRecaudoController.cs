using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Application.Services;
using NewRich.Domain.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Super")]
public sealed class PanelRecaudoController : ApiControllerBase
{
    private readonly IRecaudoService _recaudo;

    public PanelRecaudoController(IRecaudoService recaudo)
    {
        _recaudo = recaudo;
    }

    [HttpGet("/api/Recaudo/Panel")]
    public async Task<IActionResult> Listar([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        return From(await _recaudo.PanelAsync(desde ?? hoy, hasta ?? hoy, cancellationToken));
    }
}
