using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize]
public sealed class BoletosController : ApiControllerBase
{
    private readonly IValidacionBoletoService _validacionBoletoService;

    public BoletosController(IValidacionBoletoService validacionBoletoService)
    {
        _validacionBoletoService = validacionBoletoService;
    }

    [Authorize(Roles = "Administrador,Observador")]
    [HttpPost("validar-qr")]
    public async Task<IActionResult> ValidarQr([FromBody] ValidarQrRequest request, CancellationToken cancellationToken)
    {
        return From(await _validacionBoletoService.ValidarQrAsync(request, cancellationToken));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost("{id:guid}/pagar")]
    public async Task<IActionResult> AutorizarPago(Guid id, CancellationToken cancellationToken)
    {
        return From(await _validacionBoletoService.AutorizarPagoAsync(id, cancellationToken));
    }

    [Authorize(Roles = "Administrador,Observador")]
    [HttpGet]
    public async Task<IActionResult> Filtrar([FromQuery] FiltroBoletosRequest request, CancellationToken cancellationToken)
    {
        return From(await _validacionBoletoService.FiltrarAsync(request, cancellationToken));
    }

    [Authorize(Roles = "Administrador,Observador")]
    [HttpGet("{id:guid}/tirilla")]
    public async Task<IActionResult> Tirilla(Guid id, CancellationToken cancellationToken)
    {
        return From(await _validacionBoletoService.ObtenerTirillaAsync(id, cancellationToken));
    }
}
