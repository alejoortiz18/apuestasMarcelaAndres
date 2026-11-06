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
    private const int TamanoHistorico = 20;
    private const string OrdenHistoricoInicial = "fecha";
    private readonly IAdminApiClient _api;

    public RevisadaManualController(IAdminApiClient api) => _api = api;

    [HttpGet]
    public Task<IActionResult> Resumen(DateTime? desde, DateTime? hasta, string? valorMinimo, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default) =>
        MostrarAsync("resumen", desde, hasta, valorMinimo, null, null, page, pageSize, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cubrir(List<string>? claves, DateTime desde = default, DateTime hasta = default, string? valorMinimo = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _api.RevisadaCubrirDesdeResumenAsync(new CubrirDesdeResumenRevisadaManualRequest
        {
            Claves = claves ?? []
        }, cancellationToken);
        var negado = RedirectIfUnauthorized(result);
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
        var modelo = new RecubrirPreviaViewModel
        {
            Desde = inicio,
            Hasta = finDia,
            ValorMinimo = valorMinimo ?? string.Empty,
            Page = page,
            PageSize = pageSize,
            Orden = OrdenActual() ?? string.Empty
        };
        if (RangoParteActual(inicio, finDia) is not { } parte)
        {
            return PartialView("_RecubrirPrevia", new RecubrirPreviaViewModel { Error = UiTexts.RecubrirSinPendientes });
        }

        var previa = await _api.RevisadaRecubrirPreviaAsync(parte.Inicio, parte.Fin.AddDays(1).AddTicks(-1), minimo, cancellationToken);
        var negado = RedirectIfUnauthorized(previa);
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
        var result = await _api.RevisadaRecubrirAsync(new RecubrirRevisadaManualRequest
        {
            Claves = claves ?? []
        }, cancellationToken);
        var negado = RedirectIfUnauthorized(result);
        if (negado is not null)
        {
            return negado;
        }

        SetAvisoModal(result.Success && result.Data is not null
            ? string.Format(Cultura, UiTexts.RecubrirResultado, result.Message, result.Data.Grupos.Count, result.Data.Jugadas)
            : result.Message);
        return volver;
    }

    [HttpGet]
    public Task<IActionResult> Cubierto(DateTime? desde, DateTime? hasta, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default) =>
        MostrarAsync("cubierto", desde, hasta, null, null, null, page, pageSize, cancellationToken);

    private async Task<IActionResult> MostrarAsync(string modo, DateTime? desde, DateTime? hasta, string? valorMinimo, string? comision, string? porcentaje, int page, int pageSize, CancellationToken cancellationToken)
    {
        var cubierto = modo == "cubierto";
        SetNav(cubierto ? "revisada-cubierto" : "revisada-resumen", cubierto ? UiTexts.NavRevisadaCubierto : UiTexts.NavRevisadaResumen);

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

        var hoy = DateTime.Today;
        var parteActual = RangoParteActual(inicio, finDia);
        var rangoHistorico = cubierto ? null : RangoHistoricoResumen(inicio, finDia);
        var incluyeHoy = cubierto ? inicio <= hoy && hoy <= finDia : parteActual is not null;
        var vacia = ApiCallResult<VistaRevisadaManualResponse>.Ok(new VistaRevisadaManualResponse(), string.Empty);
        var consulta = !cubierto
            ? parteActual is { } parte
                ? await _api.RevisadaResumenAsync(parte.Inicio, parte.Fin.AddDays(1).AddTicks(-1), minimo, cancellationToken)
                : vacia
            : incluyeHoy
                ? await _api.RevisadaCubiertoHoyAsync(cancellationToken)
                : vacia;
        var negado = RedirectIfUnauthorized(consulta);
        if (negado is not null)
        {
            return negado;
        }

        ResumenHistoricoResponse? historicoResumen = null;
        if (rangoHistorico is { } rango)
        {
            var respuesta = await _api.RevisadaResumenHistoricoAsync(rango.Inicio, rango.Fin, OrdenHistoricoInicial, true, 1, TamanoHistorico, cancellationToken);
            negado = RedirectIfUnauthorized(respuesta);
            if (negado is not null)
            {
                return negado;
            }

            historicoResumen = respuesta.Data ?? new ResumenHistoricoResponse();
        }

        CubiertoHistoricoResponse? historico = null;
        if (cubierto)
        {
            var respuesta = await _api.RevisadaCubiertoHistoricoAsync(inicio, finDia, OrdenHistoricoInicial, true, 1, TamanoHistorico, cancellationToken);
            negado = RedirectIfUnauthorized(respuesta);
            if (negado is not null)
            {
                return negado;
            }

            historico = respuesta.Data ?? new CubiertoHistoricoResponse();
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
            Pagina = PagingHelper.Paginate(loterias, page, pageSize),
            IncluyeHoy = incluyeHoy,
            Historico = historico,
            HistoricoResumen = historicoResumen,
            HistoricoDesde = rangoHistorico?.Inicio ?? inicio,
            HistoricoHasta = rangoHistorico?.Fin ?? finDia
        });
    }

    // En Resumen lo de hoy en adelante se puede cubrir; lo de días anteriores va al histórico.
    private static (DateTime Inicio, DateTime Fin)? RangoParteActual(DateTime inicio, DateTime finDia) =>
        finDia < DateTime.Today ? null : (inicio < DateTime.Today ? DateTime.Today : inicio, finDia);

    private static (DateTime Inicio, DateTime Fin)? RangoHistoricoResumen(DateTime inicio, DateTime finDia) =>
        inicio >= DateTime.Today ? null : (inicio, finDia < DateTime.Today ? finDia : DateTime.Today.AddDays(-1));

    [HttpGet]
    public async Task<IActionResult> ResumenTabla(DateTime desde, DateTime hasta, string? orden, bool descendente = false, int pagina = 1, CancellationToken cancellationToken = default)
    {
        var respuesta = await _api.RevisadaResumenHistoricoAsync(desde.Date, hasta.Date, orden, descendente, pagina, TamanoHistorico, cancellationToken);
        var negado = RedirectIfUnauthorized(respuesta);
        if (negado is not null)
        {
            return negado;
        }

        return PartialView("_ResumenFilas", new ResumenFilasViewModel
        {
            Filas = respuesta.Data?.Filas ?? [],
            HayMas = respuesta.Data?.HayMas ?? false,
            Siguiente = pagina + 1
        });
    }

    [HttpGet]
    public async Task<IActionResult> CubiertoTabla(DateTime desde, DateTime hasta, string? orden, bool descendente = false, int pagina = 1, CancellationToken cancellationToken = default)
    {
        var respuesta = await _api.RevisadaCubiertoHistoricoAsync(desde.Date, hasta.Date, orden, descendente, pagina, TamanoHistorico, cancellationToken);
        var negado = RedirectIfUnauthorized(respuesta);
        if (negado is not null)
        {
            return negado;
        }

        return PartialView("_CubiertoFilas", new CubiertoFilasViewModel
        {
            Filas = respuesta.Data?.Filas ?? [],
            HayMas = respuesta.Data?.HayMas ?? false,
            Siguiente = pagina + 1
        });
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
