using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Controllers;

public sealed class ConfigRecaudoController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public ConfigRecaudoController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(
        string? desde,
        string? hasta,
        int page = 1,
        int pageSize = 10,
        int pageV = 1,
        int pageSizeV = 10,
        CancellationToken cancellationToken = default)
    {
        SetNav("recaudo-config", UiTexts.NavConfigRecaudo);
        var inicio = RecaudoFechas.Leer(desde, RecaudoFechas.Hoy());
        var fin = RecaudoFechas.Leer(hasta, inicio);
        var config = await _api.ConfiguracionRecaudoAsync(inicio, fin, cancellationToken);
        var usuarios = await _api.ListarUsuariosAsync(cancellationToken);
        var unauthorized = RedirectIfUnauthorized(config) ?? RedirectIfUnauthorized(usuarios);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!config.Success)
        {
            SetFlash(config.Message, false);
        }

        var datos = config.Data;
        var grupos = (datos?.Grupos ?? []).Select(g => new FilaGrupoRecaudo
        {
            GrupoId = g.GrupoId,
            Nombre = g.Nombre,
            Vendedores = g.Vendedores,
            Porcentaje = g.Porcentaje,
            SinConfigurar = g.SinConfigurar,
            TotalPorRecaudar = g.TotalPorRecaudar,
            TotalRecaudado = g.TotalRecaudado,
            RecaudadorNombre = string.IsNullOrWhiteSpace(g.RecaudadorNombre) ? UiTexts.NoAplica : g.RecaudadorNombre
        }).ToList();
        var vendedores = (datos?.VendedoresSinGrupo ?? []).Select(v => new FilaVendedorRecaudo
        {
            VendedorId = v.VendedorId,
            Nombre = v.Nombre,
            Porcentaje = v.Porcentaje,
            RecaudadorNombre = string.IsNullOrWhiteSpace(v.RecaudadorNombre) ? UiTexts.NoAplica : v.RecaudadorNombre,
            SenalSinGrupo = true
        }).ToList();
        var listaUsuarios = usuarios.Data ?? [];

        return View(new ConfigRecaudoViewModel
        {
            Desde = inicio,
            Hasta = fin,
            Grupos = PagingHelper.Paginate(grupos, page, pageSize),
            Vendedores = PagingHelper.Paginate(vendedores, pageV, pageSizeV),
            Alarmas = datos?.Alarmas ?? [],
            Recaudadores = listaUsuarios
                .Where(u => u.Rol == RolUsuario.Recaudador)
                .OrderBy(u => u.NombreCompleto)
                .Select(u => new OpcionRecaudo { Id = u.UsuarioId, Nombre = u.NombreCompleto })
                .ToList(),
            CatalogoGrupos = grupos.Select(g => new OpcionRecaudo { Id = g.GrupoId, Nombre = g.Nombre }).ToList(),
            CatalogoVendedores = vendedores.Select(v => new OpcionRecaudo { Id = v.VendedorId, Nombre = v.Nombre }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AsignarGrupo(Guid recaudadorId, Guid grupoId, int porcentaje, string? desde, string? hasta, CancellationToken cancellationToken)
    {
        var result = await _api.AsignarGrupoRecaudoAsync(new AsignarGrupoRecaudoRequest
        {
            RecaudadorId = recaudadorId,
            GrupoId = grupoId,
            Porcentaje = porcentaje
        }, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        SetFlash(result.Message, result.Success);
        return RedirectToAction(nameof(Index), new { desde, hasta });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AsignarVendedor(Guid recaudadorId, Guid vendedorId, int porcentaje, string? desde, string? hasta, CancellationToken cancellationToken)
    {
        var result = await _api.AsignarVendedorRecaudoAsync(new AsignarVendedorRecaudoRequest
        {
            RecaudadorId = recaudadorId,
            VendedorId = vendedorId,
            Porcentaje = porcentaje
        }, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        SetFlash(result.Message, result.Success);
        return RedirectToAction(nameof(Index), new { desde, hasta });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetirarGrupo(Guid id, string? desde, string? hasta, CancellationToken cancellationToken)
    {
        var result = await _api.RetirarGrupoRecaudoAsync(id, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        SetFlash(result.Message, result.Success);
        return RedirectToAction(nameof(Index), new { desde, hasta });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetirarVendedor(Guid id, string? desde, string? hasta, CancellationToken cancellationToken)
    {
        var result = await _api.RetirarVendedorRecaudoAsync(id, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        SetFlash(result.Message, result.Success);
        return RedirectToAction(nameof(Index), new { desde, hasta });
    }
}
