using System.Globalization;
using System.Text.Json;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Application.Contracts.Grupos;
using NewRich.Application.Contracts.Kpi;
using NewRich.Application.Contracts.PremiosEntregadosWeb;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Application.Contracts.RevisadaManual;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Application.Contracts.Utilidades;
using NewRich.Constants;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Services;

public sealed class FuenteInformeVentas
{
    public KpiResponse? Kpi { get; init; }
    public decimal IngresosWeb { get; init; }
    public IReadOnlyList<PremioEntregadoWebResponse> PremiosWeb { get; init; } = [];
    public InformeGastosPropiosResponse? Gastos { get; init; }
    public IReadOnlyList<MovimientoGastoResponse> Movimientos { get; init; } = [];
    public VistaRevisadaManualResponse? Cubierto { get; init; }
    public TableroRecaudoResponse? Recaudo { get; init; }
}

public static class VentasInforme
{
    public const string PeriodoHoy = "hoy";
    public const string Periodo7 = "7";
    public const string Periodo15 = "15";
    public const string Periodo30 = "30";
    public const string PeriodoRango = "rango";
    public const int DiasMaximos = 31;
    public const string TipoEgresoPremios = "premios";
    public const string TipoEgresoGastos = "gastos";
    public const string TipoEgresoCubierto = "cubierto";

    public static readonly IReadOnlyList<OpcionInforme> TiposEgreso =
    [
        new() { Valor = TipoEgresoPremios, Texto = UiTexts.InformeTipoEgresoPremios },
        new() { Valor = TipoEgresoGastos, Texto = UiTexts.InformeTipoEgresoGastos },
        new() { Valor = TipoEgresoCubierto, Texto = UiTexts.InformeTipoEgresoCubierto }
    ];

    public static bool Incluye(FiltrosPeriodoInforme filtros, string tipo) =>
        filtros.TiposEgreso is null || filtros.TiposEgreso.Contains(tipo);

    public static readonly IReadOnlyList<string> CategoriasPorPeriodo =
        [CategoriasInforme.General, CategoriasInforme.Ventas, CategoriasInforme.Egresos, CategoriasInforme.Recaudo];

    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");

    public static FiltrosPeriodoInforme LeerFiltros(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Predeterminados();
        try
        {
            var filtros = JsonSerializer.Deserialize<FiltrosPeriodoInforme>(json, PersonalInforme.Json);
            return filtros is null || string.IsNullOrWhiteSpace(filtros.Periodo) ? Predeterminados() : filtros;
        }
        catch (JsonException)
        {
            return Predeterminados();
        }
    }

    public static FiltrosPeriodoInforme Predeterminados() => new() { Periodo = Periodo7 };

    public static string? Validar(FiltrosPeriodoInforme filtros)
    {
        if (filtros.Periodo is not (PeriodoHoy or Periodo7 or Periodo15 or Periodo30 or PeriodoRango))
            return UiTexts.InformePeriodoInvalido;
        if (filtros.Periodo == PeriodoRango)
        {
            if (filtros.Desde is null || filtros.Hasta is null) return UiTexts.InformeRangoIncompleto;
            if (filtros.Desde > filtros.Hasta) return UiTexts.InformeRangoInvalido;
            if (filtros.Hasta.Value.DayNumber - filtros.Desde.Value.DayNumber + 1 > DiasMaximos) return UiTexts.InformeRangoMaximo;
        }
        if (filtros.Grupos is { Count: 0 }) return UiTexts.InformeEligeGrupo;
        if (filtros.Vendedores is { Count: 0 }) return UiTexts.InformeEligeVendedor;
        if (filtros.TiposEgreso is { Count: 0 }) return UiTexts.InformeEligeTipoEgreso;
        return null;
    }

    public static (DateOnly Desde, DateOnly Hasta) Rango(FiltrosPeriodoInforme filtros, DateOnly hoy) => filtros.Periodo switch
    {
        PeriodoHoy => (hoy, hoy),
        Periodo15 => (hoy.AddDays(-14), hoy),
        Periodo30 => (hoy.AddDays(-29), hoy),
        PeriodoRango when filtros.Desde is { } desde && filtros.Hasta is { } hasta && desde <= hasta
            && hasta.DayNumber - desde.DayNumber + 1 <= DiasMaximos => (desde, hasta),
        _ => (hoy.AddDays(-6), hoy)
    };

    public static IReadOnlyList<OpcionInforme> OpcionesPeriodo() =>
    [
        new() { Valor = PeriodoHoy, Texto = UiTexts.InformeHoy },
        new() { Valor = Periodo7, Texto = UiTexts.Ultimos7Dias },
        new() { Valor = Periodo15, Texto = UiTexts.InformeUltimos15Dias },
        new() { Valor = Periodo30, Texto = UiTexts.Ultimos30Dias },
        new() { Valor = PeriodoRango, Texto = UiTexts.InformeRangoFechas }
    ];

    public static IReadOnlyList<OpcionInforme> OpcionesGrupo(IEnumerable<GrupoResponse> grupos) => grupos
        .OrderBy(g => g.Nombre, StringComparer.CurrentCultureIgnoreCase)
        .Select(g => new OpcionInforme { Valor = g.GrupoId.ToString(), Texto = g.Nombre })
        .ToList();

