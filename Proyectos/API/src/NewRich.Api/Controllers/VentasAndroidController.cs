using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Ventas;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize]
public sealed class VentasAndroidController : ApiControllerBase
{
    private readonly IAndroidPdaService _pda;

    public VentasAndroidController(IAndroidPdaService pda)
    {
        _pda = pda;
    }

    [Authorize(Roles = "Vendedor")]
    [HttpPost("ConfirmarMob")]
    public async Task<IActionResult> ConfirmarMob([FromBody] ConfirmarVentaRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        return From(await _pda.ConfirmarVentaMobAsync(UsuarioId, DispositivoId, request, idempotencyKey, cancellationToken));
    }

    [HttpGet("ConsultarMob")]
    public async Task<IActionResult> ConsultarMob([FromQuery] ConsultaVentasRequest request, CancellationToken cancellationToken)
    {
        var soloPropias = User.IsInRole("Vendedor");
        return From(await _pda.ConsultarVentasMobAsync(request, UsuarioId, soloPropias, cancellationToken));
    }
}
