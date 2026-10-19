using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Utilidades;
using NewRich.Domain.Services;

namespace NewRich.Admin.Controllers;

public sealed class UtilidadesController : AdminControllerBase
{
    private static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");
    private readonly IAdminApiClient _api;

    public UtilidadesController(IAdminApiClient api) => _api = api;

    [HttpGet]
    [HttpGet]
    public async Task<IActionResult> Indicadores(string? periodo, DateOnly? desde, DateOnly? hasta, CancellationToken cancellationToken)
    {
        SetNav("utilidades-indicadores", UiTexts.NavUtilidadesIndicadores);
        periodo = Normalizar(periodo);
        var consulta = await _api.UtilidadesIndicadoresAsync(periodo, desde, hasta, cancellationToken);
        var negado = RedirectIfUnauthorized(consulta);
        if (negado is not null)
        {
            return negado;
        }

        if (!consulta.Success || consulta.Data is null)
        {
            SetAvisoModal(consulta.Message);
            return View(new IndicadoresUtilidadesViewModel
            {
                Periodo = periodo,
                Desde = desde,
                Hasta = hasta,
                SerieTitulo = UiTexts.GastosPorDia
            });
        }

        var datos = consulta.Data;
        var porMes = datos.Hasta.DayNumber - datos.Desde.DayNumber > 31;
        var serie = porMes
            ? datos.PorDia
                .GroupBy(d => new { d.Fecha.Year, d.Fecha.Month })
                .Select(g => new GastoPorDiaResponse(new DateOnly(g.Key.Year, g.Key.Month, 1), g.Sum(d => d.Valor)))
                .OrderBy(d => d.Fecha)
                .ToList()
            : datos.PorDia.ToList();
        var maximo = serie.Count == 0 ? 0 : serie.Max(d => d.Valor);
        var mayor = datos.MayorValor is null
            ? "-"
            : $"{datos.MayorTipo}, {datos.MayorFecha:dd-MM-yyyy}";

        return View(new IndicadoresUtilidadesViewModel
        {
            Periodo = periodo,
            Desde = desde,
            Hasta = hasta,
            PeriodoTexto = PeriodoTexto(datos.Desde, datos.Hasta),
            Ingresos = FormatoPesos.De(datos.TotalIngresos),
            Ganancia = FormatoPesos.De(datos.Ganancia),
            Total = FormatoPesos.De(datos.TotalValor),
            Cantidad = datos.Cantidad,
            Promedio = FormatoPesos.De(datos.Promedio),
            Mayor = mayor,
            MayorValor = datos.MayorValor is null ? "-" : FormatoPesos.De(datos.MayorValor.Value),
            AnuladosTexto = datos.AnuladosCantidad == 0
                ? null
                : string.Format(UiTexts.AnuladosAparte, datos.AnuladosCantidad, FormatoPesos.De(datos.AnuladosValor)),
            SerieTitulo = porMes ? UiTexts.GastosPorMes : UiTexts.GastosPorDia,
            Tipos = datos.PorTipo.Select(t => new BarraGasto
            {
                Etiqueta = t.Tipo,
                Valor = FormatoPesos.De(t.Valor),
                Cantidad = t.Cantidad,
                Porcentaje = datos.TotalValor == 0 ? 0 : (int)Math.Round(t.Valor / datos.TotalValor * 100m, MidpointRounding.AwayFromZero)
            }).ToList(),
            Serie = serie.Select(d => new BarraGasto
            {
                Etiqueta = porMes ? d.Fecha.ToString("MMM yyyy", Colombia) : d.Fecha.ToString("dd MMM", Colombia),
                Valor = FormatoPesos.De(d.Valor),
                Porcentaje = maximo == 0 ? 0 : (int)Math.Round(d.Valor / maximo * 100m, MidpointRounding.AwayFromZero)
            }).ToList()
        });
    }

