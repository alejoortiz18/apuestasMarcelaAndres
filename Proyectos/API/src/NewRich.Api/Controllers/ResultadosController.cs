using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Api.Filters;
using NewRich.Application.Contracts.Resultados;
using NewRich.Application.Services;
using NewRich.Constants;

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
    [RequiereConfirmacion(AccionesProtegidas.ResultadosGuardar)]
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

    [HttpGet("{numeroGanadorId:guid}/ganadores")]
    public async Task<IActionResult> Ganadores(Guid numeroGanadorId, CancellationToken cancellationToken)
    {
        return From(await _resultadoService.ListarGanadoresAsync(numeroGanadorId, cancellationToken));
    }
}
