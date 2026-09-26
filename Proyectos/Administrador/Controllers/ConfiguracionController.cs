using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Application.Contracts.Loterias;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;

namespace NewRich.Admin.Controllers;

public sealed class ConfiguracionController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public ConfiguracionController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(
        string? q,
        int page = 1,
        int pageSize = 5,
        int pageNumeros = 1,
        int pageSizeNumeros = 5,
        CancellationToken cancellationToken = default)
    {
        SetNav("configuracion", UiTexts.NavConfiguracion);
        var configTask = _api.ObtenerConfiguracionOperativaAsync(cancellationToken);
        var loteriasTask = _api.ListarLoteriasAsync(cancellationToken);
        var numerosTask = _api.ListarNumerosRestringidosAsync(cancellationToken);
        await Task.WhenAll(configTask, loteriasTask, numerosTask);
        var unauthorized = RedirectIfUnauthorized(configTask.Result)
            ?? RedirectIfUnauthorized(loteriasTask.Result)
            ?? RedirectIfUnauthorized(numerosTask.Result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        IReadOnlyList<LoteriaResponse> todas = loteriasTask.Result.Data ?? [];
        IReadOnlyList<LoteriaResponse> loterias = todas;
        if (!string.IsNullOrWhiteSpace(q))
        {
            var termino = q.Trim();
            loterias = loterias.Where(l => l.Nombre.Contains(termino, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return View(new ConfiguracionIndexViewModel
        {
            Form = Mapear(configTask.Result.Data),
            Busqueda = q,
            Pagina = PagingHelper.Paginate(loterias, page, pageSize),
            DiasVenta = MapDias(todas),
            Topes = todas.OrderBy(l => l.Nombre).ToList(),
            PaginaNumeros = PagingHelper.Paginate(numerosTask.Result.Data ?? [], pageNumeros, pageSizeNumeros)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar([Bind(Prefix = "Form")] ConfiguracionOperativaFormViewModel form, CancellationToken cancellationToken)
    {
        SetNav("configuracion", UiTexts.NavConfiguracion);
        ValidarHorarioOperativo(form);
        if (HorarioIgual(form))
        {
            SetAvisoModal(ConfiguracionMessages.HorasOperacionIguales);
            return await VistaConfiguracionAsync(form, cancellationToken);
        }

        if (!ModelState.IsValid)
        {
            return await VistaConfiguracionAsync(form, cancellationToken);
        }

        var result = await _api.GuardarConfiguracionOperativaAsync(new GuardarConfiguracionOperativaRequest
        {
            HoraApertura = form.HoraApertura,
            HoraCierre = form.HoraCierre,
            VigenciaPremiosDias = form.VigenciaPremiosDias,
            DiasInactividadEliminarPda = form.DiasInactividadEliminarPda,
            MaxJuegosCombinado = form.MaxJuegosCombinado,
            MaxLineasIndividual = form.MaxLineasIndividual,
            AlertaRepeticionNumero = form.AlertaRepeticionNumero,
            AlertaValorMinimo = form.AlertaValorMinimo,
            CodigosOfflineCapacidad = form.CodigosOfflineCapacidad,
            ReposicionDiariaOffline = form.ReposicionDiariaOffline,
            PermitirJuegosOffline = form.PermitirJuegosOffline,
            SincronizacionModo = form.SincronizacionModo,
            LeyendaTirilla = form.LeyendaTirilla,
            MensajeSuperacionTope = form.MensajeSuperacionTope
        }, cancellationToken);
        var denied = RedirectIfUnauthorized(result);
        if (denied is not null)
        {
            return denied;
        }

        if (!result.Success)
        {
            if (string.Equals(result.Message, ConfiguracionMessages.HorasOperacionIguales, StringComparison.Ordinal))
            {
                SetAvisoModal(ConfiguracionMessages.HorasOperacionIguales);
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.Message);
            }

            return await VistaConfiguracionAsync(form, cancellationToken);
        }

        SetFlash(SuccessMessages.RegistroActualizado);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarDias(List<DiasLoteriaFormItem> diasVenta, CancellationToken cancellationToken)
    {
        SetNav("configuracion", UiTexts.NavConfiguracion);
        var result = await _api.ActualizarDiasLoteriasAsync(new ActualizarDiasLoteriasRequest
        {
            Loterias = (diasVenta ?? []).Select(item => new DiasLoteriaRequest
            {
                LoteriaId = item.LoteriaId,
                DiasHabilitados = item.DiasHabilitados ?? []
            }).ToList()
        }, cancellationToken);
        var denied = RedirectIfUnauthorized(result);
        if (denied is not null)
        {
            return denied;
        }

        SetFlash(result.Success ? SuccessMessages.RegistroActualizado : result.Message, result.Success);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarNumeroRestringido(string numero, CancellationToken cancellationToken)
    {
        SetNav("configuracion", UiTexts.NavConfiguracion);
        var result = await _api.AgregarNumeroRestringidoAsync(numero, cancellationToken);
        var denied = RedirectIfUnauthorized(result);
        if (denied is not null)
        {
            return denied;
        }

        SetFlash(result.Success ? SuccessMessages.RegistroCreado : result.Message, result.Success);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarNumeroRestringido(Guid id, CancellationToken cancellationToken)
    {
        var result = await _api.EliminarNumeroRestringidoAsync(id, cancellationToken);
        var denied = RedirectIfUnauthorized(result);
        if (denied is not null)
        {
            return denied;
        }

        SetFlash(result.Success ? SuccessMessages.RegistroEliminado : result.Message, result.Success);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarTopes(List<TopeLoteriaFormItem> topes, CancellationToken cancellationToken)
    {
        var result = await _api.ActualizarTopesLoteriasAsync(new ActualizarTopesLoteriasRequest
        {
            Loterias = (topes ?? []).Select(item => new TopeLoteriaRequest
            {
                LoteriaId = item.LoteriaId,
                Tope = item.Tope
            }).ToList()
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

        var result = await _api.CrearLoteriaAsync(new CrearLoteriaRequest
        {
            Nombre = model.Nombre.Trim(),
            Tope = model.Tope,
            DiasHabilitados = model.DiasHabilitados,
            HoraInicio = model.HoraInicio,
            HoraFin = model.HoraFin
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
            Estado = item.Estado,
            Tope = item.Tope,
            HoraInicio = FormatoHoraInput(item.HoraInicio, string.Empty),
            HoraFin = FormatoHoraInput(item.HoraFin, string.Empty)
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
            Estado = model.Estado,
            Tope = model.Tope,
            HoraInicio = model.HoraInicio,
            HoraFin = model.HoraFin
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
            Estado = nuevo,
            HoraInicio = item.HoraInicio ?? string.Empty,
            HoraFin = item.HoraFin ?? string.Empty
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

        return new ConfiguracionOperativaFormViewModel
        {
            HoraApertura = FormatoHoraInput(data.HoraApertura, "10:00"),
            HoraCierre = FormatoHoraInput(data.HoraCierre, "20:00"),
            VigenciaPremiosDias = data.VigenciaPremiosDias,
            DiasInactividadEliminarPda = data.DiasInactividadEliminarPda,
            MaxJuegosCombinado = data.MaxJuegosCombinado,
            MaxLineasIndividual = data.MaxLineasIndividual,
            AlertaRepeticionNumero = data.AlertaRepeticionNumero,
            AlertaValorMinimo = data.AlertaValorMinimo,
            CodigosOfflineCapacidad = data.CodigosOfflineCapacidad,
            ReposicionDiariaOffline = data.ReposicionDiariaOffline,
            PermitirJuegosOffline = data.PermitirJuegosOffline,
            SincronizacionModo = data.SincronizacionModo,
            LeyendaTirilla = string.IsNullOrWhiteSpace(data.LeyendaTirilla)
                ? TirillaCuerpo.CuerpoDefecto
                : data.LeyendaTirilla,
            MensajeSuperacionTope = string.IsNullOrWhiteSpace(data.MensajeSuperacionTope)
                ? ValidacionTope.PlantillaSuperacionDefecto
                : data.MensajeSuperacionTope
        };
    }

    /// <summary>El input type=time espera HH:mm.</summary>
    private static string FormatoHoraInput(string? valor, string defecto)
    {
        if (Hora12.TryParse(valor, out var span))
        {
            return span.ToString(@"hh\:mm");
        }

        return defecto;
    }

    private void ValidarHorarioOperativo(ConfiguracionOperativaFormViewModel form)
    {
        if (!Hora12.TryParse(form.HoraApertura, out _))
        {
            ModelState.AddModelError("Form.HoraApertura", ConfiguracionMessages.HoraAperturaInvalida);
        }

        if (!Hora12.TryParse(form.HoraCierre, out _))
        {
            ModelState.AddModelError("Form.HoraCierre", ConfiguracionMessages.HoraCierreInvalida);
        }
    }

    private static bool HorarioIgual(ConfiguracionOperativaFormViewModel form) =>
        Hora12.TryParse(form.HoraApertura, out var apertura)
        && Hora12.TryParse(form.HoraCierre, out var cierre)
        && !HorarioOperacion.SonDistintas(apertura, cierre);

    private async Task<IActionResult> VistaConfiguracionAsync(
        ConfiguracionOperativaFormViewModel form,
        CancellationToken cancellationToken)
    {
        var loterias = await _api.ListarLoteriasAsync(cancellationToken);
        var unauthorized = RedirectIfUnauthorized(loterias);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var numeros = await _api.ListarNumerosRestringidosAsync(cancellationToken);
        unauthorized = RedirectIfUnauthorized(numeros);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        return View("Index", new ConfiguracionIndexViewModel
        {
            Form = form,
            Pagina = PagingHelper.Paginate(loterias.Data ?? [], 1, 5),
            DiasVenta = MapDias(loterias.Data ?? []),
            Topes = (loterias.Data ?? []).OrderBy(l => l.Nombre).ToList(),
            PaginaNumeros = PagingHelper.Paginate(numeros.Data ?? [], 1, 5)
        });
    }

    private static List<DiasLoteriaFormItem> MapDias(IReadOnlyList<LoteriaResponse> loterias) =>
        loterias
            .OrderBy(l => l.Nombre)
            .Select(l => new DiasLoteriaFormItem
            {
                LoteriaId = l.LoteriaId,
                Nombre = l.Nombre,
                Estado = l.Estado,
                DiasHabilitados = [.. l.DiasHabilitados]
            })
            .ToList();
}
