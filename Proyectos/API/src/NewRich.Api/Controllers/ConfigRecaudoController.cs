using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Services;
using NewRich.Domain.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Super")]
public sealed class ConfigRecaudoController : ApiControllerBase
{
    private readonly IRecaudoService _recaudo;

    public ConfigRecaudoController(IRecaudoService recaudo)
    {
        _recaudo = recaudo;
    }

    [HttpGet("/api/Recaudo/Configuracion")]
    public async Task<IActionResult> Obtener([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        return From(await _recaudo.ConfiguracionAsync(desde ?? hoy, hasta ?? hoy, cancellationToken));
    }

    [HttpPost("/api/Recaudo/Grupos/{grupoId:guid}/retiro")]
    public async Task<IActionResult> RetirarGrupo(Guid grupoId, CancellationToken cancellationToken)
    {
        return From(await _recaudo.RetirarGrupoAsync(grupoId, cancellationToken));
    }

    [HttpPost("/api/Recaudo/Vendedores/{vendedorId:guid}/retiro")]
    public async Task<IActionResult> RetirarVendedor(Guid vendedorId, CancellationToken cancellationToken)
    {
        return From(await _recaudo.RetirarVendedorAsync(vendedorId, cancellationToken));
    }
}
