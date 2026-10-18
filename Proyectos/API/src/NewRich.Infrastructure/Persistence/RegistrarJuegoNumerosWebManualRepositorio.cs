using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Domain.Entities;

namespace NewRich.Infrastructure.Persistence;

public sealed class RegistrarJuegoNumerosWebManualRepositorio : IRegistrarJuegoNumerosWebManualRepositorio
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly NewRichDbContext _db;

    public RegistrarJuegoNumerosWebManualRepositorio(NewRichDbContext db) => _db = db;

    public Task<RegistroJuegoNumerosWebManual?> BuscarAsync(Guid registroId, CancellationToken cancellationToken) =>
        _db.RegistrosJuegoNumerosWebManual
            .Include(r => r.Apuestas)
            .FirstOrDefaultAsync(r => r.RegistroId == registroId, cancellationToken);

    public Task<bool> ExisteCodigoAsync(string codigoPublico, CancellationToken cancellationToken) =>
        _db.RegistrosJuegoNumerosWebManual.AnyAsync(r => r.CodigoPublico == codigoPublico, cancellationToken);

    public async Task GuardarAsync(RegistroJuegoNumerosWebManual registro, CancellationToken cancellationToken)
    {
        if (!_db.Database.IsRelational())
        {
            _db.RegistrosJuegoNumerosWebManual.Add(registro);
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        var apuestas = JsonSerializer.Serialize(registro.Apuestas.Select(a => new
        {
            apuestaId = a.ApuestaId,
            loteriaId = a.LoteriaId,
            numero = a.Numero,
            valor = a.Valor
        }), Json);

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
EXEC manual.RegistrarJuegoNumerosWeb
    @RegistroId={registro.RegistroId},
    @UsuarioId={registro.UsuarioId},
    @NombreCompleto={registro.NombreCompleto},
    @FechaRegistro={registro.FechaRegistro},
    @CodigoPublico={registro.CodigoPublico},
    @ValorJugado={registro.ValorJugado},
    @Apuestas={apuestas}", cancellationToken);
    }
}
