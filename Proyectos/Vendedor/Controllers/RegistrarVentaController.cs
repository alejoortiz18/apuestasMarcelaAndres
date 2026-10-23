using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.VentasRegistradasWeb;
using NewRich.Constants.Messages;
using NewRich.Domain.Services;
using NewRich.Vendedor.Constants;
using NewRich.Vendedor.Models;
using NewRich.Vendedor.Services;

namespace NewRich.Vendedor.Controllers;

public sealed class RegistrarVentaController : VendedorController
{
    private const int FilasPorPagina = 10;
    private const int DigitosMaximos = 12;
    private static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");
    private readonly IVendedorApiClient _api;

    public RegistrarVentaController(IVendedorApiClient api) => _api = api;

    [HttpGet]
    public Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        Nav("registrar-venta", UiTexts.RegistrarVenta);
        return PaginaAsync(page, null, null, cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(RegistrarVentaViewModel model, int page = 1, CancellationToken cancellationToken = default)
    {
        Nav("registrar-venta", UiTexts.RegistrarVenta);
        model.Password = model.Password?.Trim();
        if (!Pesos(model.Valor, out var valor))
        {
            ViewData["AvisoPagina"] = UiTexts.ValorVentaObligatorio;
            return await PaginaAsync(page, model, null, cancellationToken);
        }

        var guardada = await _api.RegistrarVentaWebAsync(new RegistrarVentaWebRequest(model.FechaVenta, valor, model.Password), cancellationToken);
        var negado = SiNoAutorizado(guardada);
        if (negado is not null)
        {
            return negado;
        }

        if (!guardada.Success || guardada.Data is null)
        {
            if (guardada.Message is VentaRegistradaWebMessages.ContrasenaIncorrecta or VentaRegistradaWebMessages.ContrasenaRequerida)
            {
                return await PaginaAsync(page, model, guardada.Message, cancellationToken);
            }

            ViewData["AvisoPagina"] = guardada.Message;
            return await PaginaAsync(page, model, null, cancellationToken);
        }

        Aviso(string.Format(Colombia, UiTexts.VentaRegistrada, Montos.De(guardada.Data.Valor), guardada.Data.FechaVenta.ToString("dd-MM-yyyy")));
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> PaginaAsync(int page, RegistrarVentaViewModel? formulario, string? errorConfirmacion, CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        page = Math.Max(1, page);
        var consulta = await _api.MisVentasRegistradasAsync(page, FilasPorPagina, cancellationToken);
        var negado = SiNoAutorizado(consulta);
        if (negado is not null)
        {
            return negado;
        }

        var datos = consulta.Data;
        var total = datos?.TotalFilas ?? 0;
        var valida = Paginas.Numero(page, total, FilasPorPagina);
        if (consulta.Success && valida != page && formulario is null)
        {
            return RedirectToAction(nameof(Index), new { page = valida });
        }

        formulario ??= new RegistrarVentaViewModel();
        formulario.FechaVenta = hoy;
        formulario.Password = null;
        return View("Index", new RegistrarVentaPaginaViewModel
        {
            Hoy = hoy,
            Formulario = formulario,
            AbrirConfirmacion = errorConfirmacion is not null,
            ErrorConfirmacion = errorConfirmacion,
            AvisoListado = consulta.Success ? null : consulta.Message,
            Items = (datos?.Items ?? []).Select(v => new VentaRegistradaFilaViewModel
            {
                FechaVenta = v.FechaVenta.ToString("dd-MM-yyyy"),
                Valor = Montos.De(v.Valor),
                Registrada = ZonaHorariaColombia.ALocal(v.FechaRegistro).ToString("dd-MM-yyyy hh:mm tt", Colombia)
            }).ToList(),
            Paginacion = new PaginacionViewModel
            {
                Page = valida,
                PageSize = FilasPorPagina,
                Total = total,
                Accion = nameof(Index),
                Controlador = "RegistrarVenta"
            }
        });
    }

    private static bool Pesos(string? texto, out decimal valor)
    {
        valor = 0;
        var limpio = (texto ?? string.Empty).Replace(".", string.Empty).Trim();
        return limpio.Length is > 0 and <= DigitosMaximos
            && limpio.All(char.IsDigit)
            && decimal.TryParse(limpio, NumberStyles.None, CultureInfo.InvariantCulture, out valor)
            && valor > 0;
    }
}
