using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Application.Services;
using NewRich.Domain.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Super")]
public sealed class TableroRecaudoController : ApiControllerBase
{
    private readonly IRecaudoService _recaudo;

    public TableroRecaudoController(IRecaudoService recaudo)
    {
        _recaudo = recaudo;
    }

    [HttpGet("/api/Recaudo/Tablero")]
    public async Task<IActionResult> Obtener(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] Guid? recaudadorId,
        [FromQuery] string? grupo,
        CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        var fin = hasta ?? hoy;
        return From(await _recaudo.TableroAsync(new FiltroTableroRecaudo
        {
            Desde = desde ?? fin.AddDays(-6),
            Hasta = fin,
            RecaudadorId = recaudadorId,
            Grupo = grupo
        }, cancellationToken));
    }
}
