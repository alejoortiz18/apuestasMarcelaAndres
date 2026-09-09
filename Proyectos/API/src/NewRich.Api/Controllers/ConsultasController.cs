using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Consultas;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador")]
public sealed class ConsultasController : ApiControllerBase
{
    private readonly IConsultaService _consultaService;

    public ConsultasController(IConsultaService consultaService)
    {
        _consultaService = consultaService;
    }

    [HttpGet]
    public async Task<IActionResult> Buscar([FromQuery] BusquedaAdministrativaRequest request, CancellationToken cancellationToken)
    {
        return From(await _consultaService.BuscarAsync(request, cancellationToken));
    }
}
