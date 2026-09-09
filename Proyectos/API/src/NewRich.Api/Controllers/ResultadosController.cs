using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Resultados;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize]
public sealed class ResultadosController : ApiControllerBase
{
    private readonly IResultadoService _resultadoService;

    public ResultadosController(IResultadoService resultadoService)
    {
        _resultadoService = resultadoService;
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarResultadoRequest request, CancellationToken cancellationToken)
    {
        return From(await _resultadoService.RegistrarAsync(request, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] DateOnly? fecha, [FromQuery] Guid? loteriaId, CancellationToken cancellationToken)
    {
        return From(await _resultadoService.ListarAsync(fecha, loteriaId, cancellationToken));
    }
}
