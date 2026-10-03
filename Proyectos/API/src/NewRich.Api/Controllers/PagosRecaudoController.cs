using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Recaudador")]
public sealed class PagosRecaudoController : ApiControllerBase
{
    private readonly IRecaudoService _recaudo;

    public PagosRecaudoController(IRecaudoService recaudo)
    {
        _recaudo = recaudo;
    }

    [HttpPost("/api/Recaudo/Pagos")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarPagoRecaudoRequest request, CancellationToken cancellationToken)
    {
        return From(await _recaudo.RegistrarPagoAsync(UsuarioId, request, cancellationToken));
    }
}
