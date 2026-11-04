using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Grupos;
using NewRich.Application.Contracts.Utilidades;
using NewRich.Constants;
using NewRich.Domain.Services;

namespace NewRich.Admin.Controllers;

public sealed class UtilidadesController : AdminControllerBase
{
    private static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");
    private readonly IAdminApiClient _api;

    public UtilidadesController(IAdminApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Indicadores(
        string? periodo,
        DateOnly? desde,
        DateOnly? hasta,
        Guid? grupoId,
        int pageV = 1,
        int pageSizeV = 10,
        string? ordenV = null,
        string? dirV = null,
        CancellationToken cancellationToken = default)
    {
        SetNav("utilidades-indicadores", UiTexts.NavUtilidadesIndicadores);
        var grupos = await _api.ListarGruposAsync(cancellationToken);
        var negado = RedirectIfUnauthorized(grupos);
        if (negado is not null)
        {
            return negado;
        }

        IReadOnlyList<GrupoResponse> listaGrupos = grupos.Data ?? [];
        if (grupoId is { } elegido && listaGrupos.All(g => g.GrupoId != elegido))
        {
            grupoId = null;
        }

        var conocido = VentasInforme.OpcionesPeriodo().Any(p => p.Valor == periodo);
        var filtros = new FiltrosPeriodoInforme
        {
            Periodo = conocido ? periodo! : VentasInforme.Periodo7,
            Desde = desde,
            Hasta = hasta,
            Grupos = grupoId is null ? null : [grupoId.Value]
        };
        var invalido = VentasInforme.Validar(filtros);
        if (invalido is not null)
        {
            filtros = new FiltrosPeriodoInforme { Periodo = VentasInforme.Periodo7, Grupos = filtros.Grupos };
        }

        var aviso = grupos.Success ? invalido : grupos.Message;
        if (aviso is not null)
        {
            SetAvisoModal(aviso);
        }

        var ahora = ZonaHorariaColombia.ALocal(DateTime.UtcNow);
        var hoy = DateOnly.FromDateTime(ahora);
        var (inicio, fin) = VentasInforme.Rango(filtros, hoy);
        var carga = await InformeVentasCarga.CargarAsync(_api, DatosInforme.Ventas, filtros, listaGrupos, inicio, fin, cancellationToken);
        if (carga.Unauthorized)
        {
            return RedirectToAction("Ingresar", "Cuenta", new { expired = true });
        }

        if (aviso is null && carga.Error is not null)
        {
            SetAvisoModal(carga.Error);
        }

        return View(new UtilidadesVentasViewModel
        {
            Periodo = filtros.Periodo,
            Desde = filtros.Desde,
            Hasta = filtros.Hasta,
            GrupoId = grupoId,
            Configuracion = VentasInforme.Construir(CategoriasInforme.Ventas, listaGrupos, filtros, true, hoy),
            Informe = VentasInforme.Informe(carga.Fuente, filtros, listaGrupos, inicio, fin, ahora, pageV, pageSizeV, ordenV, dirV)
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
        var formulario = await FormularioAsync(new RegistrarGastoAdminViewModel
        {
            FechaGasto = DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow))
        }, cancellationToken);
        return await VistaMovimientosAsync(periodo, desde, hasta, usuarioId, tipoGastoId, estado, page, pageSize, formulario, false, cancellationToken);
    }

    private async Task<IActionResult> VistaMovimientosAsync(
        string? periodo,
        DateOnly? desde,
        DateOnly? hasta,
        Guid? usuarioId,
        Guid? tipoGastoId,
        string? estado,
        int page,
        int pageSize,
        RegistrarGastoAdminViewModel formulario,
        bool abrir,
        CancellationToken cancellationToken)
    {
        SetNav("utilidades-movimientos", UiTexts.NavUtilidadesMovimientos);
        abrir = abrir || !string.IsNullOrWhiteSpace(formulario.Error);
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
            return View("Movimientos", new MovimientosUtilidadesViewModel
            {
                Periodo = periodo,
                Desde = desde,
                Hasta = hasta,
                UsuarioId = usuarioId,
                TipoGastoId = tipoGastoId,
                Estado = estado ?? string.Empty,
                Usuarios = usuarios.Data ?? [],
                Tipos = tipos.Data ?? [],
                Formulario = formulario,
                AbrirModal = abrir
            });
        }

        var datos = consulta.Data;
        return View("Movimientos", new MovimientosUtilidadesViewModel
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
            },
            Formulario = formulario,
            AbrirModal = abrir
        });
    }

    [HttpGet]
    public IActionResult Registrar() => RedirectToAction(nameof(Resumen), new { abrir = true });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(
        RegistrarGastoAdminViewModel model,
        string? periodo,
        DateOnly? desde,
        DateOnly? hasta,
        string? volver,
        Guid? usuarioId,
        Guid? filtroTipo,
        string? estado,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var aMovimientos = string.Equals(volver, "movimientos", StringComparison.OrdinalIgnoreCase);
        Task<IActionResult> Reabrir() => aMovimientos
            ? VistaMovimientosAsync(periodo, desde, hasta, usuarioId, filtroTipo, estado, page, pageSize, model, true, cancellationToken)
            : VistaResumenAsync(periodo, desde, hasta, model, true, cancellationToken);

        SetNav("utilidades-resumen", UiTexts.NavUtilidadesResumen);
        model = await FormularioAsync(model, cancellationToken);
        if (!Pesos(model.Valor, out var valor))
        {
            model.Error = UiTexts.ValorInvalido;
            return await Reabrir();
        }

        var tipo = model.Tipos.FirstOrDefault(t => t.Id == model.TipoGastoId);
        if (tipo is null)
        {
            model.Error = UiTexts.TipoGasto;
            return await Reabrir();
        }

        if (tipo.RequiereObservacion && string.IsNullOrWhiteSpace(model.Observacion))
        {
            model.Error = UiTexts.ObservacionObligatoria;
            return await Reabrir();
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
            return await Reabrir();
        }

        SetAvisoModal(string.Format(Colombia, "{0} {1}, {2}, {3:dd-MM-yyyy}.", UiTexts.GuardarGasto, guardado.Data.Tipo, FormatoPesos.De(guardado.Data.Valor), guardado.Data.FechaGasto));
        return aMovimientos
            ? RedirectToAction(nameof(Movimientos), new { periodo, desde, hasta, usuarioId, tipoGastoId = filtroTipo, estado, page, pageSize })
            : RedirectToAction(nameof(Resumen), new { periodo, desde, hasta });
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
