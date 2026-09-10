using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Auth;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

public sealed class AuthAndroidController : ApiControllerBase
{
    private readonly IAndroidPdaService _pda;

    public AuthAndroidController(IAndroidPdaService pda)
    {
        _pda = pda;
    }

    [AllowAnonymous]
    [HttpPost("LoginMob")]
    public async Task<IActionResult> LoginMob([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        return From(await _pda.LoginMobAsync(request, cancellationToken));
    }

    [Authorize]
    [HttpPost("CambiarPasswordMob")]
    public async Task<IActionResult> CambiarPasswordMob([FromBody] CambiarPasswordRequest request, CancellationToken cancellationToken)
    {
        return From(await _pda.CambiarPasswordMobAsync(UsuarioId, SesionId, request, cancellationToken));
    }

    [Authorize]
    [HttpPost("LogoutMob")]
    public async Task<IActionResult> LogoutMob(CancellationToken cancellationToken)
    {
        return From(await _pda.LogoutMobAsync(SesionId, cancellationToken));
    }

    [Authorize]
    [HttpGet("PerfilMob")]
    public async Task<IActionResult> PerfilMob(CancellationToken cancellationToken)
    {
        return From(await _pda.PerfilMobAsync(UsuarioId, DispositivoId, cancellationToken));
    }
}
