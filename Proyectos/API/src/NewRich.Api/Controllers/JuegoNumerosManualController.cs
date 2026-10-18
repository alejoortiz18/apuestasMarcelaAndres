using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.JuegoNumerosManual;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Vendedor")]
public sealed class JuegoNumerosManualController : ApiControllerBase
{
    private readonly IJuegoNumerosManualService _juegos;

    public JuegoNumerosManualController(IJuegoNumerosManualService juegos) => _juegos = juegos;

    [HttpPost("Registrar")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarJuegoNumerosManualRequest request, CancellationToken cancellationToken)
    {
        return From(await _juegos.RegistrarAsync(UsuarioId, DispositivoId, request, cancellationToken));
    }
}
