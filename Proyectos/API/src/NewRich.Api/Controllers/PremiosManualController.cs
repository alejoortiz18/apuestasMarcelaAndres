using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Premios;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Vendedor,VendedorWeb")]
public sealed class PremiosManualController : ApiControllerBase
{
    private readonly IPremioService _premios;

    public PremiosManualController(IPremioService premios) => _premios = premios;

    [HttpGet("Listar")]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        return From(await _premios.ListarDelVendedorAsync(UsuarioId, cancellationToken));
    }

    [HttpPost("RegistrarEntrega/{id:guid}")]
    public async Task<IActionResult> RegistrarEntrega(Guid id, [FromBody] RegistrarEntregaVendedorRequest request, CancellationToken cancellationToken)
    {
        return From(await _premios.RegistrarEntregaSitioWebAsync(id, UsuarioId, request.ValorGanado, cancellationToken));
    }
}
