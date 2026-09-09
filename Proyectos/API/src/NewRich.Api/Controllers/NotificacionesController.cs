using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize]
public sealed class NotificacionesController : ApiControllerBase
{
    private readonly INotificacionService _notificacionService;

    public NotificacionesController(INotificacionService notificacionService)
    {
        _notificacionService = notificacionService;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        return From(await _notificacionService.ListarAsync(UsuarioId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        return From(await _notificacionService.ObtenerAsync(id, UsuarioId, cancellationToken));
    }

    [HttpPost("marcar-leidas")]
    public async Task<IActionResult> MarcarLeidas(CancellationToken cancellationToken)
    {
        return From(await _notificacionService.MarcarLeidasAsync(UsuarioId, cancellationToken));
    }
}
