using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Application.Contracts.Utilidades;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Vendedor.Constants;
using NewRich.Vendedor.Models;
using NewRich.Vendedor.Services;

namespace NewRich.Vendedor.Controllers;

public sealed class GastosController : VendedorController
{
    private static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");
    private readonly IVendedorApiClient _api;

    public GastosController(IVendedorApiClient api) => _api = api;

    [HttpGet]
    public Task<IActionResult> Index(string? periodo, DateOnly? desde, DateOnly? hasta, int page = 1, int pageSize = 10, bool abrir = false, CancellationToken cancellationToken = default)
    {
        Nav("mis-gastos", UiTexts.MisGastos);
        return PaginaAsync(periodo, desde, hasta, page, pageSize, null, abrir, cancellationToken);
    }

    [HttpGet]
    public IActionResult Registrar() => RedirectToAction(nameof(Index), new { abrir = true });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(RegistrarGastoViewModel model, string? periodo, DateOnly? desde, DateOnly? hasta, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        Nav("mis-gastos", UiTexts.MisGastos);
        var tipos = await _api.TiposGastoAsync(cancellationToken);
        var negado = SiNoAutorizado(tipos);
        if (negado is not null)
        {
            return negado;
        }

        model.Tipos = (tipos.Data ?? []).Select(t => new TipoGastoOpcionViewModel
        {
            Id = t.Id,
            Nombre = t.Nombre,
            RequiereObservacion = t.RequiereObservacion
        }).ToList();
        model.ClaveIdempotencia = string.IsNullOrWhiteSpace(model.ClaveIdempotencia)
            ? Guid.NewGuid().ToString("N")
            : model.ClaveIdempotencia;

        if (model.Tipos.Count == 0)
        {
            model.Error = tipos.Success ? UiTexts.SinTiposGasto : tipos.Message;
            return await PaginaAsync(periodo, desde, hasta, page, pageSize, model, true, cancellationToken);
        }

        if (model.TipoGastoId == Guid.Empty)
        {
            model.Error = UiTexts.TipoObligatorio;
            return await PaginaAsync(periodo, desde, hasta, page, pageSize, model, true, cancellationToken);
        }

        if (model.FechaGasto == default)
        {
            model.Error = UiTexts.FechaObligatoria;
            return await PaginaAsync(periodo, desde, hasta, page, pageSize, model, true, cancellationToken);
        }

        if (!Pesos(model.Valor, out var valor))
        {
            model.Error = UiTexts.ValorObligatorio;
            return await PaginaAsync(periodo, desde, hasta, page, pageSize, model, true, cancellationToken);
        }

        var guardado = await _api.RegistrarGastoAsync(
            new RegistrarGastoRequest(model.TipoGastoId, model.FechaGasto, valor, model.Observacion, model.ClaveIdempotencia),
            cancellationToken);
        negado = SiNoAutorizado(guardado);
        if (negado is not null)
        {
            return negado;
        }

        if (!guardado.Success || guardado.Data is null)
        {
            model.Error = guardado.Message;
            return await PaginaAsync(periodo, desde, hasta, page, pageSize, model, true, cancellationToken);
        }

        Aviso(string.Format(
            Colombia,
            UiTexts.GastoRegistrado,
            guardado.Data.Tipo,
            Montos.De(guardado.Data.Valor),
            guardado.Data.FechaGasto.ToString("dd-MM-yyyy")));
        return RedirectToAction(nameof(Index), new { periodo, desde = Fecha(desde), hasta = Fecha(hasta), page, pageSize });
    }

