using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Jornadas;
using NewRich.Constants.Messages;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class JornadaService : IJornadaService
{
    private readonly INewRichDbContext _db;

    public JornadaService(INewRichDbContext db)
    {
        _db = db;
    }

    public async Task<Result<IReadOnlyList<JornadaResponse>>> ListarAsync(CancellationToken cancellationToken)
    {
        var items = await _db.Jornadas
            .Select(j => new JornadaResponse
            {
                JornadaId = j.JornadaId,
                Nombre = j.Nombre,
                CantidadLoterias = j.Loterias.Count
            })
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<JornadaResponse>>.Ok(
            items.OrderBy(j => JornadaPorHoraCierre.Orden(j.Nombre)).ThenBy(j => j.Nombre).ToList(),
            SuccessMessages.OperacionExitosa);
    }
}
