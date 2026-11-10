using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Api.Filters;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Application.Services;
using NewRich.Constants;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Super")]
public sealed class PagosAdministracionRecaudoController : ApiControllerBase
{
    private readonly IRecaudoService _recaudo;

    public PagosAdministracionRecaudoController(IRecaudoService recaudo)
    {
        _recaudo = recaudo;
    }

    [HttpPost("/api/Recaudo/PagosAdministracion")]
    [RequiereConfirmacion(AccionesProtegidas.RecaudoRegistrarPagoAdmin)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarPagoAdministracionRequest request, CancellationToken cancellationToken)
    {
        return From(await _recaudo.RegistrarPagoAdministracionAsync(request, UsuarioId, cancellationToken));
    }
}