    /// <param name="usuarios">Con usuarios, el filtro ofrece elegir vendedores; sin ellos (Utilidades) no aparece.</param>
    public static PeriodoInformeViewModel Construir(string clave, IReadOnlyList<GrupoResponse> grupos, FiltrosPeriodoInforme filtros, bool configurada, DateOnly hoy, IReadOnlyList<UsuarioResponse>? usuarios = null)
    {
        var periodos = OpcionesPeriodo();
        var filtrosUso = clave == CategoriasInforme.General
            ? new FiltrosPeriodoInforme
            {
                Periodo = filtros.Periodo,
                Desde = filtros.Desde,
                Hasta = filtros.Hasta
            }
            : filtros;
        var (desde, hasta) = Rango(filtrosUso, hoy);
        var periodo = $"{periodos.First(p => p.Valor == (filtrosUso.Periodo is PeriodoHoy or Periodo15 or Periodo30 or PeriodoRango ? filtrosUso.Periodo : Periodo7)).Texto} ({Fechas(desde, hasta)})";
        if (clave == CategoriasInforme.General)
        {
            return new PeriodoInformeViewModel
            {
                Clave = clave,
                Configurada = configurada,
                Filtros = filtrosUso,
                Periodos = periodos,
                Hoy = hoy,
                Detalle = [new() { Etiqueta = UiTexts.InformeEtiquetaPeriodo, Valor = periodo }],
                ResumenCorto = $"{periodo} · {UiTexts.InformeTodosGrupos}"
            };
        }

        var alcance = Alcance(filtrosUso, grupos);
        var vendedores = usuarios is null ? [] : OpcionesVendedor(usuarios);
        var detalle = new List<DetalleFiltroInforme>
        {
            new() { Etiqueta = UiTexts.InformeEtiquetaPeriodo, Valor = periodo },
            new() { Etiqueta = UiTexts.InformeEtiquetaGrupos, Valor = alcance }
        };
        var resumen = $"{periodo} · {alcance}";
        if (usuarios is not null)
        {
            var elegidos = Nombres(
                filtrosUso.Vendedores?.Select(id => vendedores.FirstOrDefault(v => v.Valor == id.ToString())?.Texto ?? id.ToString()).ToList(),
                UiTexts.InformeTodosVendedores);
            detalle.Add(new() { Etiqueta = UiTexts.InformeEtiquetaVendedores, Valor = elegidos });
            resumen += $" · {elegidos}";
        }

        if (clave == CategoriasInforme.Egresos)
        {
            var tipos = string.Join(", ", TiposEgreso.Where(t => Incluye(filtrosUso, t.Valor)).Select(t => t.Texto));
            detalle.Add(new() { Etiqueta = UiTexts.InformeEtiquetaTiposEgreso, Valor = tipos });
            resumen += $" · {tipos}";
        }

        return new PeriodoInformeViewModel
        {
            Clave = clave,
            Configurada = configurada,
            Filtros = filtrosUso,
            Periodos = periodos,
            Grupos = OpcionesGrupo(grupos),
            Vendedores = vendedores,
            Hoy = hoy,
            Detalle = detalle,
            ResumenCorto = resumen
        };
    }

    public static InformeGeneralViewModel InformeGeneral(
        FuenteInformeVentas fuente,
        FiltrosPeriodoInforme filtros,
        DateOnly desde,
        DateOnly hasta,
        DateTime corte) => new()
    {
        Corte = corte,
        Desde = desde,
        Hasta = hasta,
        Alcance = UiTexts.InformeTodosGrupos,
        TotalPorRecibir = fuente.Recaudo?.TotalEntregarAdministracion ?? 0m,
        Recibido = fuente.Recaudo?.TotalEntregadoAdministracion ?? 0m,
        EnDeuda = Math.Max(0m, (fuente.Recaudo?.TotalEntregarAdministracion ?? 0m) - (fuente.Recaudo?.TotalEntregadoAdministracion ?? 0m)) > 0m,
        Recortado = fuente.Recaudo?.PeriodoRecortado ?? false
    };

    public static IReadOnlyList<OpcionInforme> OpcionesVendedor(IEnumerable<UsuarioResponse> usuarios) => usuarios
        .Where(u => RolConsola.EsVendedor(u.Rol))
        .OrderBy(u => u.NombreCompleto, StringComparer.CurrentCultureIgnoreCase)
        .Select(u => new OpcionInforme
        {
            Valor = u.UsuarioId.ToString(),
            Texto = u.NombreCompleto,
            Detalle = $"{u.Usuario} {u.GrupoNombre}",
            Grupo = u.GrupoId?.ToString() ?? string.Empty,
            Columnas = [u.Usuario, string.IsNullOrWhiteSpace(u.GrupoNombre) ? UiTexts.SinGrupo : u.GrupoNombre, RolUi.Nombre(u.Rol)]
        })
        .ToList();

