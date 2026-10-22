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
        MostrarAsync("resumen", desde, hasta, valorMinimo, null, null, page, pageSize, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cubrir(List<string>? claves, DateTime desde = default, DateTime hasta = default, string? valorMinimo = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var parametros = await _api.ParametrosRevisadaManualAsync(cancellationToken);
        var negado = RedirectIfUnauthorized(parametros);
        if (negado is not null)
        {
            return negado;
        }

        if (parametros.Data?.ComisionPorcentaje is not { } comision || parametros.Data.DescuentoPorcentaje is not { } descuento)
        {
            SetAvisoModal(UiTexts.PorcentajesSinConfigurar);
            return await MostrarAsync("resumen", desde, hasta, valorMinimo, null, null, page, pageSize, cancellationToken);
        }

        var result = await _api.RevisadaCubrirDesdeResumenAsync(new CubrirDesdeResumenRevisadaManualRequest
        {
            Claves = claves ?? [],
            ComisionPorcentaje = comision,
            DescuentoPorcentaje = descuento
        }, cancellationToken);
        negado = RedirectIfUnauthorized(result);
        if (negado is not null)
        {
            return negado;
        }

        SetAvisoModal(result.Message);
        return result.Success
            ? RedirectToAction(nameof(Resumen), new { desde = desde.ToString("yyyy-MM-dd"), hasta = hasta.ToString("yyyy-MM-dd"), valorMinimo, page, pageSize, orden = OrdenActual() })
            : await MostrarAsync("resumen", desde, hasta, valorMinimo, null, null, page, pageSize, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> RecubrirPrevia(DateTime? desde, DateTime? hasta, string? valorMinimo, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var inicio = (desde ?? DateTime.Today).Date;
        var finDia = (hasta ?? DateTime.Today).Date;
        if (finDia < inicio)
        {
            finDia = inicio;
        }

        decimal? minimo = !string.IsNullOrWhiteSpace(valorMinimo) && Pesos(valorMinimo, out var valor) ? valor : null;
        var parametros = await _api.ParametrosRevisadaManualAsync(cancellationToken);
        var negado = RedirectIfUnauthorized(parametros);
        if (negado is not null)
        {
            return negado;
        }

        var modelo = new RecubrirPreviaViewModel
        {
            Desde = inicio,
            Hasta = finDia,
            ValorMinimo = valorMinimo ?? string.Empty,
            Page = page,
            PageSize = pageSize,
            Orden = OrdenActual() ?? string.Empty
        };
        if (parametros.Data?.ComisionPorcentaje is not { } comision || parametros.Data.DescuentoPorcentaje is not { } descuento)
        {
            return PartialView("_RecubrirPrevia", new RecubrirPreviaViewModel { Error = UiTexts.PorcentajesSinConfigurar });
        }

        var previa = await _api.RevisadaRecubrirPreviaAsync(inicio, finDia.AddDays(1).AddTicks(-1), minimo, cancellationToken);
        negado = RedirectIfUnauthorized(previa);
        if (negado is not null)
        {
            return negado;
        }

        if (!previa.Success || previa.Data is null)
        {
            return PartialView("_RecubrirPrevia", new RecubrirPreviaViewModel { Error = previa.Message });
        }

        return PartialView("_RecubrirPrevia", new RecubrirPreviaViewModel
        {
            Comision = comision,
            Descuento = descuento,
            Vista = previa.Data,
            Desde = modelo.Desde,
            Hasta = modelo.Hasta,
            ValorMinimo = modelo.ValorMinimo,
            Page = modelo.Page,
            PageSize = modelo.PageSize,
            Orden = modelo.Orden
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Recubrir(List<string>? claves, DateTime desde = default, DateTime hasta = default, string? valorMinimo = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var volver = RedirectToAction(nameof(Resumen), new { desde = desde.ToString("yyyy-MM-dd"), hasta = hasta.ToString("yyyy-MM-dd"), valorMinimo, page, pageSize, orden = OrdenActual() });
        var parametros = await _api.ParametrosRevisadaManualAsync(cancellationToken);
        var negado = RedirectIfUnauthorized(parametros);
        if (negado is not null)
        {
            return negado;
        }

        if (parametros.Data?.ComisionPorcentaje is not { } comision || parametros.Data.DescuentoPorcentaje is not { } descuento)
        {
            SetAvisoModal(UiTexts.PorcentajesSinConfigurar);
            return volver;
        }

        var result = await _api.RevisadaRecubrirAsync(new RecubrirRevisadaManualRequest
        {
            Claves = claves ?? [],
            ComisionPorcentaje = comision,
            DescuentoPorcentaje = descuento
        }, cancellationToken);
        negado = RedirectIfUnauthorized(result);
        if (negado is not null)
        {
            return negado;
        }

        SetAvisoModal(result.Success && result.Data is not null
            ? string.Format(Cultura, UiTexts.RecubrirResultado, result.Message, result.Data.Grupos.Count, result.Data.Jugadas)
            : result.Message);
        return volver;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarPorcentajes(string? comision, string? descuento, DateTime desde = default, DateTime hasta = default, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var volver = RedirectToAction(nameof(Cubierto), new { desde = desde.ToString("yyyy-MM-dd"), hasta = hasta.ToString("yyyy-MM-dd"), pageSize, orden = OrdenActual() });
        var conComision = !string.IsNullOrWhiteSpace(comision);
        var conDescuento = !string.IsNullOrWhiteSpace(descuento);
        if (!conComision && !conDescuento)
        {
            SetAvisoModal(UiTexts.PorcentajesRequeridos);
            return volver;
        }

        decimal valorComision = 0;
        decimal valorDescuento = 0;
        if ((conComision && !DosDigitos(comision, out valorComision)) || (conDescuento && !DosDigitos(descuento, out valorDescuento)))
        {
            SetAvisoModal(conComision && !DosDigitos(comision, out _) ? UiTexts.ComisionDosDigitos : UiTexts.DescuentoDosDigitos);
            return volver;
        }

        if (conComision)
        {
            var guardada = await _api.GuardarComisionRevisadaManualAsync(valorComision, cancellationToken);
            var negado = RedirectIfUnauthorized(guardada);
            if (negado is not null)
            {
                return negado;
            }

            if (!guardada.Success)
            {
                SetAvisoModal(guardada.Message);
                return volver;
            }
        }

        if (conDescuento)
        {
            var guardado = await _api.GuardarDescuentoRevisadaManualAsync(valorDescuento, cancellationToken);
            var negado = RedirectIfUnauthorized(guardado);
            if (negado is not null)
            {
                return negado;
            }

            if (!guardado.Success)
            {
                SetAvisoModal(guardado.Message);
                return volver;
            }
        }

        SetAvisoModal(UiTexts.PorcentajesGuardados);
        return volver;
    }

    [HttpGet]
    public Task<IActionResult> Cubierto(DateTime? desde, DateTime? hasta, string? comision, string? descuento, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default) =>
        MostrarAsync("cubierto", desde, hasta, null, comision, descuento, page, pageSize, cancellationToken);

    private async Task<IActionResult> MostrarAsync(string modo, DateTime? desde, DateTime? hasta, string? valorMinimo, string? comision, string? porcentaje, int page, int pageSize, CancellationToken cancellationToken)
    {
        var cubierto = modo == "cubierto";
        SetNav(cubierto ? "revisada-cubierto" : "revisada-resumen", cubierto ? UiTexts.NavRevisadaCubierto : UiTexts.NavRevisadaResumen);

        if (cubierto)
        {
            var guardados = (await _api.ParametrosRevisadaManualAsync(cancellationToken)).Data;
            comision = Mostrado(comision, guardados?.ComisionPorcentaje);
            porcentaje = Mostrado(porcentaje, guardados?.DescuentoPorcentaje);
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

        var consulta = cubierto
            ? await _api.RevisadaCubiertoAsync(inicio, fin, cancellationToken)
            : await _api.RevisadaResumenAsync(inicio, fin, minimo, cancellationToken);
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
        return View(cubierto ? "Cubierto" : "Resumen", new RevisadaPaginaViewModel
        {
            Modo = modo,
            Desde = inicio,
            Hasta = finDia,
            ValorMinimo = valorMinimo ?? string.Empty,
            Comision = comision ?? string.Empty,
            Porcentaje = porcentaje ?? string.Empty,
            Orden = orden ?? string.Empty,
            Jugadas = consulta.Data?.Jugadas ?? 0,
            Loterias = consulta.Data?.Loterias ?? 0,
            Total = consulta.Data?.Total ?? 0,
            Pagina = PagingHelper.Paginate(loterias, page, pageSize)
        });
    }

    private static string Mostrado(string? escrito, decimal? guardado) =>
        !string.IsNullOrWhiteSpace(escrito)
            ? escrito
            : guardado is null ? string.Empty : guardado.Value.ToString("0", CultureInfo.InvariantCulture);

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
