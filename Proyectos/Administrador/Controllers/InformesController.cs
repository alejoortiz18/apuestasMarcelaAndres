using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
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
        CancellationToken cancellationToken = default)
    {
        SetNav("informes", UiTexts.NavInformes);
        var (modelo, salida) = await ConstruirAsync(orden, dir, page, pageSize, pageV, pageSizeV, pageG, pageSizeG, cancellationToken);
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
            return VentasInforme.Construir(categoria, listaGrupos, VentasInforme.LeerFiltros(guardadaPeriodo?.Filtros), guardadaPeriodo is not null, hoy);
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

        var ventas = Configuracion(CategoriasInforme.Ventas);
        var egresos = Configuracion(CategoriasInforme.Egresos);
        var recaudo = Configuracion(CategoriasInforme.Recaudo);

        var cargaVentas = await Cargar(ventas, DatosInforme.Ventas);
        var cargaEgresos = await Cargar(egresos, DatosInforme.Egresos);
        var cargaRecaudo = await Cargar(recaudo, DatosInforme.Recaudo);
        if (new[] { cargaVentas.Carga, cargaEgresos.Carga, cargaRecaudo.Carga }.Any(c => c?.Unauthorized == true))
            return (null, RedirectToAction("Ingresar", "Cuenta", new { expired = true }));

        return (new InformesViewModel
        {
            Corte = ahora,
            Vendedores = vendedores,
            Informe = vendedores.Configurada
                ? PersonalInforme.Informe(personal, listaGrupos, filtros, ahora, orden, dir, page, pageSize)
                : null,
            Ventas = ventas,
            InformeVentas = cargaVentas.Carga is { } fuenteVentas
                ? VentasInforme.Informe(fuenteVentas.Fuente, ventas.Filtros, listaGrupos, cargaVentas.Desde, cargaVentas.Hasta, ahora, pageV, pageSizeV)
                : null,
            Egresos = egresos,
            InformeEgresos = cargaEgresos.Carga is { } fuenteEgresos
                ? VentasInforme.InformeEgresos(fuenteEgresos.Fuente, egresos.Filtros, listaGrupos, cargaEgresos.Desde, cargaEgresos.Hasta, ahora, pageG, pageSizeG)
                : null,
            Recaudo = recaudo,
            InformeRecaudo = cargaRecaudo.Carga is { } fuenteRecaudo
                ? VentasInforme.InformeRecaudo(fuenteRecaudo.Fuente, recaudo.Filtros, listaGrupos, cargaRecaudo.Desde, cargaRecaudo.Hasta, ahora)
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
        CancellationToken cancellationToken)
    {
        var clave = VentasInforme.CategoriasPorPeriodo.FirstOrDefault(c => c == categoria);
        var rango = periodo == VentasInforme.PeriodoRango;
        var filtros = new FiltrosPeriodoInforme
        {
            Periodo = periodo?.Trim() ?? string.Empty,
            Desde = rango ? desde : null,
            Hasta = rango ? hasta : null,
            Grupos = Elegidos(modoGrupos, grupos?.Where(g => g != Guid.Empty))
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
    public async Task<IActionResult> Quitar(string categoria, CancellationToken cancellationToken)
    {
        var result = await _api.QuitarConfiguracionInformeAsync(categoria, cancellationToken);
        if (RedirectIfUnauthorized(result) is { } negado) return negado;

        SetAvisoModal(result.Message);
        return RedirectToAction(nameof(Index));
    }

    private static List<T>? Elegidos<T>(string? modo, IEnumerable<T>? valores) =>
        modo == ModoElegidos ? (valores ?? []).Distinct().ToList() : null;
}
