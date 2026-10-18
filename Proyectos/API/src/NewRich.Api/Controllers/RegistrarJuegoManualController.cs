using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.RegistrarJuegoManual;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Vendedor")]
public sealed class RegistrarJuegoManualController : ApiControllerBase
{
    private readonly IRegistrarJuegoManualService _juegos;

    public RegistrarJuegoManualController(IRegistrarJuegoManualService juegos) => _juegos = juegos;

    [HttpGet("Loterias")]
    public async Task<IActionResult> Loterias(CancellationToken cancellationToken)
    {
        return From(await _juegos.ListarLoteriasAsync(cancellationToken));
    }

    [HttpGet("Consultar")]
    public async Task<IActionResult> Consultar([FromQuery] DateTime fechaInicial, [FromQuery] DateTime fechaFinal, CancellationToken cancellationToken)
    {
        return From(await _juegos.ConsultarAsync(UsuarioId, fechaInicial, fechaFinal, cancellationToken));
    }

    [HttpPost("Registrar")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarJuegoManualRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        return From(await _juegos.RegistrarAsync(UsuarioId, request, idempotencyKey, cancellationToken));
    }
}
