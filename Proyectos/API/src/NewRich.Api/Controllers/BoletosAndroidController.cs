using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize]
public sealed class BoletosAndroidController : ApiControllerBase
{
    private readonly IAndroidPdaService _pda;

    public BoletosAndroidController(IAndroidPdaService pda)
    {
        _pda = pda;
    }

    [Authorize(Roles = "Administrador,Observador,Vendedor")]
    [HttpPost("ConsultarMob")]
    public async Task<IActionResult> ConsultarMob([FromBody] ConsultaTicketRequest request, CancellationToken cancellationToken)
    {
        return From(await _pda.ConsultarTicketMobAsync(request.TicketCode, cancellationToken));
    }

    [Authorize(Roles = "Administrador,Observador")]
    [HttpPost("ValidarQrMob")]
    public async Task<IActionResult> ValidarQrMob([FromBody] ValidarQrRequest request, CancellationToken cancellationToken)
    {
        return From(await _pda.ValidarQrMobAsync(request, cancellationToken));
    }

    [Authorize(Roles = "Administrador,Observador")]
    [HttpGet("FiltrarMob")]
    public async Task<IActionResult> FiltrarMob([FromQuery] FiltroBoletosRequest request, CancellationToken cancellationToken)
    {
        return From(await _pda.FiltrarBoletosMobAsync(request, cancellationToken));
    }

    [Authorize(Roles = "Administrador,Observador")]
    [HttpGet("TirillaMob/{id:guid}")]
    public async Task<IActionResult> TirillaMob(Guid id, CancellationToken cancellationToken)
    {
        return From(await _pda.TirillaMobAsync(id, cancellationToken));
    }
}
