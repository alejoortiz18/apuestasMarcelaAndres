using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.RegistrarJuegoNumerosWebManual;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Vendedor,VendedorWeb")]
public sealed class RegistrarJuegoNumerosWebManualController : ApiControllerBase
{
    private readonly IRegistrarJuegoNumerosWebManualService _juegos;

    public RegistrarJuegoNumerosWebManualController(IRegistrarJuegoNumerosWebManualService juegos) => _juegos = juegos;

    [HttpPost("Registrar")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarJuegoNumerosWebManualRequest request, CancellationToken cancellationToken)
    {
        return From(await _juegos.RegistrarAsync(UsuarioId, request, cancellationToken));
    }
}
