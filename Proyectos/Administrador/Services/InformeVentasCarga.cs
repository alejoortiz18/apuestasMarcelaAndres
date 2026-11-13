using NewRich.Admin.Models;
using NewRich.Application.Contracts.Grupos;
using NewRich.Application.Contracts.Kpi;
using NewRich.Application.Contracts.PremiosEntregadosWeb;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Application.Contracts.RevisadaManual;
using NewRich.Application.Contracts.Utilidades;

namespace NewRich.Admin.Services;

public sealed class CargaInformeVentas
{
    public FuenteInformeVentas Fuente { get; init; } = new();
    public string? Error { get; init; }
    public bool Unauthorized { get; init; }
}

[Flags]
public enum DatosInforme
{
    Ninguno = 0,
    Kpi = 1,
    Gastos = 2,
    Movimientos = 4,
    Cubierto = 8,
    Recaudo = 16,
    PremiosWeb = 32,
    Ventas = Kpi | Gastos | Cubierto | PremiosWeb,
    Egresos = Kpi | Gastos | Movimientos | Cubierto | PremiosWeb
}

public static class InformeVentasCarga
{
    private const int FilasPorConsulta = 15;
    private const int PaginasMaximas = 60;

    public static async Task<CargaInformeVentas> CargarAsync(
        IAdminApiClient api,
        DatosInforme datos,
        FiltrosPeriodoInforme filtros,
        IReadOnlyList<GrupoResponse> grupos,
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken)
    {
        string? error = null;
        var unauthorized = false;

        bool Revisar<T>(ApiCallResult<T> resultado)
        {
            unauthorized |= resultado.Unauthorized;
            if (!resultado.Success) error ??= resultado.Message;
            return resultado.Success && resultado.Data is not null;
        }

        var kpis = new List<KpiResponse>();
        var ingresosWeb = 0m;
        var vendedores = filtros.Vendedores?.Cast<Guid?>().ToList() ?? [null];
        if (datos.HasFlag(DatosInforme.Kpi))
        {
            foreach (var grupoId in filtros.Grupos?.Cast<Guid?>() ?? [null])
            {
                foreach (var vendedorId in vendedores)
                {
                    var kpi = await api.ConsultarKpiAsync(new KpiRequest
                    {
                        GrupoId = grupoId,
                        VendedorId = vendedorId,
                        FechaInicial = desde.ToDateTime(TimeOnly.MinValue),
                        FechaFinal = hasta.ToDateTime(TimeOnly.MinValue),
                        CompararAnterior = false
                    }, cancellationToken);
                    if (Revisar(kpi)) kpis.Add(kpi.Data!);
                    if (unauthorized) return new CargaInformeVentas { Unauthorized = true };

                    var web = await api.TotalVentasRegistradasWebAsync(desde, hasta, grupoId, vendedorId, cancellationToken);
                    if (Revisar(web)) ingresosWeb += web.Data!.Total + web.Data.TotalJuegos;
                    if (unauthorized) return new CargaInformeVentas { Unauthorized = true };
                }
            }
        }

        var premiosWeb = new Dictionary<Guid, PremioEntregadoWebResponse>();
        if (datos.HasFlag(DatosInforme.PremiosWeb))
        {
            foreach (var grupoId in filtros.Grupos?.Cast<Guid?>() ?? [null])
            {
                var lote = await api.ListadoPremiosEntregadosWebAsync(desde, hasta, grupoId, cancellationToken);
                if (Revisar(lote))
                {
                    foreach (var premio in lote.Data!.Where(p => filtros.Vendedores is null || filtros.Vendedores.Contains(p.VendedorId)))
                    {
                        premiosWeb.TryAdd(premio.Id, premio);
                    }
                }
                if (unauthorized) return new CargaInformeVentas { Unauthorized = true };
            }
        }

        async Task<List<MovimientoGastoResponse>> Movimientos(Guid? usuarioId, string? estado)
        {
            var lista = new List<MovimientoGastoResponse>();
            for (var pagina = 1; pagina <= PaginasMaximas; pagina++)
            {
                var lote = await api.UtilidadesMovimientosAsync(VentasInforme.PeriodoRango, desde, hasta, usuarioId, null, estado, pagina, FilasPorConsulta, cancellationToken);
                if (!Revisar(lote)) break;
                lista.AddRange(lote.Data!.Items);
                if (pagina * FilasPorConsulta >= lote.Data.TotalFilas || lote.Data.Items.Count == 0) break;
            }

            return lista;
        }

        InformeGastosPropiosResponse? gastos = null;
        var movimientos = new List<MovimientoGastoResponse>();
        if (filtros.Vendedores is not null && datos.HasFlag(DatosInforme.Gastos))
        {
            // Con vendedores elegidos los gastos salen solo de lo que ellos registraron.
            var suyos = new List<MovimientoGastoResponse>();
            foreach (var vendedorId in filtros.Vendedores)
            {
                suyos.AddRange(await Movimientos(vendedorId, null));
            }

            gastos = GastosDe(suyos, desde, hasta);
            if (datos.HasFlag(DatosInforme.Movimientos))
            {
                movimientos = suyos.Where(m => m.Estado == "Activo").ToList();
            }
        }
        else
        {
            if (datos.HasFlag(DatosInforme.Gastos))
            {
                var respuesta = await api.UtilidadesIndicadoresAsync(VentasInforme.PeriodoRango, desde, hasta, cancellationToken);
                if (Revisar(respuesta)) gastos = respuesta.Data;
            }

            if (datos.HasFlag(DatosInforme.Movimientos))
            {
                movimientos = await Movimientos(null, "Activo");
            }
        }

        VistaRevisadaManualResponse? cubierto = null;
        VistaRevisadaManualResponse? cubiertoPda = null;
        if (datos.HasFlag(DatosInforme.Cubierto))
        {
            var inicio = desde.ToDateTime(TimeOnly.MinValue);
            var fin = hasta.ToDateTime(TimeOnly.MinValue).AddDays(1).AddTicks(-1);
            var respuesta = filtros.Vendedores is null
                ? await api.RevisadaCubiertoAsync(inicio, fin, cancellationToken)
                : await api.RevisadaCubiertoAsync(inicio, fin, filtros.Vendedores, cancellationToken);
            if (Revisar(respuesta)) cubierto = respuesta.Data;
            var pda = await api.CubrirVentaCubiertoAsync(inicio, fin, filtros.Vendedores, cancellationToken);
            if (Revisar(pda)) cubiertoPda = pda.Data;
        }

        var tableros = new List<TableroRecaudoResponse>();
        if (datos.HasFlag(DatosInforme.Recaudo))
        {
            var nombres = filtros.Grupos?
                .Select(id => grupos.FirstOrDefault(g => g.GrupoId == id)?.Nombre)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Cast<string?>()
                .ToList() ?? [null];
            foreach (var nombre in nombres)
            {
                var tablero = filtros.Vendedores is null
                    ? await api.TableroRecaudoAsync(desde, hasta, null, nombre, cancellationToken)
                    : await api.TableroRecaudoAsync(desde, hasta, null, nombre, filtros.Vendedores, cancellationToken);
                if (Revisar(tablero)) tableros.Add(tablero.Data!);
            }
        }

        if (unauthorized) return new CargaInformeVentas { Unauthorized = true };

        return new CargaInformeVentas
        {
            Error = error,
            Fuente = new FuenteInformeVentas
            {
                Kpi = VentasInforme.FusionarKpi(kpis),
                IngresosWeb = ingresosWeb,
                PremiosWeb = premiosWeb.Values.ToList(),
                Gastos = gastos,
                Movimientos = movimientos,
                Cubierto = cubierto,
                CubiertoPda = cubiertoPda,
                Recaudo = VentasInforme.FusionarTablero(tableros)
            }
        };
    }

