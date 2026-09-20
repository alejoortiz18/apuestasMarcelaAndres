using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NewRich.Admin.Constants;
using NewRich.Admin.Hubs;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Admin.Services.Pda;
using NewRich.Application.Contracts.Dispositivos;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Controllers;

public sealed class DispositivosController : AdminControllerBase
{
    private readonly IAdminApiClient _api;
    private readonly IRegistroPdaService _registro;
    private readonly IHubContext<RegistroPdaHub> _hub;
    private readonly CandadoRegistroPda _candado;

    public DispositivosController(
        IAdminApiClient api,
        IRegistroPdaService registro,
        IHubContext<RegistroPdaHub> hub,
        CandadoRegistroPda candado)
    {
        _api = api;
        _registro = registro;
        _hub = hub;
        _candado = candado;
    }

    public async Task<IActionResult> Index(string? q, int? estado, int page = 1, int pageSize = 5, CancellationToken cancellationToken = default)
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
        var codigosRegistrados = items
            .Select(d => d.CodigoDispositivo)
            .OrderBy(codigo => codigo, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var codigo = q.Trim();
            items = items
                .Where(d => string.Equals(d.CodigoDispositivo, codigo, StringComparison.OrdinalIgnoreCase))
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
            Usuarios = usuariosTask.Result.Data ?? [],
            CodigosRegistrados = codigosRegistrados
        });
    }

    /// <summary>Asistente guiado. El administrador no escribe ni conoce el codigo del dispositivo.</summary>
    [HttpGet]
    public IActionResult Crear()
    {
        SetNav("pda", UiTexts.RegistrarPda);
        return View();
    }

    /// <summary>Confirma que el PDA quedo bien preparado antes de iniciar la instalacion.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verificar(CancellationToken cancellationToken)
    {
        var verificacion = await _registro.VerificarAsync(cancellationToken);
        return Json(new
        {
            listo = verificacion.Listo,
            mensaje = verificacion.Mensaje,
            modelo = verificacion.Modelo
        });
    }

    /// <summary>
    /// Ejecuta el registro completo e informa el avance por el hub. La respuesta nunca incluye el
    /// codigo del dispositivo.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(TipoDispositivo tipo, string? conexionId, CancellationToken cancellationToken)
    {
        if (!_candado.Tomar())
        {
            return Json(new { exitoso = false, mensaje = UiTexts.PdaRegistroEnCurso });
        }

        try
        {
            var avance = new AvanceRegistroPdaPorHub(_hub, conexionId);
            var resultado = await _registro.RegistrarAsync(tipo, avance, cancellationToken);
            return Json(new
            {
                exitoso = resultado.Exitoso,
                mensaje = resultado.Mensaje,
                modelo = resultado.Modelo,
                detalle = resultado.Exitoso ? UiTexts.PdaRegistroCompletadoDetalle : null
            });
        }
        finally
        {
            _candado.Liberar();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Asociar(Guid[]? ids, Guid usuarioId, CancellationToken cancellationToken)
    {
        var seleccion = NormalizeIds(ids);
        if (seleccion.Length == 0)
        {
            SetFlash(UiTexts.SeleccionePda, false);
            return RedirectToAction(nameof(Index));
        }

        if (seleccion.Length != 1)
        {
            SetFlash(UiTexts.AsociarUnSoloPda, false);
            return RedirectToAction(nameof(Index));
        }

        if (usuarioId == Guid.Empty)
        {
            SetFlash(UiTexts.SeleccioneUsuario, false);
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.AsociarDispositivoAsync(seleccion[0], usuarioId, cancellationToken);
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
    public async Task<IActionResult> Desasociar(Guid[]? ids, CancellationToken cancellationToken)
    {
        return await RunOnSelectionAsync(ids, id => _api.DesasociarDispositivoAsync(id, cancellationToken), cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(Guid[]? ids, EstadoGeneral estado, CancellationToken cancellationToken)
    {
        return await RunOnSelectionAsync(ids, id => _api.ActualizarDispositivoAsync(id, new ActualizarDispositivoRequest
        {
            Estado = estado
        }, cancellationToken), cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Eliminar(Guid[]? ids, CancellationToken cancellationToken)
    {
        SetNav("pda", UiTexts.EliminarPda);
        var seleccion = NormalizeIds(ids);
        if (seleccion.Length == 0)
        {
            SetFlash(UiTexts.SeleccionePda, false);
            return RedirectToAction(nameof(Index));
        }

        var listado = await _api.ListarDispositivosAsync(cancellationToken);
        var unauthorized = RedirectIfUnauthorized(listado);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var items = (listado.Data ?? []).Where(d => seleccion.Contains(d.DispositivoId)).ToList();
        if (items.Count == 0)
        {
            SetFlash(UsuarioMessages.DispositivoNoEncontrado, false);
            return RedirectToAction(nameof(Index));
        }

        var config = await _api.ObtenerConfiguracionOperativaAsync(cancellationToken);
        var configDenied = RedirectIfUnauthorized(config);
        if (configDenied is not null)
        {
            return configDenied;
        }

        return View(new EliminarDispositivosViewModel
        {
            Items = items,
            DiasInactividadEliminarPda = config.Data?.DiasInactividadEliminarPda > 0
                ? config.Data.DiasInactividadEliminarPda
                : 30
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarEliminar(Guid[]? ids, CancellationToken cancellationToken)
    {
        return await RunOnSelectionAsync(ids, id => _api.EliminarDispositivoAsync(id, cancellationToken), cancellationToken, SuccessMessages.RegistroEliminado);
    }

    private async Task<IActionResult> RunOnSelectionAsync<T>(
        Guid[]? ids,
        Func<Guid, Task<ApiCallResult<T>>> action,
        CancellationToken cancellationToken,
        string? successMessage = null)
    {
        var seleccion = NormalizeIds(ids);
        if (seleccion.Length == 0)
        {
            SetFlash(UiTexts.SeleccionePda, false);
            return RedirectToAction(nameof(Index));
        }

        ApiCallResult<T>? last = null;
        foreach (var id in seleccion)
        {
            last = await action(id);
            var unauthorized = RedirectIfUnauthorized(last);
            if (unauthorized is not null)
            {
                return unauthorized;
            }

            if (!last.Success)
            {
                SetFlash(last.Message, false);
                return RedirectToAction(nameof(Index));
            }
        }

        SetFlash(successMessage ?? SuccessMessages.RegistroActualizado, true);
        return RedirectToAction(nameof(Index));
    }

    private static Guid[] NormalizeIds(Guid[]? ids) =>
        (ids ?? []).Where(id => id != Guid.Empty).Distinct().ToArray();
}
