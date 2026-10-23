using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.PremiosEntregadosWeb;
using NewRich.Application.Services;
using NewRich.Domain.Services;

namespace NewRich.Api.Controllers;

public sealed class PremiosEntregadosWebController : ApiControllerBase
{
    private readonly IPremioEntregadoWebService _premios;

    public PremiosEntregadosWebController(IPremioEntregadoWebService premios) => _premios = premios;

    [Authorize(Roles = "Vendedor,VendedorWeb")]
    [HttpPost("Registrar")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarPremioWebRequest request, CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        return From(await _premios.RegistrarAsync(UsuarioId, request, hoy, cancellationToken));
    }

    [Authorize(Roles = "Vendedor,VendedorWeb")]
    [HttpGet("Mios")]
    public async Task<IActionResult> Mios([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        return From(await _premios.ListarPropiosAsync(UsuarioId, page, pageSize, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet("Total")]
    public async Task<IActionResult> Total(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta,
        [FromQuery] Guid? grupoId,
        [FromQuery] Guid? vendedorId,
        CancellationToken cancellationToken = default)
    {
        return From(await _premios.TotalAsync(desde, hasta, grupoId, vendedorId, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet("Listado")]
    public async Task<IActionResult> Listado(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta,
        [FromQuery] Guid? grupoId,
        CancellationToken cancellationToken = default)
    {
        return From(await _premios.ListadoAsync(desde, hasta, grupoId, cancellationToken));
    }
}
