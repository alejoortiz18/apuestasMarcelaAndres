using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Api.Filters;
using NewRich.Application.Contracts.Llaves;
using NewRich.Application.Services;
using NewRich.Constants;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador")]
public sealed class LlavesAdministradorController : ApiControllerBase
{
    private readonly ILlaveAdministradorService _llaves;

    public LlavesAdministradorController(ILlaveAdministradorService llaves)
    {
        _llaves = llaves;
    }

    [HttpGet("administradores")]
    public async Task<IActionResult> Administradores(CancellationToken cancellationToken)
    {
        return From(await _llaves.AdministradoresAsync(cancellationToken));
    }

    [HttpGet("estado/{usuarioId:guid}")]
    public async Task<IActionResult> Estado(Guid usuarioId, CancellationToken cancellationToken)
    {
        return From(await _llaves.EstadoAsync(usuarioId, cancellationToken));
    }

    [RequiereConfirmacion(AccionesProtegidas.LlaveRegistrar)]
    [HttpPost]
    public async Task<IActionResult> Generar([FromBody] GenerarLlaveAdministradorRequest request, CancellationToken cancellationToken)
    {
        return From(await _llaves.GenerarAsync(request, cancellationToken));
    }
}
