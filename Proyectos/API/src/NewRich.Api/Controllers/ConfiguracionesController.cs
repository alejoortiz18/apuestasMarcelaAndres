using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Observador")]
public sealed class ConfiguracionesController : ApiControllerBase
{
    private readonly IConfiguracionService _configuracionService;

    public ConfiguracionesController(IConfiguracionService configuracionService)
    {
        _configuracionService = configuracionService;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        return From(await _configuracionService.ListarAsync(cancellationToken));
    }

    [HttpGet("operativa")]
    public async Task<IActionResult> ObtenerOperativa(CancellationToken cancellationToken)
    {
        return From(await _configuracionService.ObtenerOperativaAsync(cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("operativa")]
    public async Task<IActionResult> GuardarOperativa([FromBody] GuardarConfiguracionOperativaRequest request, CancellationToken cancellationToken)
    {
        return From(await _configuracionService.GuardarOperativaAsync(request, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("{clave}")]
    public async Task<IActionResult> Actualizar(string clave, [FromBody] ActualizarConfiguracionRequest request, CancellationToken cancellationToken)
    {
        return From(await _configuracionService.ActualizarAsync(clave, request.Valor, cancellationToken));
    }
}
