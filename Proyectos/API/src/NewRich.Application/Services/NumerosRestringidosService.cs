using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class NumerosRestringidosService : INumerosRestringidosService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;

    public NumerosRestringidosService(INewRichDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<NumeroRestringidoResponse>>> ListarAsync(CancellationToken cancellationToken)
    {
        var items = await _db.NumerosRestringidos
            .AsNoTracking()
            .OrderBy(x => x.Numero)
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyList<NumeroRestringidoResponse>>.Ok(items.Select(Map).ToList(), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<NumeroRestringidoResponse>> AgregarAsync(string numero, CancellationToken cancellationToken)
    {
        if (!NumeroApuesta.EsValido(numero))
        {
            return Result<NumeroRestringidoResponse>.Fail(ValidationMessages.NumeroApuestaFormato);
        }

        var valor = numero.Trim();
        if (await _db.NumerosRestringidos.AnyAsync(x => x.Numero == valor, cancellationToken))
        {
            return Result<NumeroRestringidoResponse>.Fail(ConfiguracionMessages.NumeroRestringidoDuplicado);
        }

        var item = new NumeroRestringido
        {
            NumeroRestringidoId = Guid.NewGuid(),
            Numero = valor,
            FechaCreacion = _clock.UtcNow
        };
        _db.NumerosRestringidos.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<NumeroRestringidoResponse>.Ok(Map(item), SuccessMessages.RegistroCreado);
    }

    public async Task<Result> EliminarAsync(Guid numeroRestringidoId, CancellationToken cancellationToken)
    {
        var item = await _db.NumerosRestringidos.FirstOrDefaultAsync(x => x.NumeroRestringidoId == numeroRestringidoId, cancellationToken);
        if (item is null)
        {
            return Result.Fail(ConfiguracionMessages.NumeroRestringidoNoEncontrado, 404);
        }

        _db.NumerosRestringidos.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.RegistroEliminado);
    }

    private static NumeroRestringidoResponse Map(NumeroRestringido item) => new()
    {
        NumeroRestringidoId = item.NumeroRestringidoId,
        Numero = item.Numero
    };
}
