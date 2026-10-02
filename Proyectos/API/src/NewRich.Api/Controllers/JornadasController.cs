using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Api.Filters;
using NewRich.Application.Contracts.Jornadas;
using NewRich.Application.Services;
using NewRich.Constants;

namespace NewRich.Api.Controllers;

[Authorize]
public sealed class JornadasController : ApiControllerBase
{
    private readonly IJornadaService _jornadaService;

    public JornadasController(IJornadaService jornadaService)
    {
        _jornadaService = jornadaService;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        return From(await _jornadaService.ListarAsync(cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [RequiereConfirmacion(AccionesProtegidas.LoteriasGuardar)]
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearJornadaRequest request, CancellationToken cancellationToken)
    {
        return From(await _jornadaService.CrearAsync(request, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [RequiereConfirmacion(AccionesProtegidas.LoteriasGuardar)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] ActualizarJornadaRequest request, CancellationToken cancellationToken)
    {
        return From(await _jornadaService.ActualizarAsync(id, request, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [RequiereConfirmacion(AccionesProtegidas.LoteriasGuardar)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        return From(await _jornadaService.EliminarAsync(id, cancellationToken));
    }
}
