using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize]
public sealed class LoteriasAndroidController : ApiControllerBase
{
    private readonly IAndroidPdaService _pda;

    public LoteriasAndroidController(IAndroidPdaService pda)
    {
        _pda = pda;
    }

    [HttpGet("ListarMob")]
    public async Task<IActionResult> ListarMob(CancellationToken cancellationToken)
    {
        return From(await _pda.LoteriasMobAsync(cancellationToken));
    }
}
