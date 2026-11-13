using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Cierre;
using NewRich.Application.Contracts.Grupos;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Constants;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;

namespace NewRich.Admin.Controllers;

public sealed class InformesController : AdminControllerBase
{
    private const string ModoElegidos = "elegidos";

    private readonly IAdminApiClient _api;

    public InformesController(IAdminApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(
        string? orden,
        string? dir,
        int page = 1,
        int pageSize = 10,
        int pageV = 1,
        int pageSizeV = 10,
        int pageG = 1,
        int pageSizeG = 10,
        string? ordenP = null,
        string? dirP = null,
        int pageP = 1,
        int pageSizeP = 10,
        string? ordenG = null,
        string? dirG = null,
        string? ordenV = null,
        string? dirV = null,
        string? ordenAt = null,
        string? dirAt = null,
        int pageAt = 1,
        int pageSizeAt = 10,
        int pageRd = 1,
        int pageSizeRd = 10,
        string? ordenC = null,
        string? dirC = null,
        int pageC = 1,
        int pageSizeC = 10,
        string? ordenCp = null,
        string? dirCp = null,
        int pageCp = 1,
        int pageSizeCp = 10,
        string? pestana = null,
        Guid? cierre = null,
        string? grupo = null,
        int pageH = 1,
        int pageSizeH = 10,
        int pageD = 1,
        int pageSizeD = 10,
        string? ordenH = null,
        string? dirH = null,
        string? ordenD = null,
        string? dirD = null,
        bool nuevo = false,
        CancellationToken cancellationToken = default)
    {
        SetNav("informes", UiTexts.NavInformes);
        var premios = new OrdenesInforme(ordenP, dirP, pageP, pageSizeP, ordenG, dirG, ordenV, dirV, ordenAt, dirAt, pageAt, pageSizeAt, ordenC, dirC, pageC, pageSizeC, ordenCp, dirCp, pageCp, pageSizeCp);
        var (modelo, salida) = await ConstruirAsync(orden, dir, page, pageSize, pageV, pageSizeV, pageG, pageSizeG, premios, pestana, cierre, grupo, pageH, pageSizeH, pageD, pageSizeD, ordenH, dirH, ordenD, dirD, nuevo, pageRd, pageSizeRd, cancellationToken);
        return salida ?? View(modelo);
    }

    private async Task<(InformesViewModel? Modelo, IActionResult? Salida)> ConstruirAsync(
        string? orden,
        string? dir,
        int page,
        int pageSize,
        int pageV,
        int pageSizeV,
        int pageG,
        int pageSizeG,
        OrdenesInforme premios,
        string? pestana,
        Guid? cierre,
        string? grupo,
        int pageH,
        int pageSizeH,
        int pageD,
        int pageSizeD,
        string? ordenH,
        string? dirH,
        string? ordenD,
        string? dirD,
        bool nuevo,
        int pageRd,
        int pageSizeRd,
        CancellationToken cancellationToken)
    {
        var configuraciones = await _api.ListarConfiguracionesInformeAsync(cancellationToken);
        if (RedirectIfUnauthorized(configuraciones) is { } negado) return (null, negado);
        var usuarios = await _api.ListarUsuariosAsync(cancellationToken);
        if (RedirectIfUnauthorized(usuarios) is { } negadoUsuarios) return (null, negadoUsuarios);
        var grupos = await _api.ListarGruposAsync(cancellationToken);
        if (RedirectIfUnauthorized(grupos) is { } negadoGrupos) return (null, negadoGrupos);

        var fallo = new[] { (configuraciones.Success, configuraciones.Message), (usuarios.Success, usuarios.Message), (grupos.Success, grupos.Message) }
            .FirstOrDefault(r => !r.Success);
        if (fallo.Message is not null) SetAvisoModal(fallo.Message);

        var guardada = configuraciones.Data?.FirstOrDefault(c => c.Categoria == CategoriasInforme.Vendedores);
        IReadOnlyList<UsuarioResponse> personal = usuarios.Data ?? [];
        IReadOnlyList<GrupoResponse> listaGrupos = grupos.Data ?? [];
        var filtros = PersonalInforme.LeerFiltros(guardada?.Filtros);
        var vendedores = PersonalInforme.Construir(personal, listaGrupos, filtros, guardada is not null);
        var ahora = ZonaHorariaColombia.ALocal(DateTime.UtcNow);
        var hoy = DateOnly.FromDateTime(ahora);

        var avisado = fallo.Message is not null;
        PeriodoInformeViewModel Configuracion(string categoria)
        {
            var guardadaPeriodo = configuraciones.Data?.FirstOrDefault(c => c.Categoria == categoria);
            return VentasInforme.Construir(categoria, listaGrupos, VentasInforme.LeerFiltros(guardadaPeriodo?.Filtros), guardadaPeriodo is not null, hoy, personal);
        }

        async Task<(CargaInformeVentas? Carga, DateOnly Desde, DateOnly Hasta)> Cargar(PeriodoInformeViewModel config, DatosInforme datos)
        {
            var (desde, hasta) = VentasInforme.Rango(config.Filtros, hoy);
            if (!config.Configurada) return (null, desde, hasta);
            var carga = await InformeVentasCarga.CargarAsync(_api, datos, config.Filtros, listaGrupos, desde, hasta, cancellationToken);
            if (!avisado && carga.Error is not null)
            {
                SetAvisoModal(carga.Error);
                avisado = true;
            }
            return (carga, desde, hasta);
        }

        var general = Configuracion(CategoriasInforme.General);
        var ventas = Configuracion(CategoriasInforme.Ventas);
        var egresos = Configuracion(CategoriasInforme.Egresos);
        var recaudo = Configuracion(CategoriasInforme.Recaudo);

        var cargaGeneral = await Cargar(general, DatosInforme.Recaudo);
        var cargaVentas = await Cargar(ventas, DatosInforme.Ventas);
        var cargaEgresos = await Cargar(egresos, DatosInforme.Egresos);
        var cargaRecaudo = await Cargar(recaudo, DatosInforme.Recaudo);
        if (new[] { cargaGeneral.Carga, cargaVentas.Carga, cargaEgresos.Carga, cargaRecaudo.Carga }.Any(c => c?.Unauthorized == true))
            return (null, RedirectToAction("Ingresar", "Cuenta", new { expired = true }));

        var enCierres = string.Equals(pestana, "cierres", StringComparison.OrdinalIgnoreCase);
        var cierresIncluidos = configuraciones.Data?.Any(c => c.Categoria == CategoriasInforme.Cierres) == true;
        var listaCierres = await _api.ListarCierresAsync(cancellationToken);
        if (listaCierres.Unauthorized) return (null, RedirectToAction("Ingresar", "Cuenta", new { expired = true }));
        if (!avisado && !listaCierres.Success)
        {
            SetAvisoModal(listaCierres.Message);
            avisado = true;
        }

        CierrePreviaResponse? previa = null;
        CierreDetalleResponse? detalle = null;
        if (enCierres)
        {
            var previaResultado = await _api.PreviaCierreAsync(cancellationToken);
            if (previaResultado.Unauthorized) return (null, RedirectToAction("Ingresar", "Cuenta", new { expired = true }));
            if (!avisado && !previaResultado.Success)
            {
                SetAvisoModal(previaResultado.Message);
                avisado = true;
            }

            previa = previaResultado.Data;
            if (cierre is not null)
            {
                var detalleResultado = await _api.DetalleCierreAsync(cierre.Value, grupo, pageD, pageSizeD, ordenD, dirD, cancellationToken);
                if (detalleResultado.Unauthorized) return (null, RedirectToAction("Ingresar", "Cuenta", new { expired = true }));
                if (!avisado && !detalleResultado.Success)
                {
                    SetAvisoModal(detalleResultado.Message);
                    avisado = true;
                }

                detalle = detalleResultado.Data;
            }
        }

        var historial = listaCierres.Data ?? [];

        return (new InformesViewModel
        {
            Corte = ahora,
            OrdenSecciones = OrdenSeccionesInforme.Leer(configuraciones.Data?.FirstOrDefault(c => c.Categoria == CategoriasInforme.OrdenSecciones)?.Filtros),
            Pestana = enCierres ? "cierres" : "informes",
            CierresIncluidos = cierresIncluidos,
            MostrarNuevo = enCierres && nuevo && previa is not null,
            Previa = previa,
            Cierres = historial,
            Historial = PagingHelper.Paginate(CierreTablas.OrdenarHistorial(historial, ordenH, dirH), pageH, pageSizeH),
            OrdenHistorial = CierreTablas.Historial(ordenH, dirH),
            OrdenDetalle = CierreTablas.Detalle(ordenD, dirD),
            Detalle = detalle,
            Vendedores = vendedores,
            Informe = vendedores.Configurada
                ? PersonalInforme.Informe(personal, listaGrupos, filtros, ahora, orden, dir, page, pageSize)
                : null,
            General = general,
            InformeGeneral = cargaGeneral.Carga is { } fuenteGeneral
                ? VentasInforme.InformeGeneral(fuenteGeneral.Fuente, general.Filtros, cargaGeneral.Desde, cargaGeneral.Hasta, ahora)
                : null,
            Ventas = ventas,
            InformeVentas = cargaVentas.Carga is { } fuenteVentas
                ? VentasInforme.Informe(fuenteVentas.Fuente, ventas.Filtros, listaGrupos, cargaVentas.Desde, cargaVentas.Hasta, ahora, pageV, pageSizeV,
                    premios.OrdenRanking, premios.DireccionRanking)
                : null,
            Egresos = egresos,
            InformeEgresos = cargaEgresos.Carga is { } fuenteEgresos
                ? VentasInforme.InformeEgresos(fuenteEgresos.Fuente, egresos.Filtros, listaGrupos, cargaEgresos.Desde, cargaEgresos.Hasta, ahora, pageG, pageSizeG,
                    premios.Orden, premios.Direccion, premios.Pagina, premios.Filas, premios.OrdenGastos, premios.DireccionGastos,
                    premios.OrdenCubiertos, premios.DireccionCubiertos, premios.PaginaCubiertos, PagingHelper.NormalizePageSize(premios.FilasCubiertos),
                    premios.OrdenCubiertosPda, premios.DireccionCubiertosPda, premios.PaginaCubiertosPda, PagingHelper.NormalizePageSize(premios.FilasCubiertosPda))
                : null,
            Recaudo = recaudo,
            InformeRecaudo = cargaRecaudo.Carga is { } fuenteRecaudo
                ? VentasInforme.InformeRecaudo(fuenteRecaudo.Fuente, recaudo.Filtros, listaGrupos, cargaRecaudo.Desde, cargaRecaudo.Hasta, ahora,
                    premios.OrdenAtrasos, premios.DireccionAtrasos, premios.PaginaAtrasos, PagingHelper.NormalizePageSize(premios.FilasAtrasos),
                    pageRd, PagingHelper.NormalizePageSize(pageSizeRd))
                : null,
            Tarjetas =
            [
                new()
                {
                    Clave = CategoriasInforme.Vendedores,
                    Nombre = UiTexts.InformeCategoriaVendedores,
                    Descripcion = UiTexts.InformeCategoriaVendedoresAyuda,
                    Habilitada = true,
                    Configurada = vendedores.Configurada,
                    Resumen = vendedores.Configurada ? vendedores.ResumenCorto : null
                },
                new()
                {
                    Clave = CategoriasInforme.General,
                    Nombre = UiTexts.InformeCategoriaGeneral,
                    Descripcion = UiTexts.InformeCategoriaGeneralAyuda,
                    Habilitada = true,
                    Configurada = general.Configurada,
                    Resumen = general.Configurada ? general.ResumenCorto : null
                },
                new()
                {
                    Clave = CategoriasInforme.Ventas,
                    Nombre = UiTexts.InformeCategoriaVentas,
                    Descripcion = UiTexts.InformeCategoriaVentasAyuda,
                    Habilitada = true,
                    Configurada = ventas.Configurada,
                    Resumen = ventas.Configurada ? ventas.ResumenCorto : null
                },
                new()
                {
                    Clave = CategoriasInforme.Egresos,
                    Nombre = UiTexts.InformeCategoriaEgresos,
                    Descripcion = UiTexts.InformeCategoriaEgresosAyuda,
                    Habilitada = true,
                    Configurada = egresos.Configurada,
                    Resumen = egresos.Configurada ? egresos.ResumenCorto : null
                },
                new()
                {
                    Clave = CategoriasInforme.Recaudo,
                    Nombre = UiTexts.InformeCategoriaRecaudo,
                    Descripcion = UiTexts.InformeCategoriaRecaudoAyuda,
                    Habilitada = true,
                    Configurada = recaudo.Configurada,
                    Resumen = recaudo.Configurada ? recaudo.ResumenCorto : null
                },
                new()
                {
                    Clave = CategoriasInforme.Cierres,
                    Nombre = UiTexts.InformeCategoriaCierres,
                    Descripcion = UiTexts.InformeCategoriaCierresAyuda,
                    Habilitada = true,
                    Configurada = cierresIncluidos,
                    Resumen = cierresIncluidos ? $"{(listaCierres.Data ?? []).Count} períodos" : null
                }
            ]
        }, null);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarVendedores(
        string? modoGrupos,
        List<string>? grupos,
        string? modoUsuarios,
        List<Guid>? usuarios,
        string? modoTipos,
        List<RolUsuario>? tipos,
        CancellationToken cancellationToken)
    {
        var filtros = new FiltrosVendedoresInforme
        {
            Grupos = Elegidos(modoGrupos, grupos?.Where(g => !string.IsNullOrWhiteSpace(g)).Select(g => g.Trim())),
            Usuarios = Elegidos(modoUsuarios, usuarios?.Where(u => u != Guid.Empty)),
            Tipos = Elegidos(modoTipos, tipos?.Where(Enum.IsDefined))
        };

        var aviso = filtros switch
        {
            { Grupos.Count: 0 } => UiTexts.InformeEligeGrupo,
            { Usuarios.Count: 0 } => UiTexts.InformeEligeUsuario,
            { Tipos.Count: 0 } => UiTexts.InformeEligeTipo,
            _ => null
        };
        if (aviso is not null)
        {
            SetAvisoModal(aviso);
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.GuardarConfiguracionInformeAsync(
            CategoriasInforme.Vendedores,
            JsonSerializer.Serialize(filtros, PersonalInforme.Json),
            cancellationToken);
        if (RedirectIfUnauthorized(result) is { } negado) return negado;

        SetAvisoModal(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarPeriodo(
        string? categoria,
        string? periodo,
        DateOnly? desde,
        DateOnly? hasta,
        string? modoGrupos,
        List<Guid>? grupos,
        string? modoVendedores,
        List<Guid>? vendedores,
        List<string>? tiposEgreso,
        CancellationToken cancellationToken)
    {
        var clave = VentasInforme.CategoriasPorPeriodo.FirstOrDefault(c => c == categoria);
        var rango = periodo == VentasInforme.PeriodoRango;
        List<string>? tipos = null;
        if (clave == CategoriasInforme.Egresos)
        {
            tipos = VentasInforme.TiposEgreso.Select(t => t.Valor).Where(v => tiposEgreso?.Contains(v) == true).ToList();
            if (tipos.Count == VentasInforme.TiposEgreso.Count) tipos = null;
        }
        var filtros = new FiltrosPeriodoInforme
        {
            Periodo = periodo?.Trim() ?? string.Empty,
            Desde = rango ? desde : null,
            Hasta = rango ? hasta : null,
            Grupos = clave == CategoriasInforme.General ? null : Elegidos(modoGrupos, grupos?.Where(g => g != Guid.Empty)),
            Vendedores = clave == CategoriasInforme.General ? null : Elegidos(modoVendedores, vendedores?.Where(v => v != Guid.Empty)),
            TiposEgreso = clave == CategoriasInforme.General ? null : tipos
        };

        var aviso = clave is null ? UiTexts.InformeCategoriaInvalida : VentasInforme.Validar(filtros);
        if (aviso is not null)
        {
            SetAvisoModal(aviso);
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.GuardarConfiguracionInformeAsync(
            clave!,
            JsonSerializer.Serialize(filtros, PersonalInforme.Json),
            cancellationToken);
        if (RedirectIfUnauthorized(result) is { } negado) return negado;

        SetAvisoModal(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarCierres(CancellationToken cancellationToken)
    {
        var result = await _api.GuardarConfiguracionInformeAsync(CategoriasInforme.Cierres, "{}", cancellationToken);
        if (RedirectIfUnauthorized(result) is { } negado) return negado;

        SetAvisoModal(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(string? password, CancellationToken cancellationToken)
    {
        var resultado = await _api.ConfirmarCierreAsync(new ConfirmarCierreRequest { Password = password ?? string.Empty }, cancellationToken);
        if (RedirectIfUnauthorized(resultado) is { } negadoCierre) return negadoCierre;

        SetAvisoModal(resultado.Message);
        return RedirectToAction(nameof(Index), new { pestana = "cierres" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OrdenSecciones(string? categoria, List<string>? secciones, CancellationToken cancellationToken)
    {
        var configuraciones = await _api.ListarConfiguracionesInformeAsync(cancellationToken);
        if (configuraciones.Unauthorized) return Json(new { exitoso = false, mensaje = UiTexts.PdaSesionExpirada });

        var actual = configuraciones.Data?.FirstOrDefault(c => c.Categoria == CategoriasInforme.OrdenSecciones)?.Filtros;
        var json = configuraciones.Success ? OrdenSeccionesInforme.Aplicar(actual, categoria ?? string.Empty, secciones) : null;
        if (json is null) return Json(new { exitoso = false, mensaje = UiTexts.InformeOrdenNoGuardado });

        var result = await _api.GuardarConfiguracionInformeAsync(CategoriasInforme.OrdenSecciones, json, cancellationToken);
        return Json(new { exitoso = result.Success, mensaje = result.Success ? null : UiTexts.InformeOrdenNoGuardado });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Quitar(string categoria, CancellationToken cancellationToken)
    {
        var result = await _api.QuitarConfiguracionInformeAsync(categoria, cancellationToken);
        if (RedirectIfUnauthorized(result) is { } negado) return negado;

        SetAvisoModal(result.Message);
        return RedirectToAction(nameof(Index));
    }

    private static List<T>? Elegidos<T>(string? modo, IEnumerable<T>? valores) =>
        modo == ModoElegidos ? (valores ?? []).Distinct().ToList() : null;

    private sealed record OrdenesInforme(
        string? Orden,
        string? Direccion,
        int Pagina,
        int Filas,
        string? OrdenGastos,
        string? DireccionGastos,
        string? OrdenRanking,
        string? DireccionRanking,
        string? OrdenAtrasos,
        string? DireccionAtrasos,
        int PaginaAtrasos,
        int FilasAtrasos,
        string? OrdenCubiertos,
        string? DireccionCubiertos,
        int PaginaCubiertos,
        int FilasCubiertos,
        string? OrdenCubiertosPda,
        string? DireccionCubiertosPda,
        int PaginaCubiertosPda,
        int FilasCubiertosPda);
}