    public async Task<IActionResult> Resumen(string? periodo, DateOnly? desde, DateOnly? hasta, bool abrir = false, CancellationToken cancellationToken = default)
    {
        SetNav("utilidades-resumen", UiTexts.NavUtilidadesResumen);
        var formulario = await FormularioAsync(new RegistrarGastoAdminViewModel
        {
            FechaGasto = DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow))
        }, cancellationToken);
        return await VistaResumenAsync(periodo, desde, hasta, formulario, abrir, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Movimientos(string? periodo, DateOnly? desde, DateOnly? hasta, Guid? usuarioId, Guid? tipoGastoId, string? estado, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        SetNav("utilidades-movimientos", UiTexts.NavUtilidadesMovimientos);
        periodo = Normalizar(periodo);
        pageSize = PagingHelper.NormalizePageSize(pageSize);
        var consulta = await _api.UtilidadesMovimientosAsync(periodo, desde, hasta, usuarioId, tipoGastoId, estado, page, pageSize, cancellationToken);
        var negado = RedirectIfUnauthorized(consulta);
        if (negado is not null)
        {
            return negado;
        }

        var usuarios = await _api.ListarUsuariosAsync(cancellationToken);
        var tipos = await _api.UtilidadesCatalogoAsync(cancellationToken);
        if (!consulta.Success || consulta.Data is null)
        {
            SetAvisoModal(consulta.Message);
            return View(new MovimientosUtilidadesViewModel
            {
                Periodo = periodo,
                Desde = desde,
                Hasta = hasta,
                UsuarioId = usuarioId,
                TipoGastoId = tipoGastoId,
                Estado = estado ?? string.Empty,
                Usuarios = usuarios.Data ?? [],
                Tipos = tipos.Data ?? []
            });
        }

        var datos = consulta.Data;
        return View(new MovimientosUtilidadesViewModel
        {
            Periodo = periodo,
            Desde = desde,
            Hasta = hasta,
            PeriodoTexto = PeriodoTexto(datos.Desde, datos.Hasta),
            UsuarioId = usuarioId,
            TipoGastoId = tipoGastoId,
            Estado = estado ?? string.Empty,
            Total = FormatoPesos.De(datos.TotalValor),
            Cantidad = datos.Cantidad,
            Usuarios = usuarios.Data ?? [],
            Tipos = tipos.Data ?? [],
            Pagina = new PagedViewModel<MovimientoGastoResponse>
            {
                Items = datos.Items,
                Page = page,
                PageSize = pageSize,
                TotalCount = datos.TotalFilas
            }
        });
    }

    [HttpGet]
    public IActionResult Registrar() => RedirectToAction(nameof(Resumen), new { abrir = true });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(RegistrarGastoAdminViewModel model, string? periodo, DateOnly? desde, DateOnly? hasta, CancellationToken cancellationToken)
    {
        SetNav("utilidades-resumen", UiTexts.NavUtilidadesResumen);
        model = await FormularioAsync(model, cancellationToken);
        if (!Pesos(model.Valor, out var valor))
        {
            model.Error = UiTexts.ValorInvalido;
            return await VistaResumenAsync(periodo, desde, hasta, model, true, cancellationToken);
        }

        var tipo = model.Tipos.FirstOrDefault(t => t.Id == model.TipoGastoId);
        if (tipo is null)
        {
            model.Error = UiTexts.TipoGasto;
            return await VistaResumenAsync(periodo, desde, hasta, model, true, cancellationToken);
        }

        if (tipo.RequiereObservacion && string.IsNullOrWhiteSpace(model.Observacion))
        {
            model.Error = UiTexts.ObservacionObligatoria;
            return await VistaResumenAsync(periodo, desde, hasta, model, true, cancellationToken);
        }

        var guardado = await _api.UtilidadesRegistrarAsync(
            new RegistrarGastoRequest(model.TipoGastoId, model.FechaGasto, valor, model.Observacion, Guid.NewGuid().ToString("N")),
            cancellationToken);
        var negado = RedirectIfUnauthorized(guardado);
        if (negado is not null)
        {
            return negado;
        }

        if (!guardado.Success || guardado.Data is null)
        {
            model.Error = guardado.Message;
            return await VistaResumenAsync(periodo, desde, hasta, model, true, cancellationToken);
        }

        SetAvisoModal(string.Format(Colombia, "{0} {1}, {2}, {3:dd-MM-yyyy}.", UiTexts.GuardarGasto, guardado.Data.Tipo, FormatoPesos.De(guardado.Data.Valor), guardado.Data.FechaGasto));
        return RedirectToAction(nameof(Resumen), new { periodo, desde, hasta });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Corregir(Guid id, decimal valor, DateOnly fechaGasto, Guid tipoGastoId, string? motivo, string? periodo, DateOnly? desde, DateOnly? hasta, Guid? usuarioId, Guid? filtroTipo, string? estado, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var resultado = await _api.UtilidadesCorregirAsync(id, new CorregirGastoRequest(valor, fechaGasto, tipoGastoId, motivo), cancellationToken);
        var negado = RedirectIfUnauthorized(resultado);
        if (negado is not null)
        {
            return negado;
        }

        SetAvisoModal(resultado.Message);
        return RedirectToAction(nameof(Movimientos), new { periodo, desde, hasta, usuarioId, tipoGastoId = filtroTipo, estado, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Anular(Guid id, string? motivo, string? periodo, DateOnly? desde, DateOnly? hasta, Guid? usuarioId, Guid? tipoGastoId, string? estado, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var resultado = await _api.UtilidadesAnularAsync(id, new AnularGastoRequest(motivo), cancellationToken);
        var negado = RedirectIfUnauthorized(resultado);
        if (negado is not null)
        {
            return negado;
        }

        SetAvisoModal(resultado.Message);
        return RedirectToAction(nameof(Movimientos), new { periodo, desde, hasta, usuarioId, tipoGastoId, estado, page, pageSize });
    }

    [HttpGet]
    public async Task<IActionResult> Configuracion(CancellationToken cancellationToken)
    {
        SetNav("utilidades-configuracion", UiTexts.NavUtilidadesConfiguracion);
        return View(await CatalogoAsync(new ConfiguracionGastosViewModel(), cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearTipo(ConfiguracionGastosViewModel model, CancellationToken cancellationToken)
    {
        SetNav("utilidades-configuracion", UiTexts.NavUtilidadesConfiguracion);
        var creado = await _api.UtilidadesCrearTipoAsync(new GuardarTipoGastoRequest(model.Nombre, model.Descripcion, model.RequiereObservacion), cancellationToken);
        var negado = RedirectIfUnauthorized(creado);
        if (negado is not null)
        {
            return negado;
        }

        if (!creado.Success)
        {
            model.Error = creado.Message;
            return View(await CatalogoAsync(model, cancellationToken));
        }

        SetAvisoModal(creado.Message);
        return RedirectToAction(nameof(Configuracion));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarTipo(Guid id, string? nombre, string? descripcion, bool requiereObservacion, bool activo, CancellationToken cancellationToken)
    {
        var resultado = await _api.UtilidadesActualizarTipoAsync(id, new ActualizarTipoGastoRequest(nombre, descripcion, requiereObservacion, activo), cancellationToken);
        var negado = RedirectIfUnauthorized(resultado);
        if (negado is not null)
        {
            return negado;
        }

        SetAvisoModal(resultado.Message);
        return RedirectToAction(nameof(Configuracion));
    }

    private async Task<IActionResult> VistaResumenAsync(
        string? periodo,
        DateOnly? desde,
        DateOnly? hasta,
        RegistrarGastoAdminViewModel formulario,
        bool abrir,
        CancellationToken cancellationToken)
    {
        periodo = Normalizar(periodo);
        var consulta = await _api.UtilidadesResumenAsync(periodo, desde, hasta, cancellationToken);
        var negado = RedirectIfUnauthorized(consulta);
        if (negado is not null)
        {
            return negado;
        }

        if (!consulta.Success || consulta.Data is null)
        {
            SetAvisoModal(consulta.Message);
            return View("Resumen", new ResumenUtilidadesViewModel
            {
                Periodo = periodo,
                Desde = desde,
                Hasta = hasta,
                Formulario = formulario,
                AbrirModal = abrir || !string.IsNullOrWhiteSpace(formulario.Error)
            });
        }

        var datos = consulta.Data;
        return View("Resumen", new ResumenUtilidadesViewModel
        {
            Periodo = periodo,
            Desde = desde,
            Hasta = hasta,
            PeriodoTexto = PeriodoTexto(datos.Desde, datos.Hasta),
            Gastos = FormatoPesos.De(datos.Gastos),
            Cantidad = datos.Cantidad,
            Ingresos = UiTexts.UtilidadesPendiente,
            Ganancia = UiTexts.UtilidadesPendiente,
            Tipos = datos.PorTipo.Select(t => new BarraGasto
            {
                Etiqueta = t.Tipo,
                Valor = FormatoPesos.De(t.Valor),
                Cantidad = t.Cantidad,
                Porcentaje = datos.Gastos == 0 ? 0 : (int)Math.Round(t.Valor / datos.Gastos * 100m, MidpointRounding.AwayFromZero)
            }).ToList(),
            Formulario = formulario,
            AbrirModal = abrir || !string.IsNullOrWhiteSpace(formulario.Error)
        });
    }

    private async Task<RegistrarGastoAdminViewModel> FormularioAsync(RegistrarGastoAdminViewModel model, CancellationToken cancellationToken)
    {
        var tipos = await _api.UtilidadesTiposActivosAsync(cancellationToken);
        model.Tipos = tipos.Data ?? [];
        return model;
    }

    private async Task<ConfiguracionGastosViewModel> CatalogoAsync(ConfiguracionGastosViewModel model, CancellationToken cancellationToken)
    {
        var catalogo = await _api.UtilidadesCatalogoAsync(cancellationToken);
        return new ConfiguracionGastosViewModel
        {
            Nombre = model.Nombre,
            Descripcion = model.Descripcion,
            RequiereObservacion = model.RequiereObservacion,
            Error = model.Error ?? (catalogo.Success ? null : catalogo.Message),
            Tipos = catalogo.Data ?? []
        };
    }

    private static string Normalizar(string? periodo) => string.IsNullOrWhiteSpace(periodo) ? "mes" : periodo;

    private static string PeriodoTexto(DateOnly desde, DateOnly hasta) =>
        desde.ToString("dd-MM-yyyy", Colombia) + " a " + hasta.ToString("dd-MM-yyyy", Colombia);

    private static bool Pesos(string? texto, out decimal valor)
    {
        valor = 0;
        var limpio = (texto ?? string.Empty).Replace(".", string.Empty).Replace(",", string.Empty).Trim();
        return limpio.Length > 0 && limpio.All(char.IsDigit) && decimal.TryParse(limpio, out valor) && valor > 0;
    }
}
