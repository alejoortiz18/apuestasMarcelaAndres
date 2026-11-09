using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Shared.Results;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Observador")]
public sealed class InformeRecaudoObservadorController : ApiControllerBase
{
    private readonly IRecaudoService _recaudo;

    public InformeRecaudoObservadorController(IRecaudoService recaudo) => _recaudo = recaudo;

    [HttpGet]
    public async Task<IActionResult> Consultar(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta,
        [FromQuery] string? grupo,
        [FromQuery] Guid? vendedorId,
        CancellationToken cancellationToken)
    {
        if (desde > hasta)
        {
            return From(Result<TableroRecaudoResponse>.Fail(UtilidadesMessages.PeriodoInvalido));
        }

        return From(await _recaudo.TableroAsync(new FiltroTableroRecaudo
        {
            Desde = desde,
            Hasta = hasta,
            Grupo = string.IsNullOrWhiteSpace(grupo) ? null : grupo.Trim(),
            Vendedores = vendedorId is Guid vendedor ? [vendedor] : null
        }, cancellationToken));
    }
}
