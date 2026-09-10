using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Premios;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Observador,Vendedor")]
public sealed class PremiosAndroidController : ApiControllerBase
{
    private readonly IAndroidPdaService _pda;

    public PremiosAndroidController(IAndroidPdaService pda)
    {
        _pda = pda;
    }

    [Authorize(Roles = "Administrador,Observador")]
    [HttpGet("ListarMob")]
    public async Task<IActionResult> ListarMob(CancellationToken cancellationToken)
    {
        return From(await _pda.PremiosMobAsync(cancellationToken));
    }

    [HttpPost("ReportarMob")]
    public async Task<IActionResult> ReportarMob([FromBody] ReportarCasoGanadorRequest request, CancellationToken cancellationToken)
    {
        return From(await _pda.ReportarPremioMobAsync(UsuarioId, request, cancellationToken));
    }
}
