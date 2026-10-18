using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Domain.Entities;

namespace NewRich.Infrastructure.Persistence;

public sealed class RegistrarJuegoManualRepositorio : IRegistrarJuegoManualRepositorio
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly NewRichDbContext _db;

    public RegistrarJuegoManualRepositorio(NewRichDbContext db) => _db = db;

    public Task<RegistroJuegoManual?> BuscarPorIdempotenciaAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        _db.RegistrosJuegoManual
            .Include(r => r.Filas)
            .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey, cancellationToken);

    public Task<bool> ExisteCodigoAsync(string codigoPublico, CancellationToken cancellationToken) =>
        _db.RegistrosJuegoManual.AnyAsync(r => r.CodigoPublico == codigoPublico, cancellationToken);

    public async Task GuardarAsync(RegistroJuegoManual registro, CancellationToken cancellationToken)
    {
        if (!_db.Database.IsRelational())
        {
            _db.RegistrosJuegoManual.Add(registro);
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        var filas = JsonSerializer.Serialize(registro.Filas.Select(f => new
        {
            filaId = f.FilaId,
            loteriaId = f.LoteriaId,
            valor = f.Valor,
            numero = f.Numero
        }), Json);

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
EXEC manual.RegistrarJuego
    @RegistroId={registro.RegistroId},
    @UsuarioId={registro.UsuarioId},
    @NombreCompleto={registro.NombreCompleto},
    @FechaRegistro={registro.FechaRegistro},
    @CodigoPublico={registro.CodigoPublico},
    @IdempotencyKey={registro.IdempotencyKey},
    @Filas={filas}", cancellationToken);
    }

    public async Task<IReadOnlyList<FilaHistoricoJuegoManual>> ConsultarAsync(Guid usuarioId, DateTime desde, DateTime hasta, CancellationToken cancellationToken)
    {
        if (!_db.Database.IsRelational())
        {
            var registros = await _db.RegistrosJuegoManual
                .Include(r => r.Filas)
                .Where(r => r.UsuarioId == usuarioId && r.FechaRegistro >= desde && r.FechaRegistro <= hasta)
                .ToListAsync(cancellationToken);
            var ids = registros.SelectMany(r => r.Filas.Select(f => f.LoteriaId)).Distinct().ToList();
            var nombres = await _db.Loterias
                .Where(l => ids.Contains(l.LoteriaId))
                .ToDictionaryAsync(l => l.LoteriaId, l => l.Nombre, cancellationToken);
            return registros.SelectMany(r => r.Filas.Select(f => new FilaHistoricoJuegoManual
            {
                FechaRegistro = r.FechaRegistro,
                CodigoPublico = r.CodigoPublico,
                Numero = f.Numero,
                Valor = f.Valor,
                Loteria = nombres.GetValueOrDefault(f.LoteriaId) ?? string.Empty
            })).ToList();
        }

        return await _db.Database.SqlQuery<FilaHistoricoJuegoManual>(
            $"EXEC manual.ConsultarRegistrosJuego @UsuarioId={usuarioId}, @Desde={desde}, @Hasta={hasta}")
            .ToListAsync(cancellationToken);
    }
}
