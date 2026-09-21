using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Api.Filters;
using NewRich.Application.Contracts.Grupos;
using NewRich.Application.Services;
using NewRich.Constants;

namespace NewRich.Api.Controllers;

[Authorize]
public sealed class GruposController : ApiControllerBase
{
    private readonly IGrupoService _grupoService;

    public GruposController(IGrupoService grupoService)
    {
        _grupoService = grupoService;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        return From(await _grupoService.ListarAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        return From(await _grupoService.ObtenerAsync(id, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [RequiereConfirmacion(AccionesProtegidas.GruposGuardar)]
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearGrupoRequest request, CancellationToken cancellationToken)
    {
        return From(await _grupoService.CrearAsync(request, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [RequiereConfirmacion(AccionesProtegidas.GruposGuardar)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] ActualizarGrupoRequest request, CancellationToken cancellationToken)
    {
        return From(await _grupoService.ActualizarAsync(id, request, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [RequiereConfirmacion(AccionesProtegidas.GruposEliminar)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        return From(await _grupoService.EliminarAsync(id, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [RequiereConfirmacion(AccionesProtegidas.GruposAsignar, AccionesProtegidas.UsuariosCambiarGrupo)]
    [HttpPost("{id:guid}/asignar/{usuarioId:guid}")]
    public async Task<IActionResult> Asignar(Guid id, Guid usuarioId, CancellationToken cancellationToken)
    {
        return From(await _grupoService.AsignarVendedorAsync(usuarioId, id, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [RequiereConfirmacion(AccionesProtegidas.GruposQuitar, AccionesProtegidas.UsuariosCambiarGrupo)]
    [HttpPost("desasignar/{usuarioId:guid}")]
    public async Task<IActionResult> Desasignar(Guid usuarioId, CancellationToken cancellationToken)
    {
        return From(await _grupoService.AsignarVendedorAsync(usuarioId, null, cancellationToken));
    }
}
