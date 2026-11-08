using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Observador")]
public sealed class InformeEgresosObservadorController : ApiControllerBase
{
    private readonly IInformeEgresosObservadorService _informe;

    public InformeEgresosObservadorController(IInformeEgresosObservadorService informe) => _informe = informe;

    [HttpGet]
    public async Task<IActionResult> Consultar(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta,
        [FromQuery] Guid? grupoId,
        [FromQuery] Guid? vendedorId,
        CancellationToken cancellationToken) =>
        From(await _informe.ConsultarAsync(desde, hasta, grupoId, vendedorId, cancellationToken));
}
