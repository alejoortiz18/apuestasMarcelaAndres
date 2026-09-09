using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Ventas;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize]
public sealed class VentasController : ApiControllerBase
{
    private readonly IVentaService _ventaService;

    public VentasController(IVentaService ventaService)
    {
        _ventaService = ventaService;
    }

    [Authorize(Roles = "Vendedor")]
    [HttpPost]
    public async Task<IActionResult> Confirmar([FromBody] ConfirmarVentaRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        return From(await _ventaService.ConfirmarAsync(UsuarioId, DispositivoId, request, idempotencyKey, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Consultar([FromQuery] ConsultaVentasRequest request, CancellationToken cancellationToken)
    {
        var soloPropias = User.IsInRole("Vendedor");
        return From(await _ventaService.ConsultarAsync(request, UsuarioId, soloPropias, cancellationToken));
    }
}
