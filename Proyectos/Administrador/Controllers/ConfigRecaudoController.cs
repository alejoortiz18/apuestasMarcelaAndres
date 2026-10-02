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
            RecaudadorId = g.RecaudadorId,
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
            GruposSinRecaudador = (datos?.Grupos ?? [])
                .Where(g => g.RecaudadorId is null)
                .Select(g => new OpcionRecaudo { Id = g.GrupoId, Nombre = g.Nombre })
                .ToList(),
            CatalogoVendedores = vendedores.Select(v => new OpcionRecaudo { Id = v.VendedorId, Nombre = v.Nombre }).ToList()
        });
    }

    public async Task<IActionResult> Ver(
        Guid id,
        string? desde,
        string? hasta,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        SetNav("recaudo-config", UiTexts.NavConfigRecaudo);
        var inicio = RecaudoFechas.Leer(desde, RecaudoFechas.Hoy());
        var fin = RecaudoFechas.Leer(hasta, inicio);
        var resultado = await _api.IntegrantesGrupoRecaudoAsync(id, inicio, fin, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(resultado);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!resultado.Success || resultado.Data is null)
        {
            SetFlash(resultado.Message, false);
            return RedirectToAction(nameof(Index), new { desde, hasta });
        }

        var datos = resultado.Data;
        var integrantes = datos.Integrantes.Select(v => new FilaIntegranteGrupoRecaudo
        {
            VendedorId = v.VendedorId,
            Nombre = v.NombreCompleto,
            Alias = v.Alias,
            Usuario = v.Usuario,
            Porcentaje = v.Porcentaje,
            RecaudadorNombre = string.IsNullOrWhiteSpace(v.RecaudadorNombre) ? UiTexts.NoAplica : v.RecaudadorNombre,
            TotalVendido = v.TotalVendido,
            ValorACobrar = v.ValorACobrar,
            TotalPendiente = v.TotalPendiente,
            PagosHoy = v.PagosHoy,
            Estado = v.Estado,
            Color = v.Color
        }).ToList();

        return View(new IntegrantesGrupoRecaudoViewModel
        {
            GrupoId = datos.GrupoId,
            Nombre = datos.Nombre,
            Desde = inicio,
            Hasta = fin,
            Porcentaje = datos.Porcentaje,
            SinConfigurar = datos.SinConfigurar,
            RecaudadorNombre = string.IsNullOrWhiteSpace(datos.RecaudadorNombre) ? UiTexts.NoAplica : datos.RecaudadorNombre,
            TotalPorRecaudar = datos.TotalPorRecaudar,
            TotalRecaudado = datos.TotalRecaudado,
            TotalPendiente = datos.TotalPendiente,
            Integrantes = PagingHelper.Paginate(integrantes, page, pageSize)
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
    public async Task<IActionResult> GuardarRecaudadoresGrupos(List<AsignacionRecaudadorGrupo> asignaciones, string? desde, string? hasta, CancellationToken cancellationToken)
    {
        var cambios = (asignaciones ?? []).Where(TieneCambioRecaudador).ToList();
        if (cambios.Count == 0)
        {
            SetFlash(UiTexts.EligeRecaudadorParaGuardar, false);
            return RedirectToAction(nameof(Index), new { desde, hasta });
        }

        foreach (var item in cambios)
        {
            var result = RecaudadorIdONulo(item.RecaudadorId) is { } recaudadorId
                ? await _api.AsignarGrupoRecaudoAsync(new AsignarGrupoRecaudoRequest
                {
                    RecaudadorId = recaudadorId,
                    GrupoId = item.GrupoId,
                    Porcentaje = item.Porcentaje
                }, cancellationToken)
                : await _api.RetirarGrupoRecaudoAsync(item.GrupoId, cancellationToken);
            var unauthorized = RedirectIfUnauthorized(result);
            if (unauthorized is not null)
            {
                return unauthorized;
            }

            if (!result.Success)
            {
                SetFlash(result.Message, false);
                return RedirectToAction(nameof(Index), new { desde, hasta });
            }
        }

        SetFlash(UiTexts.RecaudadoresActualizados, true);
        return RedirectToAction(nameof(Index), new { desde, hasta });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarPorcentajesGrupos(List<PorcentajeGrupoRecaudoRequest> grupos, string? desde, string? hasta, CancellationToken cancellationToken)
    {
        var result = await _api.ActualizarPorcentajesGruposRecaudoAsync(new ActualizarPorcentajesGruposRecaudoRequest
        {
            Grupos = grupos ?? []
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
    public async Task<IActionResult> GuardarGruposVendedores(List<AsignacionGrupoVendedor> asignaciones, string? desde, string? hasta, CancellationToken cancellationToken)
    {
        var elegidas = (asignaciones ?? []).Where(a => a.GrupoId is not null).ToList();
        if (elegidas.Count == 0)
        {
            SetFlash(UiTexts.EligeGrupoParaGuardar, false);
            return RedirectToAction(nameof(Index), new { desde, hasta });
        }

        foreach (var asignacion in elegidas)
        {
            var result = await _api.AsignarVendedorGrupoAsync(asignacion.GrupoId!.Value, asignacion.VendedorId, cancellationToken);
            var unauthorized = RedirectIfUnauthorized(result);
            if (unauthorized is not null)
            {
                return unauthorized;
            }

            if (!result.Success)
            {
                SetFlash(result.Message, false);
                return RedirectToAction(nameof(Index), new { desde, hasta });
            }
        }

        SetFlash(UiTexts.VendedoresAsignadosAGrupo, true);
        return RedirectToAction(nameof(Index), new { desde, hasta });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarDelGrupo(Guid id, Guid usuarioId, string? desde, string? hasta, CancellationToken cancellationToken)
    {
        var result = await _api.DesasignarVendedorGrupoAsync(usuarioId, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        SetFlash(result.Success ? UiTexts.VendedorEliminadoDelGrupo : result.Message, result.Success);
        return RedirectToAction(nameof(Ver), new { id, desde, hasta });
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

    private static bool TieneCambioRecaudador(AsignacionRecaudadorGrupo item) =>
        RecaudadorIdONulo(item.RecaudadorId) != RecaudadorIdONulo(item.RecaudadorActualId);

    private static Guid? RecaudadorIdONulo(Guid? valor) =>
        valor is null || valor == Guid.Empty ? null : valor;
}
