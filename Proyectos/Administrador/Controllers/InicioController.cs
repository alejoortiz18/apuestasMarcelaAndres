using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Ventas;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Controllers;

public sealed class InicioController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public InicioController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        SetNav("resumen", UiTexts.NavResumen);
        var hoy = DateTime.Today;
        var ventasTask = _api.ConsultarVentasAsync(new ConsultaVentasRequest
        {
            FechaInicial = hoy,
            FechaFinal = hoy.AddDays(1).AddTicks(-1)
        }, cancellationToken);
        var usuariosTask = _api.ListarUsuariosAsync(cancellationToken);
        var notifTask = _api.ListarNotificacionesAsync(cancellationToken);
        await Task.WhenAll(ventasTask, usuariosTask, notifTask);

        var unauthorized = RedirectIfUnauthorized(ventasTask.Result)
                           ?? RedirectIfUnauthorized(usuariosTask.Result)
                           ?? RedirectIfUnauthorized(notifTask.Result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var ventas = ventasTask.Result.Data ?? [];
        var usuarios = usuariosTask.Result.Data ?? [];
        var vendedores = usuarios.Where(u => u.Rol == RolUsuario.Vendedor).ToList();
        var model = new ResumenViewModel
        {
            VentasDelDia = ventas.Sum(v => v.Total),
            BoletosEmitidos = ventas.Count,
            VendedoresTotales = vendedores.Count,
            VendedoresActivos = vendedores.Count(v => v.Estado == EstadoUsuario.Activo && !v.EstadoBloqueado),
            AlertasPendientes = notifTask.Result.Data?.Pendientes ?? 0,
            UltimasVentas = ventas.OrderByDescending(v => v.FechaVenta).Take(6).ToList()
        };
        return View(model);
    }
}
