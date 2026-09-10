using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Application.Contracts.Loterias;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Controllers;

public sealed class ConfiguracionController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public ConfiguracionController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 5, CancellationToken cancellationToken = default)
    {
        SetNav("configuracion", UiTexts.NavConfiguracion);
        var configTask = _api.ObtenerConfiguracionOperativaAsync(cancellationToken);
        var loteriasTask = _api.ListarLoteriasAsync(cancellationToken);
        await Task.WhenAll(configTask, loteriasTask);
        var unauthorized = RedirectIfUnauthorized(configTask.Result) ?? RedirectIfUnauthorized(loteriasTask.Result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        IReadOnlyList<LoteriaResponse> loterias = loteriasTask.Result.Data ?? [];
        if (!string.IsNullOrWhiteSpace(q))
        {
            var termino = q.Trim();
            loterias = loterias.Where(l => l.Nombre.Contains(termino, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return View(new ConfiguracionIndexViewModel
        {
            Form = Mapear(configTask.Result.Data),
            Busqueda = q,
            Pagina = PagingHelper.Paginate(loterias, page, pageSize)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar([Bind(Prefix = "Form")] ConfiguracionOperativaFormViewModel form, CancellationToken cancellationToken)
    {
        SetNav("configuracion", UiTexts.NavConfiguracion);
        if (!ModelState.IsValid)
        {
            var loterias = await _api.ListarLoteriasAsync(cancellationToken);
            var unauthorized = RedirectIfUnauthorized(loterias);
            if (unauthorized is not null)
            {
                return unauthorized;
            }

            return View("Index", new ConfiguracionIndexViewModel
            {
                Form = form,
                Pagina = PagingHelper.Paginate(loterias.Data ?? [], 1, 5)
            });
        }

        var result = await _api.GuardarConfiguracionOperativaAsync(new GuardarConfiguracionOperativaRequest
        {
            HoraCierre = form.HoraCierre,
            VigenciaPremiosDias = form.VigenciaPremiosDias,
            MaxJuegosCombinado = form.MaxJuegosCombinado,
            MaxLineasIndividual = form.MaxLineasIndividual,
            AlertaRepeticionNumero = form.AlertaRepeticionNumero,
            AlertaValorMinimo = form.AlertaValorMinimo,
            CodigosOfflineCapacidad = form.CodigosOfflineCapacidad,
            SincronizacionModo = form.SincronizacionModo,
            LeyendaTirilla = form.LeyendaTirilla
        }, cancellationToken);
        var denied = RedirectIfUnauthorized(result);
        if (denied is not null)
        {
            return denied;
        }

        SetFlash(result.Success ? SuccessMessages.RegistroActualizado : result.Message, result.Success);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult CrearLoteria()
    {
        SetNav("configuracion", UiTexts.AgregarLoteria);
        return View("LoteriaForm", new LoteriaFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearLoteria(LoteriaFormViewModel model, CancellationToken cancellationToken)
    {
        SetNav("configuracion", UiTexts.AgregarLoteria);
        if (!ModelState.IsValid)
        {
            return View("LoteriaForm", model);
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
            return View("LoteriaForm", model);
        }

        SetFlash(SuccessMessages.RegistroCreado);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditarLoteria(Guid id, CancellationToken cancellationToken)
    {
        SetNav("configuracion", UiTexts.Editar);
        var result = await _api.ListarLoteriasAsync(cancellationToken);
        var unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var item = result.Data?.FirstOrDefault(l => l.LoteriaId == id);
        if (item is null)
        {
            SetFlash(VentaMessages.LoteriaNoEncontrada, false);
            return RedirectToAction(nameof(Index));
        }

        return View("LoteriaForm", new LoteriaFormViewModel
        {
            LoteriaId = item.LoteriaId,
            Nombre = item.Nombre,
            Estado = item.Estado
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarLoteria(Guid id, LoteriaFormViewModel model, CancellationToken cancellationToken)
    {
        SetNav("configuracion", UiTexts.Editar);
        if (!ModelState.IsValid)
        {
            return View("LoteriaForm", model);
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
            return View("LoteriaForm", model);
        }

        SetFlash(SuccessMessages.RegistroActualizado);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstadoLoteria(Guid id, CancellationToken cancellationToken)
    {
        var listado = await _api.ListarLoteriasAsync(cancellationToken);
        var unauthorized = RedirectIfUnauthorized(listado);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var item = listado.Data?.FirstOrDefault(l => l.LoteriaId == id);
        if (item is null)
        {
            SetFlash(VentaMessages.LoteriaNoEncontrada, false);
            return RedirectToAction(nameof(Index));
        }

        var nuevo = item.Estado == EstadoGeneral.Activo ? EstadoGeneral.Inactivo : EstadoGeneral.Activo;
        var result = await _api.ActualizarLoteriaAsync(id, new ActualizarLoteriaRequest
        {
            Nombre = item.Nombre,
            Estado = nuevo
        }, cancellationToken);
        unauthorized = RedirectIfUnauthorized(result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        SetFlash(result.Success ? SuccessMessages.RegistroActualizado : result.Message, result.Success);
        return RedirectToAction(nameof(Index));
    }

    private static ConfiguracionOperativaFormViewModel Mapear(ConfiguracionOperativaResponse? data)
    {
        if (data is null)
        {
            return new ConfiguracionOperativaFormViewModel();
        }

        var hora = data.HoraCierre;
        if (TimeSpan.TryParse(hora, out var span))
        {
            hora = span.ToString(@"hh\:mm");
        }

        return new ConfiguracionOperativaFormViewModel
        {
            HoraCierre = hora,
            VigenciaPremiosDias = data.VigenciaPremiosDias,
            MaxJuegosCombinado = data.MaxJuegosCombinado,
            MaxLineasIndividual = data.MaxLineasIndividual,
            AlertaRepeticionNumero = data.AlertaRepeticionNumero,
            AlertaValorMinimo = data.AlertaValorMinimo,
            CodigosOfflineCapacidad = data.CodigosOfflineCapacidad,
            SincronizacionModo = data.SincronizacionModo,
            LeyendaTirilla = string.IsNullOrWhiteSpace(data.LeyendaTirilla)
                ? TirillaCuerpo.CuerpoDefecto
                : data.LeyendaTirilla
        };
    }
}