    /// <summary>Resumen de gastos armado con los movimientos de los vendedores elegidos.</summary>
    private static InformeGastosPropiosResponse GastosDe(IReadOnlyList<MovimientoGastoResponse> movimientos, DateOnly desde, DateOnly hasta)
    {
        var activos = movimientos.Where(m => m.Estado == "Activo").ToList();
        var anulados = movimientos.Where(m => m.Estado != "Activo").ToList();
        var total = activos.Sum(m => m.Valor);
        var mayor = activos.OrderByDescending(m => m.Valor).ThenByDescending(m => m.FechaGasto).FirstOrDefault();
        return new InformeGastosPropiosResponse(
            desde,
            hasta,
            total,
            activos.Count,
            activos.Count == 0 ? 0m : decimal.Round(total / activos.Count, 2, MidpointRounding.AwayFromZero),
            mayor?.Valor,
            mayor?.Tipo,
            mayor?.FechaGasto,
            anulados.Sum(m => m.Valor),
            anulados.Count,
            activos.GroupBy(m => m.Tipo)
                .Select(g => new GastoPorTipoResponse(g.Key, g.Sum(m => m.Valor), g.Count()))
                .OrderByDescending(t => t.Valor).ThenBy(t => t.Tipo)
                .ToList(),
            activos.GroupBy(m => m.FechaGasto)
                .Select(g => new GastoPorDiaResponse(g.Key, g.Sum(m => m.Valor)))
                .OrderBy(d => d.Fecha)
                .ToList(),
            0m,
            0m);
    }
}
