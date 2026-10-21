using NewRich.Admin.Models;
using NewRich.Application.Contracts.Grupos;
using NewRich.Application.Contracts.Kpi;
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
    Ventas = Kpi | Gastos | Cubierto,
    Egresos = Kpi | Gastos | Movimientos | Cubierto
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
        if (datos.HasFlag(DatosInforme.Kpi))
        {
            foreach (var grupoId in filtros.Grupos?.Cast<Guid?>() ?? [null])
            {
                var kpi = await api.ConsultarKpiAsync(new KpiRequest
                {
                    GrupoId = grupoId,
                    FechaInicial = desde.ToDateTime(TimeOnly.MinValue),
                    FechaFinal = hasta.ToDateTime(TimeOnly.MinValue),
                    CompararAnterior = false
                }, cancellationToken);
                if (Revisar(kpi)) kpis.Add(kpi.Data!);
                if (unauthorized) return new CargaInformeVentas { Unauthorized = true };
            }
        }

        InformeGastosPropiosResponse? gastos = null;
        if (datos.HasFlag(DatosInforme.Gastos))
        {
            var respuesta = await api.UtilidadesIndicadoresAsync(VentasInforme.PeriodoRango, desde, hasta, cancellationToken);
            if (Revisar(respuesta)) gastos = respuesta.Data;
        }

        var movimientos = new List<MovimientoGastoResponse>();
        if (datos.HasFlag(DatosInforme.Movimientos))
        {
            for (var pagina = 1; pagina <= PaginasMaximas; pagina++)
            {
                var lote = await api.UtilidadesMovimientosAsync(VentasInforme.PeriodoRango, desde, hasta, null, null, "Activo", pagina, FilasPorConsulta, cancellationToken);
                if (!Revisar(lote)) break;
                movimientos.AddRange(lote.Data!.Items);
                if (pagina * FilasPorConsulta >= lote.Data.TotalFilas || lote.Data.Items.Count == 0) break;
            }
        }

        VistaRevisadaManualResponse? cubierto = null;
        if (datos.HasFlag(DatosInforme.Cubierto))
        {
            var respuesta = await api.RevisadaCubiertoAsync(
                desde.ToDateTime(TimeOnly.MinValue),
                hasta.ToDateTime(TimeOnly.MinValue).AddDays(1).AddTicks(-1),
                cancellationToken);
            if (Revisar(respuesta)) cubierto = respuesta.Data;
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
                var tablero = await api.TableroRecaudoAsync(desde, hasta, null, nombre, cancellationToken);
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
                Gastos = gastos,
                Movimientos = movimientos,
                Cubierto = cubierto,
                Recaudo = VentasInforme.FusionarTablero(tableros)
            }
        };
    }
}
