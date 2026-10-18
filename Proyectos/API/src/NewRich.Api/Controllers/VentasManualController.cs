using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.JuegoNumerosManual;
using NewRich.Application.Contracts.Ventas;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Vendedor")]
public sealed class VentasManualController : ApiControllerBase
{
    private readonly IVentaManualService _ventas;

    public VentasManualController(IVentaManualService ventas) => _ventas = ventas;

    [HttpPost("Confirmar")]
    public async Task<IActionResult> Confirmar([FromBody] ConfirmarVentaRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        return From(await _ventas.ConfirmarAsync(UsuarioId, request, idempotencyKey, cancellationToken));
    }

    [HttpPost("RegistrarNumeros")]
    public async Task<IActionResult> RegistrarNumeros([FromBody] RegistrarJuegoNumerosManualRequest request, CancellationToken cancellationToken)
    {
        return From(await _ventas.RegistrarNumerosAsync(UsuarioId, request, cancellationToken));
    }

    [HttpGet("Consultar")]
    public async Task<IActionResult> Consultar([FromQuery] ConsultaVentasRequest request, CancellationToken cancellationToken)
    {
        return From(await _ventas.ConsultarAsync(UsuarioId, request, cancellationToken));
    }

    [HttpPost("ConsultarCodigo")]
    public async Task<IActionResult> ConsultarCodigo([FromBody] ConsultaTicketRequest request, CancellationToken cancellationToken)
    {
        return From(await _ventas.ConsultarCodigoAsync(UsuarioId, request.TicketCode, cancellationToken));
    }
}
