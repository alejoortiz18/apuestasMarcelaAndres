using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.RevisadaManual;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public interface IParametrosRevisadaManualService
{
    Task<Result<ParametrosRevisadaManualResponse>> ObtenerAsync(CancellationToken cancellationToken);
    Task<Result<ParametrosRevisadaManualResponse>> GuardarComisionAsync(decimal porcentaje, CancellationToken cancellationToken);
    Task<Result<ParametrosRevisadaManualResponse>> GuardarDescuentoAsync(decimal porcentaje, CancellationToken cancellationToken);
}

public sealed class ParametrosRevisadaManualService : IParametrosRevisadaManualService
{
    private readonly INewRichDbContext _db;

    public ParametrosRevisadaManualService(INewRichDbContext db) => _db = db;

    public async Task<Result<ParametrosRevisadaManualResponse>> ObtenerAsync(CancellationToken cancellationToken)
    {
        var fila = await _db.ParametrosRevisadaManual.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ParametroId == ParametroRevisadaManual.FilaUnica, cancellationToken);
        return Result<ParametrosRevisadaManualResponse>.Ok(Mapa(fila), RevisadaManualMessages.Consultado);
    }

    public Task<Result<ParametrosRevisadaManualResponse>> GuardarComisionAsync(decimal porcentaje, CancellationToken cancellationToken) =>
        GuardarAsync(porcentaje, comision: true, cancellationToken);

    public Task<Result<ParametrosRevisadaManualResponse>> GuardarDescuentoAsync(decimal porcentaje, CancellationToken cancellationToken) =>
        GuardarAsync(porcentaje, comision: false, cancellationToken);

    private async Task<Result<ParametrosRevisadaManualResponse>> GuardarAsync(decimal porcentaje, bool comision, CancellationToken cancellationToken)
    {
        if (porcentaje < 0 || porcentaje > 99 || porcentaje != decimal.Truncate(porcentaje))
        {
            return Result<ParametrosRevisadaManualResponse>.Fail(RevisadaManualMessages.ParametroPorcentajeInvalido);
        }

        var fila = await _db.ParametrosRevisadaManual
            .FirstOrDefaultAsync(p => p.ParametroId == ParametroRevisadaManual.FilaUnica, cancellationToken);
        if (fila is null)
        {
            fila = new ParametroRevisadaManual { ParametroId = ParametroRevisadaManual.FilaUnica };
            _db.ParametrosRevisadaManual.Add(fila);
        }

        if (comision)
        {
            fila.ComisionPorcentaje = porcentaje;
        }
        else
        {
            fila.DescuentoPorcentaje = porcentaje;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<ParametrosRevisadaManualResponse>.Ok(Mapa(fila), RevisadaManualMessages.ParametroGuardado);
    }

    private static ParametrosRevisadaManualResponse Mapa(ParametroRevisadaManual? fila) => new()
    {
        ComisionPorcentaje = fila?.ComisionPorcentaje,
        DescuentoPorcentaje = fila?.DescuentoPorcentaje
    };
}
