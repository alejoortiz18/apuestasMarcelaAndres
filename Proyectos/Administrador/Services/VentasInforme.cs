using System.Globalization;
using System.Text.Json;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Application.Contracts.Grupos;
using NewRich.Application.Contracts.Kpi;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Application.Contracts.RevisadaManual;
using NewRich.Application.Contracts.Utilidades;
using NewRich.Constants;

namespace NewRich.Admin.Services;

public sealed class FuenteInformeVentas
{
    public KpiResponse? Kpi { get; init; }
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

    public static readonly IReadOnlyList<string> CategoriasPorPeriodo =
        [CategoriasInforme.Ventas, CategoriasInforme.Egresos, CategoriasInforme.Recaudo];

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

    public static PeriodoInformeViewModel Construir(string clave, IReadOnlyList<GrupoResponse> grupos, FiltrosPeriodoInforme filtros, bool configurada, DateOnly hoy)
    {
        var periodos = OpcionesPeriodo();
        var (desde, hasta) = Rango(filtros, hoy);
        var periodo = $"{periodos.First(p => p.Valor == (filtros.Periodo is PeriodoHoy or Periodo15 or Periodo30 or PeriodoRango ? filtros.Periodo : Periodo7)).Texto} ({Fechas(desde, hasta)})";
        var alcance = Alcance(filtros, grupos);

        return new PeriodoInformeViewModel
        {
            Clave = clave,
            Configurada = configurada,
            Filtros = filtros,
            Periodos = periodos,
            Grupos = OpcionesGrupo(grupos),
            Hoy = hoy,
            Detalle =
            [
                new() { Etiqueta = UiTexts.InformeEtiquetaPeriodo, Valor = periodo },
                new() { Etiqueta = UiTexts.InformeEtiquetaGrupos, Valor = alcance }
            ],
            ResumenCorto = $"{periodo} · {alcance}"
        };
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
            IngresosPorGrupo = partes.SelectMany(p => p.IngresosPorGrupo)
                .GroupBy(b => b.Nombre)
                .Select(g => new KpiBarraGrupoResponse { Nombre = g.Key, Total = g.Sum(b => b.Total) })
                .ToList(),
            IngresosPorVendedor = partes.SelectMany(p => p.IngresosPorVendedor)
                .GroupBy(v => (v.Vendedor, v.Grupo))
                .Select(g => new KpiVendedorFilaResponse { Vendedor = g.Key.Vendedor, Grupo = g.Key.Grupo, Ventas = g.Sum(v => v.Ventas), Total = g.Sum(v => v.Total) })
                .ToList(),
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
        int filasVendedores) => new()
    {
        Corte = corte,
        Desde = desde,
        Hasta = hasta,
        Alcance = Alcance(filtros, grupos),
        Resumen = Resumen(fuente),
        Ingresos = Ingresos(fuente.Kpi, desde, hasta, paginaVendedores, filasVendedores)
    };

    public static InformeEgresosViewModel InformeEgresos(
        FuenteInformeVentas fuente,
        FiltrosPeriodoInforme filtros,
        IReadOnlyList<GrupoResponse> grupos,
        DateOnly desde,
        DateOnly hasta,
        DateTime corte,
        int paginaGastos,
        int filasGastos)
    {
        var resumen = Resumen(fuente);
        return new InformeEgresosViewModel
        {
            Corte = corte,
            Desde = desde,
            Hasta = hasta,
            Alcance = Alcance(filtros, grupos),
            Resumen = resumen,
            Egresos = Egresos(resumen, fuente, paginaGastos, filasGastos)
        };
    }

    public static InformeRecaudoViewModel InformeRecaudo(
        FuenteInformeVentas fuente,
        FiltrosPeriodoInforme filtros,
        IReadOnlyList<GrupoResponse> grupos,
        DateOnly desde,
        DateOnly hasta,
        DateTime corte) => new()
    {
        Corte = corte,
        Desde = desde,
        Hasta = hasta,
        Alcance = Alcance(filtros, grupos),
        Recaudo = fuente.Recaudo is null ? null : Recaudo(fuente.Recaudo)
    };

    private static ResumenFinancieroInforme Resumen(FuenteInformeVentas fuente) => new()
    {
        IngresosPda = fuente.Kpi?.Ingresos ?? 0m,
        // Las ventas y los premios del vendedor web aún no tienen registro propio; se suman aquí cuando existan.
        IngresosWeb = 0m,
        PremiosWeb = 0m,
        VentasConfirmadas = fuente.Kpi?.VentasConfirmadas ?? 0,
        PremiosObservador = fuente.Kpi?.ValorPremiosEntregados ?? 0m,
        EntregasPremio = fuente.Kpi?.EntregasPremio ?? 0,
        TotalGastos = fuente.Gastos?.TotalValor ?? 0m,
        CantidadGastos = fuente.Gastos?.Cantidad ?? 0,
        TotalCubierto = fuente.Cubierto?.Total ?? 0m,
        JugadasCubiertas = fuente.Cubierto?.Jugadas ?? 0
    };

