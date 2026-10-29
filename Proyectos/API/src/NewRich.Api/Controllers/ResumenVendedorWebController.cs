using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Services;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Vendedor,VendedorWeb")]
public sealed class ResumenVendedorWebController : ApiControllerBase
{
    private readonly IResumenVendedorWebService _resumen;

    public ResumenVendedorWebController(IResumenVendedorWebService resumen) => _resumen = resumen;

    [HttpGet("Hoy")]
    public async Task<IActionResult> Hoy(CancellationToken cancellationToken) =>
        From(await _resumen.ObtenerAsync(UsuarioId, cancellationToken));
}
