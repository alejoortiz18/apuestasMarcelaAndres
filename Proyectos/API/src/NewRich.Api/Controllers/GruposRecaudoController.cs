using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Application.Services;
using NewRich.Domain.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Super")]
public sealed class GruposRecaudoController : ApiControllerBase
{
    private readonly IRecaudoService _recaudo;

    public GruposRecaudoController(IRecaudoService recaudo)
    {
        _recaudo = recaudo;
    }

    [HttpGet("/api/Recaudo/Grupos")]
    public async Task<IActionResult> Listar([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        return From(await _recaudo.GruposAsync(desde ?? hoy, hasta ?? hoy, cancellationToken));
    }

    [HttpPost("/api/Recaudo/Grupos")]
    public async Task<IActionResult> Asignar([FromBody] AsignarGrupoRecaudoRequest request, CancellationToken cancellationToken)
    {
        return From(await _recaudo.AsignarGrupoAsync(request, UsuarioId, cancellationToken));
    }
}
