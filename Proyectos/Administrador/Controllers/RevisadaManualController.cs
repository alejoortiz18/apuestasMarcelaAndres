using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.RevisadaManual;
using NewRich.Constants.Messages;
using NewRich.Domain.Services;

namespace NewRich.Admin.Controllers;

public sealed class RevisadaManualController : AdminControllerBase
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");
    private readonly IAdminApiClient _api;

    public RevisadaManualController(IAdminApiClient api) => _api = api;

    [HttpGet]
    public Task<IActionResult> Resumen(DateTime? desde, DateTime? hasta, string? valorMinimo, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default) =>
        MostrarAsync("resumen", desde, hasta, valorMinimo, null, page, pageSize, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PorCubrir(List<string>? claves, DateTime desde = default, DateTime hasta = default, string? valorMinimo = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _api.RevisadaMarcarPorCubrirAsync(new MarcarPorCubrirRevisadaManualRequest { Claves = claves ?? [] }, cancellationToken);
        var negado = RedirectIfUnauthorized(result);
        if (negado is not null)
        {
            return negado;
        }

        if (!result.Success)
        {
            SetAvisoModal(result.Message);
            return await MostrarAsync("resumen", desde, hasta, valorMinimo, null, page, pageSize, cancellationToken);
        }

        SetAvisoModal(result.Message);
        return RedirectToAction(nameof(Resumen), new { desde = desde.ToString("yyyy-MM-dd"), hasta = hasta.ToString("yyyy-MM-dd"), valorMinimo, page, pageSize, orden = OrdenActual() });
    }

    [HttpGet]
    [ActionName("PorCubrir")]
    public Task<IActionResult> ListaPorCubrir(DateTime? desde, DateTime? hasta, string? comision, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default) =>
        MostrarAsync("porcubrir", desde, hasta, null, comision, page, pageSize, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cubrir(Guid operacionId = default, string? comision = null, DateTime desde = default, DateTime hasta = default, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(comision))
        {
            SetAvisoModal(UiTexts.ComisionRequerida);
            return await MostrarAsync("porcubrir", desde, hasta, null, comision, page, pageSize, cancellationToken);
        }

        if (!DosDigitos(comision, out var porcentaje))
        {
            SetAvisoModal(UiTexts.ComisionDosDigitos);
            return await MostrarAsync("porcubrir", desde, hasta, null, comision, page, pageSize, cancellationToken);
        }

        var result = await _api.RevisadaCubrirAsync(new CubrirRevisadaManualRequest
        {
            OperacionId = operacionId,
            ComisionPorcentaje = porcentaje
        }, cancellationToken);
        var negado = RedirectIfUnauthorized(result);
        if (negado is not null)
        {
            return negado;
        }

        SetAvisoModal(result.Success ? result.Message : result.Message);
        return result.Success
            ? RedirectToAction("PorCubrir", new { desde = desde.ToString("yyyy-MM-dd"), hasta = hasta.ToString("yyyy-MM-dd"), comision, page, pageSize, orden = OrdenActual() })
            : await MostrarAsync("porcubrir", desde, hasta, null, comision, page, pageSize, cancellationToken);
    }

    [HttpGet]
    public Task<IActionResult> Cubierto(DateTime? desde, DateTime? hasta, string? descuento, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default) =>
        MostrarAsync("cubierto", desde, hasta, null, descuento, page, pageSize, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Descuento(Guid operacionId = default, string? descuento = null, DateTime desde = default, DateTime hasta = default, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(descuento))
        {
            SetAvisoModal(UiTexts.DescuentoRequerido);
            return await MostrarAsync("cubierto", desde, hasta, null, descuento, page, pageSize, cancellationToken);
        }

        if (!DosDigitos(descuento, out var porcentaje))
        {
            SetAvisoModal(UiTexts.DescuentoDosDigitos);
            return await MostrarAsync("cubierto", desde, hasta, null, descuento, page, pageSize, cancellationToken);
        }

        var result = await _api.RevisadaDescuentoAsync(new DescuentoRevisadaManualRequest
        {
            OperacionId = operacionId,
            DescuentoPorcentaje = porcentaje
        }, cancellationToken);
        var negado = RedirectIfUnauthorized(result);
        if (negado is not null)
        {
            return negado;
        }

        SetAvisoModal(result.Message);
        return result.Success
            ? RedirectToAction(nameof(Cubierto), new { desde = desde.ToString("yyyy-MM-dd"), hasta = hasta.ToString("yyyy-MM-dd"), descuento, page, pageSize, orden = OrdenActual() })
            : await MostrarAsync("cubierto", desde, hasta, null, descuento, page, pageSize, cancellationToken);
    }

    private async Task<IActionResult> MostrarAsync(string modo, DateTime? desde, DateTime? hasta, string? valorMinimo, string? porcentaje, int page, int pageSize, CancellationToken cancellationToken)
    {
        SetNav(modo switch
        {
            "porcubrir" => "revisada-por-cubrir",
            "cubierto" => "revisada-cubierto",
            _ => "revisada-resumen"
        }, modo switch
        {
            "porcubrir" => UiTexts.NavRevisadaPorCubrir,
            "cubierto" => UiTexts.NavRevisadaCubierto,
            _ => UiTexts.NavRevisadaResumen
        });

        if (modo is "porcubrir" or "cubierto")
        {
            porcentaje = await PorcentajeConfiguradoAsync(modo, porcentaje, cancellationToken);
        }

        var inicio = (desde ?? DateTime.Today).Date;
        var finDia = (hasta ?? DateTime.Today).Date;
        if (finDia < inicio)
        {
            finDia = inicio;
        }

        var fin = finDia.AddDays(1).AddTicks(-1);
        decimal? minimo = null;
        if (modo == "resumen" && !string.IsNullOrWhiteSpace(valorMinimo))
        {
            if (!Pesos(valorMinimo, out var valor) || valor < 0)
            {
                SetAvisoModal(RevisadaManualMessages.ValorMinimoInvalido);
            }
            else
            {
                minimo = valor;
            }
        }

        var consulta = modo switch
        {
            "porcubrir" => await _api.RevisadaPorCubrirAsync(inicio, fin, cancellationToken),
            "cubierto" => await _api.RevisadaCubiertoAsync(inicio, fin, cancellationToken),
            _ => await _api.RevisadaResumenAsync(inicio, fin, minimo, cancellationToken)
        };
        var negado = RedirectIfUnauthorized(consulta);
        if (negado is not null)
        {
            return negado;
        }

        if (!consulta.Success)
        {
            SetAvisoModal(consulta.Message);
        }

        var grupos = consulta.Data?.Grupos ?? [];
        var orden = OrdenActual();
        var loterias = grupos
            .GroupBy(g => g.Loteria)
            .Select(g => new LoteriaRevisadaViewModel { Nombre = g.Key, Grupos = g.ToList() })
            .ToList();
        loterias = orden switch
        {
            "asc" => loterias.OrderBy(l => l.Grupos.Sum(g => g.Subtotal)).ThenBy(l => l.Nombre).ToList(),
            "desc" => loterias.OrderByDescending(l => l.Grupos.Sum(g => g.Subtotal)).ThenBy(l => l.Nombre).ToList(),
            _ => loterias
        };
        return View(modo == "resumen" ? "Resumen" : modo == "porcubrir" ? "PorCubrir" : "Cubierto", new RevisadaPaginaViewModel
        {
            Modo = modo,
            Desde = inicio,
            Hasta = finDia,
            ValorMinimo = valorMinimo ?? string.Empty,
            Porcentaje = porcentaje ?? string.Empty,
            Orden = orden ?? string.Empty,
            Jugadas = consulta.Data?.Jugadas ?? 0,
            Loterias = consulta.Data?.Loterias ?? 0,
            Total = consulta.Data?.Total ?? 0,
            Pagina = PagingHelper.Paginate(loterias, page, pageSize)
        });
    }

    private async Task<string?> PorcentajeConfiguradoAsync(string modo, string? porcentaje, CancellationToken cancellationToken)
    {
        var guardados = await _api.ParametrosRevisadaManualAsync(cancellationToken);
        var guardado = modo == "porcubrir" ? guardados.Data?.ComisionPorcentaje : guardados.Data?.DescuentoPorcentaje;
        if (string.IsNullOrWhiteSpace(porcentaje))
        {
            return guardado is null ? string.Empty : guardado.Value.ToString("0", CultureInfo.InvariantCulture);
        }

        if (DosDigitos(porcentaje, out var nuevo))
        {
            var guardadoResult = modo == "porcubrir"
                ? await _api.GuardarComisionRevisadaManualAsync(nuevo, cancellationToken)
                : await _api.GuardarDescuentoRevisadaManualAsync(nuevo, cancellationToken);
            if (!guardadoResult.Success)
            {
                SetAvisoModal(guardadoResult.Message);
            }
        }

        return porcentaje;
    }

    private string? OrdenActual()
    {
        var texto = Request.HasFormContentType && Request.Form.ContainsKey("orden")
            ? Request.Form["orden"].ToString()
            : Request.Query["orden"].ToString();
        return texto is "asc" or "desc" ? texto : null;
    }

    private static bool Pesos(string texto, out decimal valor)
    {
        var limpio = new string(texto.Where(char.IsDigit).ToArray());
        return decimal.TryParse(limpio, NumberStyles.None, CultureInfo.InvariantCulture, out valor) && limpio.Length > 0;
    }

    internal static bool DosDigitos(string? texto, out decimal valor)
    {
        valor = 0;
        var limpio = (texto ?? string.Empty).Trim();
        return limpio.Length is >= 1 and <= 2
            && limpio.All(char.IsDigit)
            && decimal.TryParse(limpio, NumberStyles.None, CultureInfo.InvariantCulture, out valor);
    }

    internal static bool Porcentaje(string? texto, out decimal valor)
    {
        valor = 0;
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        return decimal.TryParse(texto.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out valor)
            && RevisadaManualCalculo.PorcentajeValido(valor);
    }
}