    private async Task<IActionResult> PaginaAsync(
        string? periodo,
        DateOnly? desde,
        DateOnly? hasta,
        int page,
        int pageSize,
        RegistrarGastoViewModel? formulario,
        bool abrir,
        CancellationToken cancellationToken)
    {
        periodo = string.IsNullOrWhiteSpace(periodo) ? "mes" : periodo;
        pageSize = Paginas.Tamano(pageSize);
        var consulta = await _api.MisGastosAsync(periodo, desde, hasta, page, pageSize, cancellationToken);
        var negado = SiNoAutorizado(consulta);
        if (negado is not null)
        {
            return negado;
        }

        formulario = await FormularioAsync(formulario, cancellationToken);
        if (!consulta.Success || consulta.Data is null)
        {
            return View("Index", new MisGastosViewModel
            {
                Periodo = periodo,
                Desde = desde,
                Hasta = hasta,
                Paginacion = new PaginacionViewModel { Page = 1, PageSize = pageSize, Total = 0, Accion = nameof(Index), Controlador = "Gastos" },
                Aviso = consulta.Message,
                Formulario = formulario,
                AbrirModal = abrir || !string.IsNullOrWhiteSpace(formulario.Error)
            });
        }

        var valida = Paginas.Numero(page, consulta.Data.TotalFilas, pageSize);
        if (valida != page && formulario.Error is null)
        {
            return RedirectToAction(nameof(Index), new { periodo, desde = Fecha(desde), hasta = Fecha(hasta), page = valida, pageSize, abrir });
        }

        return View("Index", new MisGastosViewModel
        {
            Periodo = periodo,
            Desde = desde,
            Hasta = hasta,
            PeriodoTexto = string.Format(UiTexts.PeriodoAplicado, consulta.Data.Desde.ToString("dd-MM-yyyy"), consulta.Data.Hasta.ToString("dd-MM-yyyy")),
            Total = Montos.De(consulta.Data.TotalValor),
            Cantidad = consulta.Data.Cantidad,
            Items = consulta.Data.Items.Select(fila => new GastoFilaViewModel
            {
                Fecha = fila.FechaGasto.ToString("dd-MM-yyyy"),
                Tipo = fila.Tipo,
                Observacion = string.IsNullOrWhiteSpace(fila.Observacion) ? "-" : fila.Observacion,
                Valor = Montos.De(fila.Valor),
                Estado = fila.Estado == nameof(EstadoGasto.Anulado) ? UiTexts.EstadoAnulado : UiTexts.EstadoActivo,
                Anulado = fila.Estado == nameof(EstadoGasto.Anulado)
            }).ToList(),
            Paginacion = new PaginacionViewModel
            {
                Page = valida,
                PageSize = pageSize,
                Total = consulta.Data.TotalFilas,
                Accion = nameof(Index),
                Controlador = "Gastos"
            },
            Formulario = formulario,
            AbrirModal = abrir || !string.IsNullOrWhiteSpace(formulario.Error)
        });
    }

    private async Task<RegistrarGastoViewModel> FormularioAsync(RegistrarGastoViewModel? model, CancellationToken cancellationToken)
    {
        var tipos = await _api.TiposGastoAsync(cancellationToken);
        var hoy = DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        model ??= new RegistrarGastoViewModel
        {
            FechaGasto = hoy,
            ClaveIdempotencia = Guid.NewGuid().ToString("N")
        };
        model.Tipos = (tipos.Data ?? []).Select(t => new TipoGastoOpcionViewModel
        {
            Id = t.Id,
            Nombre = t.Nombre,
            RequiereObservacion = t.RequiereObservacion
        }).ToList();
        if (model.Tipos.Count == 0 && string.IsNullOrWhiteSpace(model.Error))
        {
            model.Error = tipos.Success ? null : tipos.Message;
        }

        return model;
    }

    private static string? Fecha(DateOnly? fecha) => fecha?.ToString("yyyy-MM-dd");

    private static bool Pesos(string? texto, out decimal valor)
    {
        valor = 0;
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var limpio = texto.Replace(".", string.Empty).Trim();
        return limpio.Length > 0
            && limpio.All(char.IsDigit)
            && decimal.TryParse(limpio, NumberStyles.None, CultureInfo.InvariantCulture, out valor)
            && valor > 0;
    }
}
