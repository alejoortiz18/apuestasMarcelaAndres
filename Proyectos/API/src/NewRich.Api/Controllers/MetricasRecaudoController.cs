using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Application.Services;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Api.Controllers;

[Authorize(Roles = "Administrador,Super,Recaudador")]
public sealed class MetricasRecaudoController : ApiControllerBase
{
    private readonly IRecaudoService _recaudo;

    public MetricasRecaudoController(IRecaudoService recaudo)
    {
        _recaudo = recaudo;
    }

    [HttpGet("/api/Recaudo/Metricas")]
    public async Task<IActionResult> Obtener([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        var inicio = desde ?? hoy;
        var fin = hasta ?? hoy;
        if (User.IsInRole("Recaudador"))
        {
            var detalle = await _recaudo.DetalleAsync(UsuarioId, inicio, fin, cancellationToken);
            if (!detalle.IsSuccess || detalle.Data is null)
            {
                return From(detalle);
            }

            var filas = detalle.Data.Vendedores;
            var metricas = new MetricasRecaudoResponse
            {
                TotalVendido = filas.Sum(f => f.TotalVendido),
                TotalPorRecaudar = detalle.Data.TotalPorRecaudar,
                TotalRecaudado = detalle.Data.TotalRecaudado,
                TotalPendiente = detalle.Data.SaldoPendiente,
                PorcentajeRecaudado = detalle.Data.PorcentajeRecaudado,
                VendedoresAlDia = filas.Count(f => f.Estado == "AlDia"),
                VendedoresEnDeuda = filas.Count(f => f.Estado == "Deudado"),
                GruposConPendiente = filas.Where(f => f.TotalPendiente > 0m).Select(f => f.Grupo).Distinct().Count()
            };
            return From(Result<MetricasRecaudoResponse>.Ok(metricas, detalle.Message));
        }

        return From(await _recaudo.MetricasAsync(inicio, fin, cancellationToken));
    }
}
