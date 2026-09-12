using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Offline;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Vendedor")]
public sealed class OfflineAndroidController : ApiControllerBase
{
    private readonly IAndroidPdaService _pda;
    private readonly IOfflineService _offline;

    public OfflineAndroidController(IAndroidPdaService pda, IOfflineService offline)
    {
        _pda = pda;
        _offline = offline;
    }

    [HttpPost("DescargarMob")]
    public async Task<IActionResult> DescargarMob(CancellationToken cancellationToken)
    {
        return From(await _pda.DescargarOfflineMobAsync(UsuarioId, DispositivoId, cancellationToken));
    }

    [HttpPost("SincronizarVentasMob")]
    public async Task<IActionResult> SincronizarVentasMob(
        [FromBody] SincronizarVentasOfflineRequest request,
        CancellationToken cancellationToken)
    {
        return From(await _offline.SincronizarVentasAsync(UsuarioId, request, cancellationToken));
    }
}