    private static string Alcance(FiltrosPeriodoInforme filtros, IReadOnlyList<GrupoResponse> grupos) =>
        Nombres(
            filtros.Grupos?.Select(id => grupos.FirstOrDefault(g => g.GrupoId == id)?.Nombre ?? id.ToString()).ToList(),
            UiTexts.InformeTodosGrupos);

    private static IngresosInforme Ingresos(KpiResponse? kpi, DateOnly desde, DateOnly hasta, int pagina, int filas)
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

        var vendedores = (kpi?.IngresosPorVendedor ?? [])
            .GroupBy(v => (v.Vendedor, v.Grupo))
            .Select(g => (g.Key.Vendedor, g.Key.Grupo, Ventas: g.Sum(v => v.Ventas), Total: g.Sum(v => v.Total)))
            .OrderByDescending(v => v.Total)
            .ThenByDescending(v => v.Ventas)
            .ThenBy(v => v.Vendedor, StringComparer.CurrentCultureIgnoreCase)
            .Select((v, i) => new VendedorIngresoInforme
            {
                Posicion = i + 1,
                Vendedor = v.Vendedor,
                Grupo = string.IsNullOrWhiteSpace(v.Grupo) ? UiTexts.SinGrupo : v.Grupo,
                Ventas = v.Ventas,
                Total = v.Total,
                Participacion = Porcentaje(v.Total, ingresos)
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
            Vendedores = PagingHelper.Paginate(vendedores, pagina, filas)
        };
    }

    private static EgresosInforme Egresos(ResumenFinancieroInforme resumen, FuenteInformeVentas fuente, int pagina, int filas)
    {
        var conceptos = new List<(string Texto, decimal Valor, int Cantidad)>
        {
            (UiTexts.InformeConceptoPremios, resumen.TotalPremios, resumen.EntregasPremio),
            (UiTexts.InformeConceptoGastos, resumen.TotalGastos, resumen.CantidadGastos),
            (UiTexts.InformeConceptoCubierto, resumen.TotalCubierto, resumen.JugadasCubiertas)
        };
        var porTipo = (fuente.Gastos?.PorTipo ?? [])
            .Select(t => (Texto: t.Tipo, t.Valor, t.Cantidad))
            .ToList();
        var activos = fuente.Movimientos.Where(m => m.Estado == "Activo").ToList();
        var total = activos.Sum(m => m.Valor);
        var personas = activos
            .GroupBy(m => string.IsNullOrWhiteSpace(m.Usuario) ? m.UsuarioRegistroId.ToString() : m.Usuario)
            .Select(g => new GastoPersonaInforme
            {
                Persona = g.Key,
                Cantidad = g.Count(),
                Total = g.Sum(m => m.Valor),
                Participacion = Porcentaje(g.Sum(m => m.Valor), total),
                TipoPrincipal = g.GroupBy(m => m.Tipo).OrderByDescending(t => t.Sum(m => m.Valor)).ThenBy(t => t.Key).First().Key
            })
            .OrderByDescending(p => p.Total)
            .ThenBy(p => p.Persona, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new EgresosInforme
        {
            Conceptos = Segmentos(conceptos, resumen.TotalEgresos),
            GastosPorTipo = Segmentos(porTipo, resumen.TotalGastos),
            GastosPorPersona = PagingHelper.Paginate(personas, pagina, filas),
            AnuladosValor = fuente.Gastos?.AnuladosValor ?? 0m,
            AnuladosCantidad = fuente.Gastos?.AnuladosCantidad ?? 0
        };
    }

    private static RecaudoInforme Recaudo(TableroRecaudoResponse tablero)
    {
        var mayor = tablero.Recaudadores.Count == 0 ? 0m : tablero.Recaudadores.Max(r => r.TotalRecaudado + r.TotalPendiente);
        return new RecaudoInforme
        {
            PorRecaudar = tablero.TotalPorRecaudar,
            Recaudado = tablero.TotalRecaudado,
            Pendiente = tablero.TotalPendiente,
            DeudaAnterior = tablero.DeudaAnterior,
            PendienteDelDia = tablero.PendienteDelDia,
            Avance = Avance(tablero.TotalRecaudado, tablero.TotalPendiente),
            VendedoresAlDia = tablero.VendedoresAlDia,
            VendedoresPorCobrar = tablero.VendedoresPorCobrar,
            VendedoresEnDeuda = tablero.VendedoresEnDeuda,
            Grafico = GraficoLineaRecaudo.De(
                tablero.Dias.OrderBy(d => d.Fecha).Select(d => new PuntoLineaRecaudo(d.Fecha, d.Debia, d.Cobrado, d.Generado)).ToList(),
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
            Atrasos = tablero.MayoresSaldos
                .OrderByDescending(s => s.TotalPendiente)
                .Select(s => new AtrasoInforme
                {
                    Vendedor = s.Nombre,
                    Grupo = s.Grupo,
                    Recaudador = s.Recaudador,
                    DeudaAnterior = s.DeudaAnterior,
                    PendienteDelDia = s.PendienteDelDia,
                    TotalPendiente = s.TotalPendiente
                })
                .ToList(),
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
