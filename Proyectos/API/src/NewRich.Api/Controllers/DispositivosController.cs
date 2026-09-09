using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Dispositivos;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Observador")]
public sealed class DispositivosController : ApiControllerBase
{
    private readonly IDispositivoService _dispositivoService;

    public DispositivosController(IDispositivoService dispositivoService)
    {
        _dispositivoService = dispositivoService;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        return From(await _dispositivoService.ListarAsync(cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearDispositivoRequest request, CancellationToken cancellationToken)
    {
        return From(await _dispositivoService.CrearAsync(request, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] ActualizarDispositivoRequest request, CancellationToken cancellationToken)
    {
        return From(await _dispositivoService.ActualizarAsync(id, request, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost("{id:guid}/asociar/{usuarioId:guid}")]
    public async Task<IActionResult> Asociar(Guid id, Guid usuarioId, CancellationToken cancellationToken)
    {
        return From(await _dispositivoService.AsociarAsync(id, usuarioId, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost("{id:guid}/desasociar/{usuarioId:guid}")]
    public async Task<IActionResult> Desasociar(Guid id, Guid usuarioId, CancellationToken cancellationToken)
    {
        return From(await _dispositivoService.DesasociarAsync(id, usuarioId, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost("{id:guid}/desasociar")]
    public async Task<IActionResult> DesasociarPorDispositivo(Guid id, CancellationToken cancellationToken)
    {
        return From(await _dispositivoService.DesasociarAsync(id, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        return From(await _dispositivoService.EliminarAsync(id, cancellationToken));
    }
}
