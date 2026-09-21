using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Offline;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Shared.Results;

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

    [HttpPost("ReponerDiarioMob")]
    public async Task<IActionResult> ReponerDiarioMob(
        [FromBody] ReponerCodigosOfflineRequest request,
        CancellationToken cancellationToken)
    {
        if (DispositivoId is null)
        {
            return From(Result<ReponerCodigosOfflineResponse>.Fail(AuthMessages.DispositivoNoAsociado));
        }

        return From(await _offline.ReponerDiarioAsync(UsuarioId, DispositivoId.Value, request, cancellationToken));
    }
}
