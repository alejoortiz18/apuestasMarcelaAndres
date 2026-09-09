using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Offline;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Observador")]
public sealed class OfflineController : ApiControllerBase
{
    private readonly IOfflineService _offlineService;

    public OfflineController(IOfflineService offlineService)
    {
        _offlineService = offlineService;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        return From(await _offlineService.ListarAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        return From(await _offlineService.ObtenerAsync(id, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<IActionResult> Generar([FromBody] GenerarCodigosOfflineRequest request, CancellationToken cancellationToken)
    {
        return From(await _offlineService.GenerarAsync(request, cancellationToken));
    }
}
