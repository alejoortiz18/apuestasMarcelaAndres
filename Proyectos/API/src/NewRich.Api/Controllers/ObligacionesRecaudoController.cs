using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Services;
using NewRich.Domain.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Super,Recaudador")]
public sealed class ObligacionesRecaudoController : ApiControllerBase
{
    private readonly IRecaudoService _recaudo;

    public ObligacionesRecaudoController(IRecaudoService recaudo)
    {
        _recaudo = recaudo;
    }

    [HttpGet("/api/Recaudo/Obligaciones")]
    public async Task<IActionResult> Listar([FromQuery] Guid recaudadorId, [FromQuery] DateOnly? fecha, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Recaudador"))
        {
            recaudadorId = UsuarioId;
        }

        var dia = fecha ?? DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        return From(await _recaudo.ObligacionesAsync(recaudadorId, dia, cancellationToken));
    }
}