    /// <summary>Con grupos específicos, deja solo los vendedores de esos grupos; con todos, deja la lista completa.</summary>
    public static IReadOnlyList<OpcionInforme> VendedoresDeLosGrupos(IEnumerable<OpcionInforme> vendedores, IReadOnlyCollection<Guid>? grupos)
    {
        if (grupos is null) return vendedores.ToList();
        var claves = grupos.Select(g => g.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return vendedores.Where(v => claves.Contains(v.Grupo)).ToList();
    }

    public static KpiResponse? FusionarKpi(IReadOnlyList<KpiResponse> partes)
    {
        if (partes.Count == 0) return null;
        if (partes.Count == 1) return partes[0];
        return new KpiResponse
        {
            Corte = partes.Max(p => p.Corte),
            FechaInicial = partes[0].FechaInicial,
            FechaFinal = partes[0].FechaFinal,
            Ingresos = partes.Sum(p => p.Ingresos),
            VentasConfirmadas = partes.Sum(p => p.VentasConfirmadas),
            ValorPremiosEntregados = partes.Sum(p => p.ValorPremiosEntregados),
            EntregasPremio = partes.Sum(p => p.EntregasPremio),
            PremiosEntregadosPorLoteria = partes.SelectMany(p => p.PremiosEntregadosPorLoteria)
                .GroupBy(p => p.Loteria)
                .Select(g => new KpiPremioLoteriaResponse
                {
                    Loteria = g.Key,
                    Valor = g.Sum(p => p.Valor),
                    Cantidad = g.Sum(p => p.Cantidad)
                })
                .OrderByDescending(p => p.Valor)
                .ThenBy(p => p.Loteria, StringComparer.CurrentCultureIgnoreCase)
                .ToList(),
            PremiosEntregados = partes.SelectMany(p => p.PremiosEntregados).ToList(),
            IngresosPorGrupo = partes.SelectMany(p => p.IngresosPorGrupo)
                .GroupBy(b => b.Nombre)
                .Select(g => new KpiBarraGrupoResponse { Nombre = g.Key, Total = g.Sum(b => b.Total) })
                .ToList(),
            IngresosPorVendedor = partes.SelectMany(p => p.IngresosPorVendedor)
                .GroupBy(v => (v.Vendedor, v.Grupo))
                .Select(g => new KpiVendedorFilaResponse { Vendedor = g.Key.Vendedor, Grupo = g.Key.Grupo, Ventas = g.Sum(v => v.Ventas), Total = g.Sum(v => v.Total) })
                .ToList(),
            VentasDetalle = partes.SelectMany(p => p.VentasDetalle).ToList(),
            VentasPorDiaDetalle = partes.SelectMany(p => p.VentasPorDiaDetalle)
                .GroupBy(d => d.Fecha)
                .Select(g => new KpiVentaDiaResponse { Fecha = g.Key, Ventas = g.Sum(d => d.Ventas), Total = g.Sum(d => d.Total) })
                .OrderBy(d => d.Fecha)
                .ToList()
        };
    }

    public static TableroRecaudoResponse? FusionarTablero(IReadOnlyList<TableroRecaudoResponse> partes)
    {
        if (partes.Count == 0) return null;
        if (partes.Count == 1) return partes[0];
        return new TableroRecaudoResponse
        {
            Desde = partes[0].Desde,
            Hasta = partes[0].Hasta,
            PeriodoRecortado = partes.Any(p => p.PeriodoRecortado),
            TotalVendido = partes.Sum(p => p.TotalVendido),
            TotalPorRecaudar = partes.Sum(p => p.TotalPorRecaudar),
            TotalEntregarAdministracion = partes.Sum(p => p.TotalEntregarAdministracion),
            TotalEntregadoAdministracion = partes.Sum(p => p.TotalEntregadoAdministracion),
            TotalRecaudado = partes.Sum(p => p.TotalRecaudado),
            TotalPendiente = partes.Sum(p => p.TotalPendiente),
            DeudaAnterior = partes.Sum(p => p.DeudaAnterior),
            PendienteDelDia = partes.Sum(p => p.PendienteDelDia),
            VendedoresAlDia = partes.Sum(p => p.VendedoresAlDia),
            VendedoresPorCobrar = partes.Sum(p => p.VendedoresPorCobrar),
            VendedoresEnDeuda = partes.Sum(p => p.VendedoresEnDeuda),
            GruposConPendiente = partes.Sum(p => p.GruposConPendiente),
            Dias = partes.SelectMany(p => p.Dias)
                .GroupBy(d => d.Fecha)
                .Select(g => new LineaRecaudoDiaResponse
                {
                    Fecha = g.Key,
                    Debia = g.Sum(d => d.Debia),
                    Cobrado = g.Sum(d => d.Cobrado),
                    Generado = g.Sum(d => d.Generado),
                    EntregarAdministracion = g.Sum(d => d.EntregarAdministracion),
                    EntregadoAdministracion = g.Sum(d => d.EntregadoAdministracion),
                    Vendido = g.Sum(d => d.Vendido)
                })
                .OrderBy(d => d.Fecha)
                .ToList(),
            Recaudadores = partes.SelectMany(p => p.Recaudadores)
                .GroupBy(r => r.Id?.ToString() ?? r.Nombre)
                .Select(g => new RecaudoAgrupadoResponse
                {
                    Id = g.First().Id,
                    Nombre = g.First().Nombre,
                    Detalle = string.Join(", ", g.Select(r => r.Detalle).Where(d => !string.IsNullOrWhiteSpace(d)).Distinct()),
                    Vendedores = g.Sum(r => r.Vendedores),
                    TotalPorRecaudar = g.Sum(r => r.TotalPorRecaudar),
                    TotalRecaudado = g.Sum(r => r.TotalRecaudado),
                    TotalPendiente = g.Sum(r => r.TotalPendiente)
                })
                .ToList(),
            MayoresSaldos = partes.SelectMany(p => p.MayoresSaldos)
                .OrderByDescending(s => s.TotalPendiente)
                .Take(5)
                .ToList(),
            SaldosPendientes = partes.SelectMany(p => p.SaldosPendientes)
                .OrderByDescending(s => s.TotalPendiente)
                .ToList(),
            RecaudadoresDeuda = partes.SelectMany(p => p.RecaudadoresDeuda)
                .GroupBy(r => r.RecaudadorId == Guid.Empty ? r.Nombre : r.RecaudadorId.ToString())
                .Select(g =>
                {
                    var recibir = g.Sum(r => r.Recibir);
                    var entregado = g.Sum(r => r.Entregado);
                    var grupos = g.SelectMany(r => r.Grupos.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                        .Distinct(StringComparer.CurrentCultureIgnoreCase)
                        .OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase);
                    return new RecaudadorDeudaAdministracionResponse
                    {
                        RecaudadorId = g.First().RecaudadorId,
                        Nombre = g.First().Nombre,
                        Grupos = string.Join(", ", grupos),
                        Recibir = recibir,
                        Entregado = entregado,
                        Debe = Math.Max(0m, recibir - entregado)
                    };
                })
                .Where(r => r.Debe > 0m)
                .OrderByDescending(r => r.Debe)
                .ThenBy(r => r.Nombre, StringComparer.CurrentCultureIgnoreCase)
                .ToList()
        };
    }

    public static InformeVentasViewModel Informe(
        FuenteInformeVentas fuente,
        FiltrosPeriodoInforme filtros,
        IReadOnlyList<GrupoResponse> grupos,
        DateOnly desde,
        DateOnly hasta,
        DateTime corte,
        int paginaVendedores,
        int filasVendedores,
        string? ordenRanking = null,
        string? direccionRanking = null) => new()
    {
        Corte = corte,
        Desde = desde,
        Hasta = hasta,
        Alcance = Alcance(filtros, grupos),
        Resumen = Resumen(fuente),
        Ingresos = Ingresos(fuente.Kpi, desde, hasta, paginaVendedores, filasVendedores, InformeOrden.EstadoRanking(ordenRanking, direccionRanking))
    };

    public static InformeEgresosViewModel InformeEgresos(
        FuenteInformeVentas fuente,
        FiltrosPeriodoInforme filtros,
        IReadOnlyList<GrupoResponse> grupos,
        DateOnly desde,
        DateOnly hasta,
        DateTime corte,
        int paginaGastos,
        int filasGastos,
        string? ordenPremios = null,
        string? direccionPremios = null,
        int paginaPremios = 1,
        int filasPremios = 10,
        string? ordenGastos = null,
        string? direccionGastos = null,
        string? ordenCubiertos = null,
        string? direccionCubiertos = null,
        int paginaCubiertos = 1,
        int filasCubiertos = 10)
    {
        var conPremios = Incluye(filtros, TipoEgresoPremios);
        var conGastos = Incluye(filtros, TipoEgresoGastos);
        var conCubierto = Incluye(filtros, TipoEgresoCubierto);
        var completo = Resumen(fuente);
        var resumen = new ResumenFinancieroInforme
        {
            IngresosPda = completo.IngresosPda,
            IngresosWeb = completo.IngresosWeb,
            VentasConfirmadas = completo.VentasConfirmadas,
            PremiosObservador = conPremios ? completo.PremiosObservador : 0m,
            PremiosWeb = conPremios ? completo.PremiosWeb : 0m,
            EntregasPremio = conPremios ? completo.EntregasPremio : 0,
            TotalGastos = conGastos ? completo.TotalGastos : 0m,
            CantidadGastos = conGastos ? completo.CantidadGastos : 0,
            TotalCubierto = conCubierto ? completo.TotalCubierto : 0m,
            JugadasCubiertas = conCubierto ? completo.JugadasCubiertas : 0
        };
        var premiosWeb = conPremios ? fuente.PremiosWeb : [];
        var (columnaGastos, sentidoGastos) = OrdenTabla.Resolver(ordenGastos, direccionGastos, ColumnasGastosPersona, GastoPersonaTotal, OrdenTabla.Descendente);
        var orden = InformePremiosWebOrden.Normalizar(ordenPremios);
        var direccion = InformePremiosWebOrden.Direccion(ordenPremios, direccionPremios);
        var premios = InformePremiosWebOrden.Ordenar(premiosWeb.Select(p => new PremioWebInforme
        {
            Fecha = p.FechaPremio,
            Vendedor = p.Vendedor,
            Loteria = p.Loteria,
            Numero = p.Numero,
            ValorApostado = p.ValorApostado,
            ValorPremio = p.ValorPremio,
            Entrega = InformeTextos.EntregaVendedorWeb
        }).Concat(conPremios
            ? (fuente.Kpi?.PremiosEntregados ?? []).Select(p => new PremioWebInforme
            {
                Fecha = p.Fecha,
                Vendedor = p.Vendedor,
                Loteria = p.Loteria,
                Numero = p.Numero,
                ValorApostado = p.ValorApostado,
                ValorPremio = p.ValorPremio,
                Entrega = InformeTextos.EntregaObservador
            })
            : []), orden, direccion);
        var egresos = Egresos(resumen, fuente, paginaGastos, filasGastos, columnaGastos, sentidoGastos, conPremios, conGastos, conCubierto);
        var porLoteria = premiosWeb
            .GroupBy(p => p.Loteria)
            .Select(g => (Texto: g.Key, Valor: g.Sum(p => p.ValorPremio), Cantidad: g.Count()))
            .Concat(conPremios
                ? (fuente.Kpi?.PremiosEntregadosPorLoteria ?? [])
                    .Select(p => (Texto: p.Loteria, Valor: p.Valor, Cantidad: p.Cantidad))
                : [])
            .GroupBy(p => p.Texto)
            .Select(g => (Texto: g.Key, Valor: g.Sum(p => p.Valor), Cantidad: g.Sum(p => p.Cantidad)))
            .ToList();
        var cubiertos = (conCubierto ? fuente.Cubierto?.Grupos ?? [] : [])
            .Select(g => new CubiertoInforme
            {
                Fecha = DateOnly.FromDateTime(g.FechaOperacion ?? corte),
                Loteria = g.Loteria,
                Numero = g.Numero,
                CubiertoPor = string.IsNullOrWhiteSpace(g.CubiertoPor) ? "-" : g.CubiertoPor,
                Subtotal = g.Subtotal,
                DescuentoPorcentaje = g.DescuentoPorcentaje,
                Descuento = g.DescuentoValor ?? 0m,
                Pagado = g.TotalPagar ?? 0m
            })
            .ToList();
        var cubiertoPorLoteria = cubiertos
            .GroupBy(c => c.Loteria)
            .Select(g => (Texto: g.Key, Valor: g.Sum(c => c.Subtotal), Cantidad: g.Count()))
            .ToList();
        var (columnaCubierto, sentidoCubierto) = OrdenTabla.Resolver(ordenCubiertos, direccionCubiertos, ColumnasCubiertos, CubiertoFecha, OrdenTabla.Descendente);
        var ordenCubierto = new EstadoOrden(columnaCubierto, sentidoCubierto);
        return new InformeEgresosViewModel
        {
            Corte = corte,
            Desde = desde,
            Hasta = hasta,
            Alcance = Alcance(filtros, grupos),
            Resumen = resumen,
            Egresos = new EgresosInforme
            {
                Conceptos = egresos.Conceptos,
                GastosPorTipo = egresos.GastosPorTipo,
                PremiosWeb = PagingHelper.Paginate(premios, paginaPremios, filasPremios),
                OrdenPremios = orden,
                DireccionPremios = direccion,
                GastosPorPersona = egresos.GastosPorPersona,
                OrdenGastos = columnaGastos,
                DireccionGastos = sentidoGastos,
                AnuladosValor = egresos.AnuladosValor,
                AnuladosCantidad = egresos.AnuladosCantidad,
                PremiosPorLoteria = Segmentos(porLoteria, porLoteria.Sum(p => p.Valor)),
                CubiertoPorLoteria = Segmentos(cubiertoPorLoteria, cubiertoPorLoteria.Sum(c => c.Valor)),
                Cubiertos = PagingHelper.Paginate(OrdenarCubiertos(cubiertos, ordenCubierto), paginaCubiertos, filasCubiertos),
                OrdenCubiertos = ordenCubierto,
                IncluyePremios = conPremios,
                IncluyeGastos = conGastos,
                IncluyeCubierto = conCubierto
            }
        };
    }

    public static InformeRecaudoViewModel InformeRecaudo(
        FuenteInformeVentas fuente,
        FiltrosPeriodoInforme filtros,
        IReadOnlyList<GrupoResponse> grupos,
        DateOnly desde,
        DateOnly hasta,
        DateTime corte,
        string? ordenAtrasos = null,
        string? direccionAtrasos = null,
        int paginaAtrasos = 1,
        int filasAtrasos = 10,
        int paginaRecaudadoresDeuda = 1,
        int filasRecaudadoresDeuda = 10) => new()
    {
        Corte = corte,
        Desde = desde,
        Hasta = hasta,
        Alcance = Alcance(filtros, grupos),
        Recaudo = fuente.Recaudo is null
            ? null
            : Recaudo(
                fuente.Recaudo,
                InformeOrden.EstadoAtrasos(ordenAtrasos, direccionAtrasos),
                paginaAtrasos,
                filasAtrasos,
                paginaRecaudadoresDeuda,
                filasRecaudadoresDeuda)
    };

    private static ResumenFinancieroInforme Resumen(FuenteInformeVentas fuente) => new()
    {
        IngresosPda = fuente.Kpi?.Ingresos ?? 0m,
        IngresosWeb = fuente.IngresosWeb,
        PremiosWeb = fuente.PremiosWeb.Sum(p => p.ValorPremio),
        VentasConfirmadas = fuente.Kpi?.VentasConfirmadas ?? 0,
        PremiosObservador = fuente.Kpi?.ValorPremiosEntregados ?? 0m,
        EntregasPremio = (fuente.Kpi?.EntregasPremio ?? 0) + fuente.PremiosWeb.Count,
        TotalGastos = fuente.Gastos?.TotalValor ?? 0m,
        CantidadGastos = fuente.Gastos?.Cantidad ?? 0,
        TotalCubierto = fuente.Cubierto?.Total ?? 0m,
        JugadasCubiertas = fuente.Cubierto?.Jugadas ?? 0
    };

    private static string Alcance(FiltrosPeriodoInforme filtros, IReadOnlyList<GrupoResponse> grupos) =>
        Nombres(
            filtros.Grupos?.Select(id => grupos.FirstOrDefault(g => g.GrupoId == id)?.Nombre ?? id.ToString()).ToList(),
            UiTexts.InformeTodosGrupos);

    private static IngresosInforme Ingresos(KpiResponse? kpi, DateOnly desde, DateOnly hasta, int pagina, int filas, EstadoOrden ordenRanking)
    {
        var porFecha = (kpi?.VentasPorDiaDetalle ?? []).GroupBy(d => d.Fecha)
            .ToDictionary(g => g.Key, g => (Total: g.Sum(d => d.Total), Ventas: g.Sum(d => d.Ventas)));
        var cantidadDias = hasta.DayNumber - desde.DayNumber + 1;
        var crudos = Enumerable.Range(0, cantidadDias)
            .Select(i => desde.AddDays(i))
            .Select(f => (Fecha: f, Dato: porFecha.GetValueOrDefault(f)))
            .ToList();
        var mayor = crudos.Count == 0 ? 0m : crudos.Max(d => d.Dato.Total);
        var dias = crudos
            .Select(d => new IngresoDiaInforme { Fecha = d.Fecha, Total = d.Dato.Total, Ventas = d.Dato.Ventas, Alto = Porcentaje(d.Dato.Total, mayor) })
            .ToList();
        var ingresos = kpi?.Ingresos ?? 0m;
        var ventas = kpi?.VentasConfirmadas ?? 0;

        var porGrupo = (kpi?.IngresosPorGrupo ?? [])
            .GroupBy(b => b.Nombre)
            .Select(g => (Texto: g.Key, Valor: g.Sum(b => b.Total), Cantidad: 0))
            .ToList();

        var vendedores = (kpi?.VentasDetalle ?? [])
            .OrderByDescending(v => v.Total)
            .ThenByDescending(v => v.FechaVenta)
            .ThenBy(v => v.Vendedor, StringComparer.CurrentCultureIgnoreCase)
            .Select((v, i) => new VendedorIngresoInforme
            {
                Posicion = i + 1,
                FechaVenta = v.FechaVenta,
                Vendedor = v.Vendedor,
                Grupo = string.IsNullOrWhiteSpace(v.Grupo) ? UiTexts.SinGrupo : v.Grupo,
                Total = v.Total
            })
            .ToList();

        return new IngresosInforme
        {
            Dias = dias,
            PromedioDiario = cantidadDias == 0 ? 0 : Math.Round(ingresos / cantidadDias, 0, MidpointRounding.AwayFromZero),
            TicketPromedio = ventas == 0 ? 0 : Math.Round(ingresos / ventas, 0, MidpointRounding.AwayFromZero),
            MejorDia = dias.Where(d => d.Total > 0).OrderByDescending(d => d.Total).ThenBy(d => d.Fecha).FirstOrDefault(),
            DiasConVentas = dias.Count(d => d.Total > 0),
            PorGrupo = Segmentos(porGrupo, ingresos),
            Vendedores = PagingHelper.Paginate(InformeOrden.Ranking(vendedores, ordenRanking), pagina, filas),
            OrdenRanking = ordenRanking
        };
    }

    public const string CubiertoFecha = "fecha";
    public const string CubiertoLoteria = "loteria";
    public const string CubiertoNumero = "numero";
    public const string CubiertoQuien = "quien";
    public const string CubiertoSubtotal = "subtotal";
    public const string CubiertoDescuento = "descuento";
    public const string CubiertoPagado = "pagado";
    private static readonly string[] ColumnasCubiertos =
        [CubiertoFecha, CubiertoLoteria, CubiertoNumero, CubiertoQuien, CubiertoSubtotal, CubiertoDescuento, CubiertoPagado];

    private static IReadOnlyList<CubiertoInforme> OrdenarCubiertos(IEnumerable<CubiertoInforme> filas, EstadoOrden estado) =>
        (estado.Orden switch
        {
            CubiertoLoteria => OrdenTabla.Por(filas, c => c.Loteria, estado.Direccion, OrdenTabla.Texto),
            CubiertoNumero => OrdenTabla.Por(filas, c => c.Numero, estado.Direccion, OrdenTabla.Texto),
            CubiertoQuien => OrdenTabla.Por(filas, c => c.CubiertoPor, estado.Direccion, OrdenTabla.Texto),
            CubiertoSubtotal => OrdenTabla.Por(filas, c => c.Subtotal, estado.Direccion),
            CubiertoDescuento => OrdenTabla.Por(filas, c => c.Descuento, estado.Direccion),
            CubiertoPagado => OrdenTabla.Por(filas, c => c.Pagado, estado.Direccion),
            _ => OrdenTabla.Por(filas, c => c.Fecha, estado.Direccion)
        }).ThenBy(c => c.Loteria, OrdenTabla.Texto).ThenBy(c => c.Numero, OrdenTabla.Texto).ToList();

    public const string GastoPersonaFecha = "fecha";
    public const string GastoPersonaPersona = "persona";
    public const string GastoPersonaTipo = "tipo";
    public const string GastoPersonaTotal = "total";
    private static readonly string[] ColumnasGastosPersona = [GastoPersonaFecha, GastoPersonaPersona, GastoPersonaTipo, GastoPersonaTotal];

    private static IEnumerable<GastoPersonaInforme> OrdenarGastosPersona(IEnumerable<GastoPersonaInforme> personas, string orden, string direccion) =>
        (orden switch
        {
            GastoPersonaFecha => OrdenTabla.Por(personas, p => p.Fecha, direccion),
            GastoPersonaPersona => OrdenTabla.Por(personas, p => p.Persona, direccion, OrdenTabla.Texto),
            GastoPersonaTipo => OrdenTabla.Por(personas, p => p.Tipo, direccion, OrdenTabla.Texto),
            _ => OrdenTabla.Por(personas, p => p.Total, direccion)
        }).ThenBy(p => p.Persona, OrdenTabla.Texto);

    private static EgresosInforme Egresos(
        ResumenFinancieroInforme resumen,
        FuenteInformeVentas fuente,
        int pagina,
        int filas,
        string ordenGastos = GastoPersonaTotal,
        string direccionGastos = OrdenTabla.Descendente,
        bool conPremios = true,
        bool conGastos = true,
        bool conCubierto = true)
    {
        var conceptos = new List<(string Texto, decimal Valor, int Cantidad)>();
        if (conPremios) conceptos.Add((UiTexts.InformeConceptoPremios, resumen.TotalPremios, resumen.EntregasPremio));
        if (conGastos) conceptos.Add((UiTexts.InformeConceptoGastos, resumen.TotalGastos, resumen.CantidadGastos));
        if (conCubierto) conceptos.Add((UiTexts.InformeConceptoCubierto, resumen.TotalCubierto, resumen.JugadasCubiertas));
        var porTipo = (conGastos ? fuente.Gastos?.PorTipo ?? [] : [])
            .Select(t => (Texto: t.Tipo, t.Valor, t.Cantidad))
            .ToList();
        var personas = fuente.Movimientos
            .Where(m => conGastos && m.Estado == "Activo")
            .Select(m => new GastoPersonaInforme
            {
                Fecha = m.FechaGasto,
                Persona = string.IsNullOrWhiteSpace(m.Usuario) ? m.UsuarioRegistroId.ToString() : m.Usuario,
                Tipo = m.Tipo,
                Total = m.Valor
            });
        var ordenadas = OrdenarGastosPersona(personas, ordenGastos, direccionGastos).ToList();

        return new EgresosInforme
        {
            Conceptos = Segmentos(conceptos, resumen.TotalEgresos),
            GastosPorTipo = Segmentos(porTipo, resumen.TotalGastos),
            GastosPorPersona = PagingHelper.Paginate(ordenadas, pagina, filas),
            AnuladosValor = conGastos ? fuente.Gastos?.AnuladosValor ?? 0m : 0m,
            AnuladosCantidad = conGastos ? fuente.Gastos?.AnuladosCantidad ?? 0 : 0
        };
    }

    private static RecaudoInforme Recaudo(
        TableroRecaudoResponse tablero,
        EstadoOrden ordenAtrasos,
        int paginaAtrasos,
        int filasAtrasos,
        int paginaRecaudadoresDeuda,
        int filasRecaudadoresDeuda)
    {
        var mayor = tablero.Recaudadores.Count == 0 ? 0m : tablero.Recaudadores.Max(r => r.TotalRecaudado + r.TotalPendiente);
        var debeEntregar = tablero.TotalEntregarAdministracion;
        var valorEntregado = tablero.TotalEntregadoAdministracion;
        return new RecaudoInforme
        {
            PorRecaudar = tablero.TotalPorRecaudar,
            Recaudado = tablero.TotalRecaudado,
            Pendiente = tablero.TotalPendiente,
            DeudaAnterior = tablero.DeudaAnterior,
            PendienteDelDia = tablero.PendienteDelDia,
            DebeEntregar = debeEntregar,
            ValorEntregado = valorEntregado,
            EnDeudaAdministracion = Math.Max(0m, debeEntregar - valorEntregado) > 0m,
            Avance = Avance(tablero.TotalRecaudado, tablero.TotalPendiente),
            VendedoresAlDia = tablero.VendedoresAlDia,
            VendedoresPorCobrar = tablero.VendedoresPorCobrar,
            VendedoresEnDeuda = tablero.VendedoresEnDeuda,
            Grafico = GraficoLineaRecaudo.De(
                tablero.Dias.OrderBy(d => d.Fecha)
                    .Select(d => new PuntoLineaRecaudo(
                        d.Fecha,
                        d.Debia,
                        d.Cobrado,
                        d.Generado,
                        d.EntregarAdministracion,
                        d.EntregadoAdministracion))
                    .ToList(),
                mostrarGenerado: true),
            Recaudadores = tablero.Recaudadores
                .OrderByDescending(r => r.TotalPendiente)
                .ThenByDescending(r => r.TotalRecaudado)
                .ThenBy(r => r.Nombre, StringComparer.CurrentCultureIgnoreCase)
                .Select(r => new RecaudadorInforme
                {
                    Nombre = r.Nombre,
                    Detalle = r.Detalle,
                    Vendedores = r.Vendedores,
                    PorRecaudar = r.TotalPorRecaudar,
                    Recaudado = r.TotalRecaudado,
                    Pendiente = r.TotalPendiente,
                    Avance = Avance(r.TotalRecaudado, r.TotalPendiente),
                    AnchoRecaudado = Porcentaje(r.TotalRecaudado, mayor),
                    AnchoPendiente = Porcentaje(r.TotalPendiente, mayor)
                })
                .ToList(),
            Atrasos = PagingHelper.Paginate(InformeOrden.Atrasos(tablero.SaldosPendientes
                .Select(s => new AtrasoInforme
                {
                    Vendedor = s.Nombre,
                    Grupo = s.Grupo,
                    Recaudador = s.Recaudador,
                    DeudaAnterior = s.DeudaAnterior,
                    PendienteDelDia = s.PendienteDelDia,
                    TotalPendiente = s.TotalPendiente
                }), ordenAtrasos), paginaAtrasos, filasAtrasos),
            OrdenAtrasos = ordenAtrasos,
            RecaudadoresDeuda = PagingHelper.Paginate(
                tablero.RecaudadoresDeuda
                    .Where(r => r.Debe > 0m)
                    .OrderByDescending(r => r.Debe)
                    .ThenBy(r => r.Nombre, StringComparer.CurrentCultureIgnoreCase)
                    .Select(r => new RecaudadorDeudaInforme
                    {
                        Nombre = r.Nombre,
                        Grupos = r.Grupos,
                        Recibir = r.Recibir,
                        Entregado = r.Entregado,
                        Debe = r.Debe
                    })
                    .ToList(),
                paginaRecaudadoresDeuda,
                filasRecaudadoresDeuda),
            Recortado = tablero.PeriodoRecortado
        };
    }

    private static int Avance(decimal recaudado, decimal pendiente) =>
        recaudado + pendiente <= 0 ? 0 : (int)Math.Round(recaudado * 100m / (recaudado + pendiente), MidpointRounding.AwayFromZero);

    private static IReadOnlyList<SegmentoValorInforme> Segmentos(IReadOnlyList<(string Texto, decimal Valor, int Cantidad)> partes, decimal total)
    {
        var mayor = partes.Count == 0 ? 0m : partes.Max(p => p.Valor);
        return partes
            .OrderByDescending(p => p.Valor)
            .ThenBy(p => p.Texto, StringComparer.CurrentCultureIgnoreCase)
            .Select(p => new SegmentoValorInforme
            {
                Texto = p.Texto,
                Valor = p.Valor,
                Cantidad = p.Cantidad,
                Porcentaje = Porcentaje(p.Valor, total),
                Ancho = Porcentaje(p.Valor, mayor)
            })
            .ToList();
    }

    private static decimal Porcentaje(decimal parte, decimal total) =>
        total <= 0 ? 0 : Math.Round(parte * 100m / total, 1, MidpointRounding.AwayFromZero);

    private static string Fechas(DateOnly desde, DateOnly hasta) =>
        desde == hasta
            ? desde.ToString("dd/MM/yyyy", Cultura)
            : string.Format(UiTexts.InformeRangoTexto, desde.ToString("dd/MM/yyyy", Cultura), hasta.ToString("dd/MM/yyyy", Cultura));

    private static string Nombres(IReadOnlyList<string>? nombres, string todos)
    {
        if (nombres is null) return todos;
        return nombres.Count <= 3
            ? string.Join(", ", nombres)
            : string.Format(UiTexts.InformeYMas, string.Join(", ", nombres.Take(3)), nombres.Count - 3);
    }
}
