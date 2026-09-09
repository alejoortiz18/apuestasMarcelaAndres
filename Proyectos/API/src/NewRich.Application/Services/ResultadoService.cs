using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Resultados;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class ResultadoService : IResultadoService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;

    public ResultadoService(INewRichDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<ResultadoResponse>> RegistrarAsync(RegistrarResultadoRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Numero) || request.Numero.Length != 4 || !request.Numero.All(char.IsDigit))
        {
            return Result<ResultadoResponse>.Fail(ValidationMessages.NumeroApuestaFormato);
        }

        var loteria = await _db.Loterias.FirstOrDefaultAsync(l => l.LoteriaId == request.LoteriaId, cancellationToken);
        if (loteria is null)
        {
            return Result<ResultadoResponse>.Fail(VentaMessages.LoteriaNoEncontrada, 404);
        }

        var fecha = request.FechaJuego.ToDateTime(TimeOnly.MinValue);
        if (await _db.NumerosGanadores.AnyAsync(n => n.LoteriaId == request.LoteriaId && n.FechaJuego == fecha, cancellationToken))
        {
            return Result<ResultadoResponse>.Fail(BoletoMessages.ResultadoDuplicado, 409);
        }

        var entity = new NumeroGanador
        {
            NumeroGanadorId = Guid.NewGuid(),
            LoteriaId = request.LoteriaId,
            FechaJuego = fecha,
            Numero = request.Numero,
            FechaRegistro = _clock.UtcNow,
            Loteria = loteria
        };
        _db.NumerosGanadores.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<ResultadoResponse>.Created(Map(entity), SuccessMessages.ResultadoRegistrado);
    }

    public async Task<Result<IReadOnlyList<ResultadoResponse>>> ListarAsync(DateOnly? fecha, Guid? loteriaId, CancellationToken cancellationToken)
    {
        var query = _db.NumerosGanadores.Include(n => n.Loteria).AsQueryable();
        if (fecha.HasValue)
        {
            var dia = fecha.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(n => n.FechaJuego == dia);
        }

        if (loteriaId.HasValue)
        {
            query = query.Where(n => n.LoteriaId == loteriaId);
        }

        var items = await query.OrderByDescending(n => n.FechaJuego).ToListAsync(cancellationToken);
        return Result<IReadOnlyList<ResultadoResponse>>.Ok(items.Select(Map).ToList(), SuccessMessages.OperacionExitosa);
    }

    private static ResultadoResponse Map(NumeroGanador n) => new()
    {
        NumeroGanadorId = n.NumeroGanadorId,
        LoteriaId = n.LoteriaId,
        Loteria = n.Loteria?.Nombre ?? string.Empty,
        FechaJuego = DateOnly.FromDateTime(n.FechaJuego),
        Numero = n.Numero
    };
}
