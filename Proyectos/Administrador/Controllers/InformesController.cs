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

        var configuraciones = await _api.ListarConfiguracionesInformeAsync(cancellationToken);
        if (RedirectIfUnauthorized(configuraciones) is { } negado) return negado;
        var usuarios = await _api.ListarUsuariosAsync(cancellationToken);
        if (RedirectIfUnauthorized(usuarios) is { } negadoUsuarios) return negadoUsuarios;
        var grupos = await _api.ListarGruposAsync(cancellationToken);
        if (RedirectIfUnauthorized(grupos) is { } negadoGrupos) return negadoGrupos;

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

        var guardadaVentas = configuraciones.Data?.FirstOrDefault(c => c.Categoria == CategoriasInforme.Ventas);
        var filtrosVentas = VentasInforme.LeerFiltros(guardadaVentas?.Filtros);
        var ventas = VentasInforme.Construir(listaGrupos, filtrosVentas, guardadaVentas is not null, hoy);
        InformeVentasViewModel? informeVentas = null;
        if (ventas.Configurada)
        {
            var (desde, hasta) = VentasInforme.Rango(filtrosVentas, hoy);
            var carga = await InformeVentasCarga.CargarAsync(_api, filtrosVentas, listaGrupos, desde, hasta, cancellationToken);
            if (carga.Unauthorized) return RedirectToAction("Ingresar", "Cuenta", new { expired = true });
            if (fallo.Message is null && carga.Error is not null) SetAvisoModal(carga.Error);
            informeVentas = VentasInforme.Informe(carga.Fuente, filtrosVentas, listaGrupos, desde, hasta, ahora, pageV, pageSizeV, pageG, pageSizeG);
        }

        return View(new InformesViewModel
        {
            Vendedores = vendedores,
            Informe = vendedores.Configurada
                ? PersonalInforme.Informe(personal, listaGrupos, filtros, ahora, orden, dir, page, pageSize)
                : null,
            Ventas = ventas,
            InformeVentas = informeVentas,
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
                new() { Clave = "utilidades-kpi", Nombre = UiTexts.InformeCategoriaUtilidadesKpi, Descripcion = UiTexts.InformeCategoriaUtilidadesKpiAyuda },
                new()
                {
                    Clave = CategoriasInforme.Ventas,
                    Nombre = UiTexts.InformeCategoriaVentas,
                    Descripcion = UiTexts.InformeCategoriaVentasAyuda,
                    Habilitada = true,
                    Configurada = ventas.Configurada,
                    Resumen = ventas.Configurada ? ventas.ResumenCorto : null
                },
                new() { Clave = "mas-categorias", Nombre = UiTexts.InformeCategoriaMas, Descripcion = UiTexts.InformeCategoriaMasAyuda }
            ]
        });
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
    public async Task<IActionResult> GuardarVentas(
        string? periodo,
        DateOnly? desde,
        DateOnly? hasta,
        string? modoGrupos,
        List<Guid>? grupos,
        string? modoSecciones,
        List<string>? secciones,
        CancellationToken cancellationToken)
    {
        var rango = periodo == VentasInforme.PeriodoRango;
        var filtros = new FiltrosVentasInforme
        {
            Periodo = periodo?.Trim() ?? string.Empty,
            Desde = rango ? desde : null,
            Hasta = rango ? hasta : null,
            Grupos = Elegidos(modoGrupos, grupos?.Where(g => g != Guid.Empty)),
            Secciones = Elegidos(modoSecciones, secciones?.Where(s => VentasInforme.OpcionesSeccion().Any(o => o.Valor == s)))
        };

        var aviso = VentasInforme.Validar(filtros, DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow)));
        if (aviso is not null)
        {
            SetAvisoModal(aviso);
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.GuardarConfiguracionInformeAsync(
            CategoriasInforme.Ventas,
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
