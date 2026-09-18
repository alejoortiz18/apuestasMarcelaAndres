using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Controllers;

public sealed class UsuariosController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public UsuariosController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? q, int? rol, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        SetNav("usuarios", UiTexts.NavUsuarios);
        var usuariosTask = _api.ListarUsuariosAsync(cancellationToken);
        var gruposTask = _api.ListarGruposAsync(cancellationToken);
        await Task.WhenAll(usuariosTask, gruposTask);
        var result = usuariosTask.Result;
        var unauthorized = RedirectIfUnauthorized(result) ?? RedirectIfUnauthorized(gruposTask.Result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        IReadOnlyList<UsuarioResponse> items = result.Data ?? [];
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            items = items.Where(u =>
                    Contains(u.NombreCompleto, term)
                    || Contains(u.Usuario, term)
                    || Contains(u.Alias, term)
                    || Contains(u.Documento, term))
                .ToList();
        }

        if (rol.HasValue && Enum.IsDefined(typeof(RolUsuario), rol.Value))
        {
            var filtro = (RolUsuario)rol.Value;
            items = items.Where(u => u.Rol == filtro).ToList();
        }

        ViewBag.Query = q;
        ViewBag.Rol = rol;
        return View(new UsuariosIndexViewModel
        {
            Busqueda = q,
            Rol = rol,
            Pagina = PagingHelper.Paginate(items, page, pageSize),
            Grupos = gruposTask.Result.Data ?? []
        });
    }

    [HttpGet]
    public async Task<IActionResult> Crear(CancellationToken cancellationToken)
    {
        var model = await BuildForm(new UsuarioFormViewModel(), cancellationToken, soloDisponibles: true);
        if (EsPeticionAjax())
        {
            return PartialView("_FormModal", model);
        }

        SetNav("usuarios", UiTexts.CrearUsuario);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(UsuarioFormViewModel model, CancellationToken cancellationToken)
    {
        SetNav("usuarios", UiTexts.CrearUsuario);
        ValidarGrupo(model);
        if (!ModelState.IsValid)
        {
            return await FormularioCrear(model, cancellationToken);
        }

        var result = await _api.CrearUsuarioAsync(new CrearUsuarioRequest
        {
            NombreCompleto = model.NombreCompleto,
            Usuario = model.Usuario,
            Alias = model.Alias,
            Documento = model.Documento,
            Celular = model.Celular,
            Email = model.Email,
            Rol = model.Rol,
            DispositivoId = model.DispositivoId,
            GrupoId = model.GrupoId
        }, cancellationToken);

        var unauthorized = SalidaNoAutorizada(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return await FormularioCrear(model, cancellationToken);
        }

        SetFlash(result.Message);
        return EsPeticionAjax()
            ? Json(new { redirect = Url.Action(nameof(Index)) })
            : RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancellationToken)
    {
        SetNav("usuarios", UiTexts.Editar);
        var result = await _api.ObtenerUsuarioAsync(id, cancellationToken);
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

        var u = result.Data;
        return View("Form", await BuildForm(new UsuarioFormViewModel
        {
            UsuarioId = u.UsuarioId,
            NombreCompleto = u.NombreCompleto,
            Usuario = u.Usuario,
            Alias = u.Alias,
            Documento = u.Documento,
            Celular = u.Celular,
            Email = u.Email,
            Rol = u.Rol,
            Estado = u.Estado,
            DispositivoId = u.DispositivoId
        }, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Guid id, UsuarioFormViewModel model, CancellationToken cancellationToken)
    {
        SetNav("usuarios", UiTexts.Editar);
        model.UsuarioId = id;
        if (!ModelState.IsValid)
        {
            return View("Form", await BuildForm(model, cancellationToken));
        }

        var result = await _api.ActualizarUsuarioAsync(id, new ActualizarUsuarioRequest
        {
            NombreCompleto = model.NombreCompleto,
            Alias = model.Alias,
            Documento = model.Documento,
            Celular = model.Celular,
            Email = model.Email,
            Estado = model.Estado,
            DispositivoId = model.DispositivoId
        }, cancellationToken);

        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View("Form", await BuildForm(model, cancellationToken));
        }

        SetFlash(SuccessMessages.UsuarioActualizado);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(Guid id, CancellationToken cancellationToken)
    {
        var result = await _api.ObtenerUsuarioAsync(id, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!result.Success || result.Data is null)
        {
            if (EsPeticionAjax())
            {
                return StatusCode(400, result.Message);
            }

            SetFlash(result.Message, false);
            return RedirectToAction(nameof(Index));
        }

        if (EsPeticionAjax())
        {
            return PartialView("_DetalleModal", result.Data);
        }

        SetNav("usuarios", UiTexts.Detalle);
        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restablecer(Guid id, CancellationToken cancellationToken)
    {
        var result = await _api.RestablecerPasswordAsync(id, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (result.Success && result.Data is not null)
        {
            SetFlash($"{SuccessMessages.PasswordRestablecido} {UiTexts.PasswordTemporalMostrada}: {result.Data.PasswordTemporal}");
        }
        else
        {
            SetFlash(result.Message, false);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desbloquear(Guid id, CancellationToken cancellationToken)
    {
        var result = await _api.DesbloquearUsuarioAsync(id, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        SetFlash(result.Success ? SuccessMessages.UsuarioDesbloqueado : result.Message, result.Success);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        SetNav("usuarios", UiTexts.EliminarUsuario);
        var result = await _api.ObtenerUsuarioAsync(id, cancellationToken);
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
        var result = await _api.EliminarUsuarioAsync(id, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        SetFlash(result.Success ? SuccessMessages.UsuarioEliminado : result.Message, result.Success);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarGrupo(Guid id, Guid? grupoId, CancellationToken cancellationToken)
    {
        var result = grupoId.HasValue
            ? await _api.AsignarVendedorGrupoAsync(grupoId.Value, id, cancellationToken)
            : await _api.DesasignarVendedorGrupoAsync(id, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        SetFlash(result.Success ? SuccessMessages.GrupoVendedorCambiado : result.Message, result.Success);
        return RedirectToAction(nameof(Index));
    }

    private async Task<UsuarioFormViewModel> BuildForm(
        UsuarioFormViewModel model,
        CancellationToken cancellationToken,
        bool soloDisponibles = false)
    {
        var dispositivosTask = _api.ListarDispositivosAsync(cancellationToken);
        var gruposTask = _api.ListarGruposAsync(cancellationToken);
        await Task.WhenAll(dispositivosTask, gruposTask);
        model.Dispositivos = (dispositivosTask.Result.Data ?? [])
            .Where(d => !soloDisponibles || !d.UsuarioAsociadoId.HasValue)
            .ToList();
        model.Grupos = gruposTask.Result.Data ?? [];
        return model;
    }

    /// <summary>El formulario de creación vive en un modal, salvo que se abra la página directa.</summary>
    private async Task<IActionResult> FormularioCrear(UsuarioFormViewModel model, CancellationToken cancellationToken)
    {
        var formulario = await BuildForm(model, cancellationToken, soloDisponibles: true);
        return EsPeticionAjax()
            ? PartialView("_FormModal", formulario)
            : View("Form", formulario);
    }

    /// <summary>Desde el modal la expiración de sesión se resuelve navegando, no reemplazando el contenido.</summary>
    private IActionResult? SalidaNoAutorizada<T>(ApiCallResult<T> result)
    {
        if (RedirectIfUnauthorized(result) is not RedirectToActionResult redirect)
        {
            return null;
        }

        return EsPeticionAjax()
            ? Json(new { redirect = Url.Action(redirect.ActionName, redirect.ControllerName, redirect.RouteValues) })
            : redirect;
    }

    /// <summary>Un vendedor siempre pertenece a un grupo; los demás perfiles no se agrupan.</summary>
    private void ValidarGrupo(UsuarioFormViewModel model)
    {
        if (model.Rol == RolUsuario.Vendedor && !model.GrupoId.HasValue)
        {
            ModelState.AddModelError(nameof(model.GrupoId), UsuarioMessages.GrupoRequeridoParaVendedor);
            return;
        }

        if (model.Rol != RolUsuario.Vendedor && model.GrupoId.HasValue)
        {
            model.GrupoId = null;
        }
    }

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
