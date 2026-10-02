using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Jornadas;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class JornadaService : IJornadaService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;

    public JornadaService(INewRichDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<JornadaResponse>>> ListarAsync(CancellationToken cancellationToken)
    {
        await _db.AsegurarEsquemaJornadasAsync(cancellationToken);
        var items = await _db.Jornadas
            .Include(j => j.Loterias)
            .OrderBy(j => j.Nombre)
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyList<JornadaResponse>>.Ok(items.Select(Map).ToList(), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<JornadaResponse>> CrearAsync(CrearJornadaRequest request, CancellationToken cancellationToken)
    {
        await _db.AsegurarEsquemaJornadasAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return Result<JornadaResponse>.Fail(ValidationMessages.CampoRequerido);
        }

        var nombre = request.Nombre.Trim();
        if (await _db.Jornadas.AnyAsync(j => j.Nombre == nombre, cancellationToken))
        {
            return Result<JornadaResponse>.Fail(UsuarioMessages.JornadaNombreDuplicado, 409);
        }

        var jornada = new Jornada
        {
            JornadaId = Guid.NewGuid(),
            Nombre = nombre,
            FechaCreacion = _clock.LocalNow
        };
        _db.Jornadas.Add(jornada);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<JornadaResponse>.Created(Map(jornada), SuccessMessages.RegistroCreado);
    }

    public async Task<Result<JornadaResponse>> ActualizarAsync(Guid jornadaId, ActualizarJornadaRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return Result<JornadaResponse>.Fail(ValidationMessages.CampoRequerido);
        }

        var jornada = await _db.Jornadas.Include(j => j.Loterias).FirstOrDefaultAsync(j => j.JornadaId == jornadaId, cancellationToken);
        if (jornada is null)
        {
            return Result<JornadaResponse>.Fail(UsuarioMessages.JornadaNoEncontrada, 404);
        }

        var nombre = request.Nombre.Trim();
        if (await _db.Jornadas.AnyAsync(j => j.Nombre == nombre && j.JornadaId != jornadaId, cancellationToken))
        {
            return Result<JornadaResponse>.Fail(UsuarioMessages.JornadaNombreDuplicado, 409);
        }

        jornada.Nombre = nombre;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<JornadaResponse>.Ok(Map(jornada), SuccessMessages.RegistroActualizado);
    }

    public async Task<Result> EliminarAsync(Guid jornadaId, CancellationToken cancellationToken)
    {
        var jornada = await _db.Jornadas.Include(j => j.Loterias).FirstOrDefaultAsync(j => j.JornadaId == jornadaId, cancellationToken);
        if (jornada is null)
        {
            return Result.Fail(UsuarioMessages.JornadaNoEncontrada, 404);
        }

        if (jornada.Loterias.Count > 0)
        {
            return Result.Fail(UsuarioMessages.JornadaConLoteriasAsociadas);
        }

        _db.Jornadas.Remove(jornada);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.RegistroEliminado);
    }

    private static JornadaResponse Map(Jornada jornada) => new()
    {
        JornadaId = jornada.JornadaId,
        Nombre = jornada.Nombre,
        CantidadLoterias = jornada.Loterias?.Count ?? 0
    };
}
