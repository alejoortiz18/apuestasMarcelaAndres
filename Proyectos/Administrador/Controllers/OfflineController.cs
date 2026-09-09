using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Offline;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Controllers;

public sealed class OfflineController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public OfflineController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(
        string? q,
        int page = 1,
        int pageSize = 5,
        CancellationToken cancellationToken = default)
    {
        SetNav("offline", UiTexts.NavOffline);
        var listado = await _api.ListarCodigosOfflineAsync(cancellationToken);
        var unauthorized = RedirectIfUnauthorized(listado);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        IReadOnlyList<OfflineGrupoResponse> grupos = OfflineAgrupacion.Agrupar(listado.Data?.Codigos ?? []);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            grupos = grupos
                .Where(g =>
                    g.Usuario.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || g.Pda.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return View(new OfflineIndexViewModel
        {
            Busqueda = q,
            Pagina = PagingHelper.Paginate(grupos, page, pageSize)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Codigos(
        Guid usuarioId,
        Guid dispositivoId,
        DateTime? fechaInicial,
        DateTime? fechaFinal,
        int page = 1,
        int pageSize = 5,
        CancellationToken cancellationToken = default)
    {
        SetNav("offline", UiTexts.CodigosDelUsuario);
        var listado = await _api.ListarCodigosOfflineAsync(cancellationToken);
        var unauthorized = RedirectIfUnauthorized(listado);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var delLote = (listado.Data?.Codigos ?? [])
            .Where(c => c.UsuarioId == usuarioId && c.DispositivoId == dispositivoId)
            .ToList();
        if (delLote.Count == 0)
        {
            SetFlash(UiTexts.LoteOfflineNoEncontrado, false);
            return RedirectToAction(nameof(Index));
        }

        var filtrados = OfflineAgrupacion.EnRango(delLote, fechaInicial, fechaFinal);
        var primero = delLote[0];
        return View(new OfflineLoteViewModel
        {
            UsuarioId = usuarioId,
            DispositivoId = dispositivoId,
            Usuario = primero.Usuario,
            Pda = primero.Pda,
            FechaInicial = fechaInicial,
            FechaFinal = fechaFinal,
            Resumen = OfflineAgrupacion.Resumen(filtrados),
            Pagina = PagingHelper.Paginate(filtrados, page, pageSize)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Crear(CancellationToken cancellationToken)
    {
        SetNav("offline", UiTexts.GenerarCodigos);
        var model = new OfflineFormViewModel();
        await FormularioAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(OfflineFormViewModel model, CancellationToken cancellationToken)
    {
        SetNav("offline", UiTexts.GenerarCodigos);
        if (!ModelState.IsValid)
        {
            await FormularioAsync(model, cancellationToken);
            return View(model);
        }

        await FormularioAsync(model, cancellationToken);
        var vendedor = model.Usuarios.FirstOrDefault(u => u.UsuarioId == model.UsuarioId);
        if (vendedor?.DispositivoId is not Guid pdaId || pdaId == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, UiTexts.AsociarPdaAntesDeGenerar);
            return View(model);
        }

        var result = await _api.GenerarCodigosOfflineAsync(new GenerarCodigosOfflineRequest
        {
            UsuarioId = model.UsuarioId,
            DispositivoId = pdaId,
            Cantidad = model.Cantidad
        }, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await FormularioAsync(model, cancellationToken);
            return View(model);
        }

        SetFlash(SuccessMessages.CodigosOfflineGenerados);
        return RedirectToAction(nameof(Index));
    }

    private async Task FormularioAsync(OfflineFormViewModel model, CancellationToken cancellationToken)
    {
        var usuarios = await _api.ListarUsuariosAsync(cancellationToken);
        model.Usuarios = (usuarios.Data ?? []).Where(u => u.Rol == RolUsuario.Vendedor).ToList();
        var vendedor = model.Usuarios.FirstOrDefault(u => u.UsuarioId == model.UsuarioId);
        model.DispositivoId = vendedor?.DispositivoId ?? Guid.Empty;
    }
}
