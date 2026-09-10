using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Vendedor")]
public sealed class OfflineAndroidController : ApiControllerBase
{
    private readonly IAndroidPdaService _pda;

    public OfflineAndroidController(IAndroidPdaService pda)
    {
        _pda = pda;
    }

    [HttpPost("DescargarMob")]
    public async Task<IActionResult> DescargarMob(CancellationToken cancellationToken)
    {
        return From(await _pda.DescargarOfflineMobAsync(UsuarioId, DispositivoId, cancellationToken));
    }
}
