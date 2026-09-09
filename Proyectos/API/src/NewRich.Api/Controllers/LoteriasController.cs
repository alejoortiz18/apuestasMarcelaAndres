using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Loterias;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize]
public sealed class LoteriasController : ApiControllerBase
{
    private readonly ILoteriaService _loteriaService;

    public LoteriasController(ILoteriaService loteriaService)
    {
        _loteriaService = loteriaService;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        return From(await _loteriaService.ListarAsync(cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearLoteriaRequest request, CancellationToken cancellationToken)
    {
        return From(await _loteriaService.CrearAsync(request, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] ActualizarLoteriaRequest request, CancellationToken cancellationToken)
    {
        return From(await _loteriaService.ActualizarAsync(id, request, cancellationToken));
    }
}
