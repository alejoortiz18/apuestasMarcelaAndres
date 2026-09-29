using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Application.Services;
using NewRich.Domain.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Super,Recaudador")]
public sealed class HistorialRecaudoController : ApiControllerBase
{
    private readonly IRecaudoService _recaudo;

    public HistorialRecaudoController(IRecaudoService recaudo)
    {
        _recaudo = recaudo;
    }

    [HttpGet("/api/Recaudo/Historial")]
    public async Task<IActionResult> Listar(
        [FromQuery] Guid? recaudadorId,
        [FromQuery] Guid? grupoId,
        [FromQuery] Guid? vendedorId,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] string? estado,
        CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        if (User.IsInRole("Recaudador"))
        {
            recaudadorId = UsuarioId;
        }

        return From(await _recaudo.HistorialAsync(new FiltroHistorialRecaudo
        {
            RecaudadorId = recaudadorId,
            GrupoId = grupoId,
            VendedorId = vendedorId,
            Desde = desde ?? hoy.AddDays(-30),
            Hasta = hasta ?? hoy,
            Estado = estado
        }, cancellationToken));
    }

    [HttpGet("/api/Recaudo/Historial/Vendedor/{id:guid}")]
    public async Task<IActionResult> PorVendedor(Guid id, CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        return From(await _recaudo.HistorialAsync(new FiltroHistorialRecaudo
        {
            RecaudadorId = User.IsInRole("Recaudador") ? UsuarioId : null,
            VendedorId = id,
            Desde = hoy.AddDays(-30),
            Hasta = hoy
        }, cancellationToken));
    }
}
