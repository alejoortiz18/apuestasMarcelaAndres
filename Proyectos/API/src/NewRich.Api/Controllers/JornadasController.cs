using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize]
public sealed class JornadasController : ApiControllerBase
{
    private readonly IJornadaService _jornadaService;

    public JornadasController(IJornadaService jornadaService)
    {
        _jornadaService = jornadaService;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        return From(await _jornadaService.ListarAsync(cancellationToken));
    }
}
