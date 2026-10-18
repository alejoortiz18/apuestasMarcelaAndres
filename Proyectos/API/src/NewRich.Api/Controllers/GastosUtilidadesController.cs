using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Utilidades;
using NewRich.Application.Services;
using NewRich.Domain.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Vendedor,Administrador")]
[Route("api/utilidades")]
public sealed class GastosUtilidadesController : ApiControllerBase
{
    private readonly IGastosUtilidadesService _gastos;

    public GastosUtilidadesController(IGastosUtilidadesService gastos) => _gastos = gastos;

    [HttpGet("indicadores")]
    public async Task<IActionResult> Indicadores(
        [FromQuery] string? periodo,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        CancellationToken cancellationToken = default)
    {
        var hoy = DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        return From(await _gastos.ConsultarInformePropioAsync(UsuarioId, periodo, desde, hasta, hoy, cancellationToken));
    }

    [HttpGet("mis-gastos")]
    public async Task<IActionResult> MisGastos(
        [FromQuery] string? periodo,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var hoy = DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        return From(await _gastos.ConsultarPropiosAsync(UsuarioId, periodo, desde, hasta, hoy, page, pageSize, cancellationToken));
    }

    [HttpPost("gastos")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarGastoRequest request, CancellationToken cancellationToken)
    {
        return From(await _gastos.RegistrarAsync(UsuarioId, request, cancellationToken));
    }
}
