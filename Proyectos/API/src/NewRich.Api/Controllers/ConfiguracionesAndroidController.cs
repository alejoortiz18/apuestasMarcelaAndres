using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Vendedor,Observador")]
public sealed class ConfiguracionesAndroidController : ApiControllerBase
{
    private readonly IAndroidPdaService _pda;

    public ConfiguracionesAndroidController(IAndroidPdaService pda)
    {
        _pda = pda;
    }

    [HttpGet("ObtenerOperativaMob")]
    public async Task<IActionResult> ObtenerOperativaMob(CancellationToken cancellationToken)
    {
        return From(await _pda.ObtenerOperativaMobAsync(cancellationToken));
    }
}
