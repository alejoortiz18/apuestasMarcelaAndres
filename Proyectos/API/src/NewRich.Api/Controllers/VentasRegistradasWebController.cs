using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.VentasRegistradasWeb;
using NewRich.Application.Services;
using NewRich.Domain.Services;

namespace NewRich.Api.Controllers;

public sealed class VentasRegistradasWebController : ApiControllerBase
{
    private readonly IVentaRegistradaWebService _ventas;

    public VentasRegistradasWebController(IVentaRegistradaWebService ventas) => _ventas = ventas;

    [Authorize(Roles = "Vendedor,VendedorWeb")]
    [HttpPost("Registrar")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarVentaWebRequest request, CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        return From(await _ventas.RegistrarAsync(UsuarioId, request, hoy, cancellationToken));
    }

    [Authorize(Roles = "Vendedor,VendedorWeb")]
    [HttpGet("Mias")]
    public async Task<IActionResult> Mias([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        return From(await _ventas.ListarPropiasAsync(UsuarioId, page, pageSize, cancellationToken));
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
        return From(await _ventas.TotalAsync(desde, hasta, grupoId, vendedorId, cancellationToken));
    }
}
