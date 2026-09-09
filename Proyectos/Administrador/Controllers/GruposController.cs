using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Grupos;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Controllers;

public sealed class GruposController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public GruposController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        SetNav("grupos", UiTexts.NavGrupos);
        var result = await _api.ListarGruposAsync(cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        IReadOnlyList<GrupoResponse> items = result.Data ?? [];
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            items = items.Where(g =>
                    g.Nombre.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || (g.Descripcion?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        ViewBag.Query = q;
        return View(new GruposIndexViewModel
        {
            Busqueda = q,
            Pagina = PagingHelper.Paginate(items, page, pageSize)
        });
    }

    [HttpGet]
    public IActionResult Crear()
    {
        SetNav("grupos", UiTexts.CrearGrupo);
        return View("Form", new GrupoFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(GrupoFormViewModel model, CancellationToken cancellationToken)
    {
        SetNav("grupos", UiTexts.CrearGrupo);
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var result = await _api.CrearGrupoAsync(new CrearGrupoRequest
        {
            Nombre = model.Nombre.Trim(),
            Descripcion = model.Descripcion
        }, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View("Form", model);
        }

        SetFlash(SuccessMessages.RegistroCreado);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancellationToken)
    {
        SetNav("grupos", UiTexts.Editar);
        var result = await _api.ObtenerGrupoAsync(id, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!result.Success || result.Data is null)
        {
            SetFlash(result.Message, false);
            return RedirectToAction(nameof(Index));
        }

        return View("Form", new GrupoFormViewModel
        {
            GrupoId = result.Data.GrupoId,
            Nombre = result.Data.Nombre,
            Descripcion = result.Data.Descripcion
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Guid id, GrupoFormViewModel model, CancellationToken cancellationToken)
    {
        SetNav("grupos", UiTexts.Editar);
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var result = await _api.ActualizarGrupoAsync(id, new ActualizarGrupoRequest
        {
            Nombre = model.Nombre.Trim(),
            Descripcion = model.Descripcion
        }, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View("Form", model);
        }

        SetFlash(SuccessMessages.RegistroActualizado);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Detalle(Guid id, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        SetNav("grupos", UiTexts.NavGrupos);
        var grupo = await _api.ObtenerGrupoAsync(id, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(grupo);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!grupo.Success || grupo.Data is null)
        {
            SetFlash(grupo.Message, false);
            return RedirectToAction(nameof(Index));
        }

        var usuarios = await _api.ListarUsuariosAsync(cancellationToken);
        unauthorized = RedirectIfUnauthorized(usuarios);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var miembros = grupo.Data.Vendedores.Select(v => v.UsuarioId).ToHashSet();
        var disponibles = (usuarios.Data ?? [])
            .Where(u => u.Rol == RolUsuario.Vendedor && !miembros.Contains(u.UsuarioId))
            .ToList();

        return View(new GrupoDetalleViewModel
        {
            Grupo = grupo.Data,
            VendedoresDisponibles = disponibles,
            Pagina = PagingHelper.Paginate(grupo.Data.Vendedores, page, pageSize)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Asignar(Guid id, Guid usuarioId, CancellationToken cancellationToken)
    {
        var result = await _api.AsignarVendedorGrupoAsync(id, usuarioId, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        SetFlash(result.Success ? SuccessMessages.GrupoVendedorCambiado : result.Message, result.Success);
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Quitar(Guid id, Guid usuarioId, CancellationToken cancellationToken)
    {
        var result = await _api.DesasignarVendedorGrupoAsync(usuarioId, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        SetFlash(result.Success ? SuccessMessages.GrupoVendedorCambiado : result.Message, result.Success);
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        SetNav("grupos", UiTexts.EliminarGrupo);
        var result = await _api.ObtenerGrupoAsync(id, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!result.Success || result.Data is null)
        {
            SetFlash(result.Message, false);
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarEliminar(Guid id, CancellationToken cancellationToken)
    {
        var result = await _api.EliminarGrupoAsync(id, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        SetFlash(result.Success ? SuccessMessages.RegistroEliminado : result.Message, result.Success);
        return RedirectToAction(nameof(Index));
    }
}
