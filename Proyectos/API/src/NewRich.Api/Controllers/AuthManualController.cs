using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Auth;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

public sealed class AuthManualController : ApiControllerBase
{
    private readonly IAuthManualService _auth;

    public AuthManualController(IAuthManualService auth) => _auth = auth;

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        return From(await _auth.LoginAsync(request, cancellationToken));
    }
}
