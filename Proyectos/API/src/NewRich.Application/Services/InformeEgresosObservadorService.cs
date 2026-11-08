using NewRich.Application.Contracts.Informes;
using NewRich.Application.Contracts.Kpi;
using NewRich.Application.Informes;
using NewRich.Constants.Messages;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public interface IInformeEgresosObservadorService
{
    Task<Result<InformeEgresosObservadorResponse>> ConsultarAsync(
        DateOnly desde,
        DateOnly hasta,
        Guid? grupoId,
        Guid? vendedorId,
        CancellationToken cancellationToken);
}

public sealed class InformeEgresosObservadorService : IInformeEgresosObservadorService
{
    private readonly IKpiService _kpi;
    private readonly IPremioEntregadoWebService _premios;
    private readonly IGastosUtilidadesService _gastos;
    private readonly IRevisadaManualService _revisada;

    public InformeEgresosObservadorService(
        IKpiService kpi,
        IPremioEntregadoWebService premios,
        IGastosUtilidadesService gastos,
        IRevisadaManualService revisada)
    {
        _kpi = kpi;
        _premios = premios;
        _gastos = gastos;
        _revisada = revisada;
    }

    public async Task<Result<InformeEgresosObservadorResponse>> ConsultarAsync(
        DateOnly desde,
        DateOnly hasta,
        Guid? grupoId,
        Guid? vendedorId,
        CancellationToken cancellationToken)
    {
        if (desde > hasta)
        {
            return Result<InformeEgresosObservadorResponse>.Fail(UtilidadesMessages.PeriodoInvalido);
        }

        var hoy = DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        var indicadores = await _kpi.ConsultarAsync(new KpiRequest
        {
            GrupoId = grupoId,
            VendedorId = vendedorId,
            FechaInicial = desde.ToDateTime(TimeOnly.MinValue),
            FechaFinal = hasta.ToDateTime(TimeOnly.MaxValue),
            CompararAnterior = false
        }, cancellationToken);
        if (!indicadores.IsSuccess || indicadores.Data is null)
        {
            return Fallo(indicadores);
        }

        var web = await _premios.ListadoAsync(desde, hasta, grupoId, vendedorId, cancellationToken);
        if (!web.IsSuccess || web.Data is null)
        {
            return Fallo(web);
        }

        var gastos = vendedorId is Guid persona
            ? await _gastos.ConsultarInformePropioAsync(persona, "rango", desde, hasta, hoy, cancellationToken)
            : await _gastos.ConsultarInformeAsync("rango", desde, hasta, hoy, cancellationToken);
        if (!gastos.IsSuccess || gastos.Data is null)
        {
            return Fallo(gastos);
        }

        var inicio = desde.ToDateTime(TimeOnly.MinValue);
        var fin = hasta.ToDateTime(TimeOnly.MinValue).AddDays(1).AddTicks(-1);
        var cubierto = vendedorId is Guid vendedor
            ? await _revisada.CubiertoAsync(inicio, fin, [vendedor], cancellationToken)
            : await _revisada.CubiertoAsync(inicio, fin, cancellationToken);
        if (!cubierto.IsSuccess || cubierto.Data is null)
        {
            return Fallo(cubierto);
        }

        return Result<InformeEgresosObservadorResponse>.Ok(
            InformeEgresosLectura.Armar(
                indicadores.Data.PremiosEntregadosPorLoteria,
                web.Data,
                gastos.Data.PorTipo,
                cubierto.Data.Grupos,
                hasta),
            SuccessMessages.OperacionExitosa);
    }

    private static Result<InformeEgresosObservadorResponse> Fallo<T>(Result<T> origen) =>
        Result<InformeEgresosObservadorResponse>.Fail(
            string.IsNullOrWhiteSpace(origen.Message) ? UtilidadesMessages.PeriodoInvalido : origen.Message,
            origen.IsSuccess ? 500 : origen.StatusCode);
}
