using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Dispositivos;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Controllers;

public sealed class DispositivosController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public DispositivosController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? q, int? estado, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        SetNav("pda", UiTexts.NavPda);
        var dispositivosTask = _api.ListarDispositivosAsync(cancellationToken);
        var usuariosTask = _api.ListarUsuariosAsync(cancellationToken);
        await Task.WhenAll(dispositivosTask, usuariosTask);
        var unauthorized = RedirectIfUnauthorized(dispositivosTask.Result) ?? RedirectIfUnauthorized(usuariosTask.Result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        IReadOnlyList<DispositivoResponse> items = dispositivosTask.Result.Data ?? [];
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            items = items.Where(d =>
                    d.CodigoDispositivo.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || (d.UsuarioAsociado?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        if (estado.HasValue && Enum.IsDefined(typeof(EstadoGeneral), estado.Value))
        {
            items = items.Where(d => d.Estado == (EstadoGeneral)estado.Value).ToList();
        }

        ViewBag.Query = q;
        ViewBag.Estado = estado;
        return View(new DispositivosIndexViewModel
        {
            Busqueda = q,
            Estado = estado,
            Pagina = PagingHelper.Paginate(items, page, pageSize),
            Usuarios = usuariosTask.Result.Data ?? []
        });
    }

    [HttpGet]
    public IActionResult Crear()
    {
        SetNav("pda", UiTexts.RegistrarPda);
        return View(new DispositivoFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(DispositivoFormViewModel model, CancellationToken cancellationToken)
    {
        SetNav("pda", UiTexts.RegistrarPda);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _api.CrearDispositivoAsync(new CrearDispositivoRequest
        {
            CodigoDispositivo = model.CodigoDispositivo.Trim(),
            Tipo = model.Tipo,
            Modelo = model.Modelo,
            NumeroSerie = string.IsNullOrWhiteSpace(model.NumeroSerie)
                ? Guid.NewGuid().ToString("N")[..16]
                : model.NumeroSerie
        }, cancellationToken);

        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        SetFlash(SuccessMessages.RegistroCreado);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Asociar(Guid id, Guid usuarioId, CancellationToken cancellationToken)
    {
        var result = await _api.AsociarDispositivoAsync(id, usuarioId, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        SetFlash(result.Success ? SuccessMessages.RegistroActualizado : result.Message, result.Success);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desasociar(Guid id, Guid usuarioId, CancellationToken cancellationToken)
    {
        var result = await _api.DesasociarDispositivoAsync(id, usuarioId, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        SetFlash(result.Success ? SuccessMessages.RegistroActualizado : result.Message, result.Success);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(Guid id, EstadoGeneral estado, string? modelo, CancellationToken cancellationToken)
    {
        var result = await _api.ActualizarDispositivoAsync(id, new ActualizarDispositivoRequest
        {
            Estado = estado,
            Modelo = modelo
        }, cancellationToken);

        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        SetFlash(result.Success ? SuccessMessages.RegistroActualizado : result.Message, result.Success);
        return RedirectToAction(nameof(Index));
    }
}
