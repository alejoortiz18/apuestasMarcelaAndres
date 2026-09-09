using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Consultas;
using NewRich.Application.Contracts.Loterias;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Controllers;

public sealed class LoteriasController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public LoteriasController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? q, int? estado, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        SetNav("loterias", UiTexts.NavLoterias);
        var result = await _api.ListarLoteriasAsync(cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        IReadOnlyList<LoteriaResponse> items = result.Data ?? [];
        if (!string.IsNullOrWhiteSpace(q))
        {
            items = items.Where(l => l.Nombre.Contains(q.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (estado.HasValue && Enum.IsDefined(typeof(EstadoGeneral), estado.Value))
        {
            items = items.Where(l => l.Estado == (EstadoGeneral)estado.Value).ToList();
        }

        ViewBag.Query = q;
        ViewBag.Estado = estado;
        return View(new LoteriasIndexViewModel
        {
            Busqueda = q,
            Estado = estado,
            Pagina = PagingHelper.Paginate(items, page, pageSize)
        });
    }

    [HttpGet]
    public IActionResult Crear()
    {
        SetNav("loterias", UiTexts.NuevaLoteria);
        return View("Form", new LoteriaFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(LoteriaFormViewModel model, CancellationToken cancellationToken)
    {
        SetNav("loterias", UiTexts.NuevaLoteria);
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var result = await _api.CrearLoteriaAsync(new CrearLoteriaRequest { Nombre = model.Nombre.Trim() }, cancellationToken);
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
        SetNav("loterias", UiTexts.Editar);
        var result = await _api.ListarLoteriasAsync(cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var item = result.Data?.FirstOrDefault(l => l.LoteriaId == id);
        if (item is null)
        {
            SetFlash(result.Message, false);
            return RedirectToAction(nameof(Index));
        }

        return View("Form", new LoteriaFormViewModel
        {
            LoteriaId = item.LoteriaId,
            Nombre = item.Nombre,
            Estado = item.Estado
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Guid id, LoteriaFormViewModel model, CancellationToken cancellationToken)
    {
        SetNav("loterias", UiTexts.Editar);
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var result = await _api.ActualizarLoteriaAsync(id, new ActualizarLoteriaRequest
        {
            Nombre = model.Nombre.Trim(),
            Estado = model.Estado
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

    public async Task<IActionResult> Boletos(
        string? codigoBoleto,
        string? numero,
        string? vendedor,
        DateTime? fecha,
        string? estado,
        Guid? loteriaId,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        SetNav("loterias", UiTexts.NavLoterias);
        var loterias = await _api.ListarLoteriasAsync(cancellationToken);
        var unauthorized = RedirectIfUnauthorized(loterias);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var busqueda = await _api.BuscarConsultasAsync(new BusquedaAdministrativaRequest
        {
            CodigoBoleto = codigoBoleto,
            Numero = numero,
            Vendedor = vendedor,
            Fecha = fecha,
            Estado = estado,
            LoteriaId = loteriaId
        }, cancellationToken);

        unauthorized = RedirectIfUnauthorized(busqueda);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        return View(new ConsultaBoletosViewModel
        {
            CodigoBoleto = codigoBoleto,
            Numero = numero,
            Vendedor = vendedor,
            Fecha = fecha,
            Estado = estado,
            LoteriaId = loteriaId,
            Loterias = loterias.Data ?? [],
            Pagina = PagingHelper.Paginate(busqueda.Data ?? [], page, pageSize)
        });
    }
}
