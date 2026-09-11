using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Chat;
using NewRich.Application.Contracts.Premios;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Observador,Vendedor")]
public sealed class PremiosController : ApiControllerBase
{
    private readonly IPremioService _premioService;

    public PremiosController(IPremioService premioService)
    {
        _premioService = premioService;
    }

    [Authorize(Roles = "Administrador,Observador")]
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        return From(await _premioService.ListarAsync(cancellationToken));
    }

    [Authorize(Roles = "Administrador,Observador")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        return From(await _premioService.ObtenerAsync(id, cancellationToken));
    }

    [HttpPost("reportar")]
    public async Task<IActionResult> Reportar([FromBody] ReportarCasoGanadorRequest request, CancellationToken cancellationToken)
    {
        return From(await _premioService.ReportarAsync(UsuarioId, request, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost("{id:guid}/validar")]
    public async Task<IActionResult> Validar(Guid id, CancellationToken cancellationToken)
    {
        return From(await _premioService.ValidarAsync(id, UsuarioId, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost("{id:guid}/rechazar")]
    public async Task<IActionResult> Rechazar(Guid id, CancellationToken cancellationToken)
    {
        return From(await _premioService.RechazarAsync(id, UsuarioId, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost("{id:guid}/asignar")]
    public async Task<IActionResult> Asignar(Guid id, [FromBody] AsignarObservadorRequest request, CancellationToken cancellationToken)
    {
        return From(await _premioService.AsignarAsync(id, UsuarioId, request, cancellationToken));
    }

    [Authorize(Roles = "Administrador,Observador")]
    [HttpGet("{id:guid}/foto")]
    public async Task<IActionResult> Foto(Guid id, CancellationToken cancellationToken)
    {
        var result = await _premioService.ObtenerFotoAsync(id, cancellationToken);
        if (!result.IsSuccess || result.Data is null)
        {
            return From(result);
        }

        return File(result.Data.Contenido, ChatAdjunto.TipoMime(result.Data.NombreArchivo), result.Data.NombreArchivo);
    }
}
