using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Services;
using NewRich.Domain.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Vendedor")]
public sealed class EntregaVendedorRecaudoController : ApiControllerBase
{
    private readonly IRecaudoService _recaudo;

    public EntregaVendedorRecaudoController(IRecaudoService recaudo)
    {
        _recaudo = recaudo;
    }

    [HttpGet("/api/Recaudo/MiEntrega")]
    public async Task<IActionResult> MiEntrega([FromQuery] DateOnly? fecha, CancellationToken cancellationToken)
    {
        var dia = fecha ?? DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        return From(await _recaudo.EntregaVendedorAsync(UsuarioId, dia, cancellationToken));
    }
}
