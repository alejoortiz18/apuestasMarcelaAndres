using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Api.Filters;
using NewRich.Application.Contracts.Loterias;
using NewRich.Application.Services;
using NewRich.Constants;

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
    [RequiereConfirmacion(AccionesProtegidas.LoteriasGuardar)]
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearLoteriaRequest request, CancellationToken cancellationToken)
    {
        return From(await _loteriaService.CrearAsync(request, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [RequiereConfirmacion(AccionesProtegidas.ConfiguracionDias)]
    [HttpPut("dias")]
    public async Task<IActionResult> ActualizarDias([FromBody] ActualizarDiasLoteriasRequest request, CancellationToken cancellationToken)
    {
        return From(await _loteriaService.ActualizarDiasAsync(request, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [RequiereConfirmacion(AccionesProtegidas.LoteriasGuardar, AccionesProtegidas.LoteriasCambiarEstado)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] ActualizarLoteriaRequest request, CancellationToken cancellationToken)
    {
        return From(await _loteriaService.ActualizarAsync(id, request, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [RequiereConfirmacion(AccionesProtegidas.ConfiguracionTopes)]
    [HttpPut("topes")]
    public async Task<IActionResult> ActualizarTopes([FromBody] ActualizarTopesLoteriasRequest request, CancellationToken cancellationToken)
    {
        return From(await _loteriaService.ActualizarTopesAsync(request, cancellationToken));
    }
}
