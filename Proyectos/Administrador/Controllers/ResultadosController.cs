using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Resultados;
using NewRich.Constants.Messages;

namespace NewRich.Admin.Controllers;

public sealed class ResultadosController : AdminControllerBase
{
    private readonly IAdminApiClient _api;

    public ResultadosController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(DateOnly? fecha, Guid? loteriaId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        SetNav("resultados", UiTexts.NavResultados);
        var loteriasTask = _api.ListarLoteriasAsync(cancellationToken);
        var resultadosTask = _api.ListarResultadosAsync(fecha, loteriaId, cancellationToken);
        await Task.WhenAll(loteriasTask, resultadosTask);
        var unauthorized = RedirectIfUnauthorized(loteriasTask.Result) ?? RedirectIfUnauthorized(resultadosTask.Result);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var resultados = (resultadosTask.Result.Data ?? []).ToList();
        foreach (var resultado in resultados)
        {
            var ganadores = await _api.FiltrarBoletosAsync(new FiltroBoletosRequest
            {
                Estado = "Ganador",
                Numero = resultado.Numero,
                LoteriaId = resultado.LoteriaId,
                FechaInicial = resultado.FechaJuego.ToDateTime(TimeOnly.MinValue),
                FechaFinal = resultado.FechaJuego.ToDateTime(TimeOnly.MaxValue)
            }, cancellationToken);

            if (ganadores.Success)
            {
                resultado.CantidadGanadores = ganadores.Data?.Count ?? 0;
            }
        }

        ViewBag.Fecha = fecha?.ToString("yyyy-MM-dd");
        ViewBag.LoteriaId = loteriaId;
        return View(new ResultadosIndexViewModel
        {
            Fecha = fecha,
            LoteriaId = loteriaId,
            Loterias = loteriasTask.Result.Data ?? [],
            Pagina = PagingHelper.Paginate(resultados, page, pageSize)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Ganadores(Guid numeroGanadorId, CancellationToken cancellationToken = default)
    {
        SetNav("resultados", UiTexts.NavResultados);
        var resultados = await _api.ListarResultadosAsync(null, null, cancellationToken);
        var unauthorized = RedirectIfUnauthorized(resultados);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var resultado = resultados.Data?.FirstOrDefault(r => r.NumeroGanadorId == numeroGanadorId);
        if (resultado is null)
        {
            SetFlash("No se encontró el resultado seleccionado.", false);
            return RedirectToAction(nameof(Index));
        }

        var ganadores = await _api.FiltrarBoletosAsync(new FiltroBoletosRequest
        {
            Estado = "Ganador",
            Numero = resultado.Numero,
            LoteriaId = resultado.LoteriaId,
            FechaInicial = resultado.FechaJuego.ToDateTime(TimeOnly.MinValue),
            FechaFinal = resultado.FechaJuego.ToDateTime(TimeOnly.MaxValue)
        }, cancellationToken);

        var unauthorizedGanadores = RedirectIfUnauthorized(ganadores);
        if (unauthorizedGanadores is not null)
        {
            return unauthorizedGanadores;
        }

        return View(new ResultadoGanadoresViewModel
        {
            Resultado = resultado,
            Ganadores = ganadores.Data ?? []
        });
    }

    [HttpGet]
    public async Task<IActionResult> Crear(CancellationToken cancellationToken)
    {
        SetNav("resultados", UiTexts.RegistrarResultado);
        var loterias = await _api.ListarLoteriasAsync(cancellationToken);
        var unauthorized = RedirectIfUnauthorized(loterias);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        return View(new ResultadoFormViewModel
        {
            Loterias = loterias.Data ?? [],
            FechaJuego = DateOnly.FromDateTime(DateTime.Today)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ResultadoFormViewModel model, CancellationToken cancellationToken)
    {
        SetNav("resultados", UiTexts.RegistrarResultado);
        var loterias = await _api.ListarLoteriasAsync(cancellationToken);
        model.Loterias = loterias.Data ?? [];
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _api.RegistrarResultadoAsync(new RegistrarResultadoRequest
        {
            LoteriaId = model.LoteriaId,
            FechaJuego = model.FechaJuego,
            Numero = model.Numero.Trim()
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

        SetFlash(SuccessMessages.ResultadoRegistrado);
        return RedirectToAction(nameof(Index));
    }
}
