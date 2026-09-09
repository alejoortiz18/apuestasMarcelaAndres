using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Loterias;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class LoteriaService : ILoteriaService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;

    public LoteriaService(INewRichDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<LoteriaResponse>>> ListarAsync(CancellationToken cancellationToken)
    {
        var items = await _db.Loterias.OrderBy(x => x.Nombre).ToListAsync(cancellationToken);
        return Result<IReadOnlyList<LoteriaResponse>>.Ok(items.Select(Map).ToList(), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<LoteriaResponse>> CrearAsync(CrearLoteriaRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return Result<LoteriaResponse>.Fail(ValidationMessages.CampoRequerido);
        }

        if (await _db.Loterias.AnyAsync(x => x.Nombre == request.Nombre.Trim(), cancellationToken))
        {
            return Result<LoteriaResponse>.Fail(VentaMessages.LoteriaNombreDuplicado, 409);
        }

        var loteria = new Loteria
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = request.Nombre.Trim(),
            Estado = EstadoGeneral.Activo,
            FechaCreacion = _clock.UtcNow
        };
        _db.Loterias.Add(loteria);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<LoteriaResponse>.Created(Map(loteria), SuccessMessages.RegistroCreado);
    }

    public async Task<Result<LoteriaResponse>> ActualizarAsync(Guid loteriaId, ActualizarLoteriaRequest request, CancellationToken cancellationToken)
    {
        var loteria = await _db.Loterias.FirstOrDefaultAsync(x => x.LoteriaId == loteriaId, cancellationToken);
        if (loteria is null)
        {
            return Result<LoteriaResponse>.Fail(VentaMessages.LoteriaNoEncontrada, 404);
        }

        if (await _db.Loterias.AnyAsync(x => x.Nombre == request.Nombre.Trim() && x.LoteriaId != loteriaId, cancellationToken))
        {
            return Result<LoteriaResponse>.Fail(VentaMessages.LoteriaNombreDuplicado, 409);
        }

        loteria.Nombre = request.Nombre.Trim();
        loteria.Estado = request.Estado;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<LoteriaResponse>.Ok(Map(loteria), SuccessMessages.RegistroActualizado);
    }

    private static LoteriaResponse Map(Loteria loteria) => new()
    {
        LoteriaId = loteria.LoteriaId,
        Nombre = loteria.Nombre,
        Estado = loteria.Estado
    };
}
