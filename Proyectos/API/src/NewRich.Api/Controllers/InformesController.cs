using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Informes;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador")]
public sealed class InformesController : ApiControllerBase
{
    private readonly IConfiguracionInformeService _configuraciones;

    public InformesController(IConfiguracionInformeService configuraciones) => _configuraciones = configuraciones;

    [HttpGet("configuraciones")]
    public async Task<IActionResult> Configuraciones(CancellationToken cancellationToken) =>
        From(await _configuraciones.ListarAsync(UsuarioId, cancellationToken));

    [HttpPut("configuraciones/{categoria}")]
    public async Task<IActionResult> Guardar(string categoria, [FromBody] GuardarConfiguracionInformeRequest request, CancellationToken cancellationToken) =>
        From(await _configuraciones.GuardarAsync(UsuarioId, categoria, request, cancellationToken));

    [HttpDelete("configuraciones/{categoria}")]
    public async Task<IActionResult> Quitar(string categoria, CancellationToken cancellationToken) =>
        From(await _configuraciones.QuitarAsync(UsuarioId, categoria, cancellationToken));
}
