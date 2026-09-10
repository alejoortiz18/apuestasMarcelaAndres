using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize]
public sealed class ResultadosAndroidController : ApiControllerBase
{
    private readonly IAndroidPdaService _pda;

    public ResultadosAndroidController(IAndroidPdaService pda)
    {
        _pda = pda;
    }

    [HttpGet("ListarMob")]
    public async Task<IActionResult> ListarMob([FromQuery] DateOnly? fecha, [FromQuery] Guid? loteriaId, CancellationToken cancellationToken)
    {
        return From(await _pda.ResultadosMobAsync(fecha, loteriaId, cancellationToken));
    }
}
