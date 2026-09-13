using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Observador")]
public sealed class ObservadorAndroidController : ApiControllerBase
{
    private readonly IObservadorTicketService _tickets;

    public ObservadorAndroidController(IObservadorTicketService tickets)
    {
        _tickets = tickets;
    }

    [HttpPost("ConsultarTicketMob")]
    public async Task<IActionResult> ConsultarTicketMob([FromBody] ConsultaTicketRequest request, CancellationToken cancellationToken)
    {
        return From(await _tickets.ConsultarAsync(request.TicketCode, cancellationToken));
    }
}
