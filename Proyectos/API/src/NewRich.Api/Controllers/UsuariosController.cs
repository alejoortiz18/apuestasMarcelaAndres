using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Observador")]
public sealed class UsuariosController : ApiControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        return From(await _usuarioService.ListarAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        return From(await _usuarioService.ObtenerAsync(id, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearUsuarioRequest request, CancellationToken cancellationToken)
    {
        return From(await _usuarioService.CrearAsync(request, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] ActualizarUsuarioRequest request, CancellationToken cancellationToken)
    {
        return From(await _usuarioService.ActualizarAsync(id, request, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        return From(await _usuarioService.EliminarAsync(id, UsuarioId, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost("{id:guid}/restablecer-password")]
    public async Task<IActionResult> RestablecerPassword(Guid id, CancellationToken cancellationToken)
    {
        return From(await _usuarioService.RestablecerPasswordAsync(id, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost("{id:guid}/desbloquear")]
    public async Task<IActionResult> Desbloquear(Guid id, CancellationToken cancellationToken)
    {
        return From(await _usuarioService.DesbloquearAsync(id, cancellationToken));
    }
}
