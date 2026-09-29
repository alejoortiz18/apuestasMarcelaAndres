using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Application.Services;
using NewRich.Domain.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Super")]
public sealed class VendedoresRecaudoController : ApiControllerBase
{
    private readonly IRecaudoService _recaudo;

    public VendedoresRecaudoController(IRecaudoService recaudo)
    {
        _recaudo = recaudo;
    }

    [HttpGet("/api/Recaudo/Vendedores")]
    public async Task<IActionResult> Listar([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        return From(await _recaudo.VendedoresAsync(desde ?? hoy, hasta ?? hoy, cancellationToken));
    }

    [HttpPost("/api/Recaudo/Vendedores")]
    public async Task<IActionResult> Asignar([FromBody] AsignarVendedorRecaudoRequest request, CancellationToken cancellationToken)
    {
        return From(await _recaudo.AsignarVendedorAsync(request, UsuarioId, cancellationToken));
    }
}
