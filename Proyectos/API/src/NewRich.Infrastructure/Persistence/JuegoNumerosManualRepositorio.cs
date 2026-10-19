using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;

namespace NewRich.Infrastructure.Persistence;

/// <summary>Hora de Colombia y marca de «Juego números» en el esquema manual.</summary>
public sealed class JuegoNumerosManualRepositorio : IJuegoNumerosManualRepositorio
{
    private readonly NewRichDbContext _db;

    public JuegoNumerosManualRepositorio(NewRichDbContext db) => _db = db;

    public async Task<DateTime> FechaHoraColombiaAsync(CancellationToken cancellationToken) =>
        await _db.Database
            .SqlQueryRaw<DateTime>("SELECT CONVERT(datetime2(0), SYSDATETIMEOFFSET() AT TIME ZONE 'SA Pacific Standard Time') AS [Value]")
            .SingleAsync(cancellationToken);

    public async Task MarcarAsync(MarcaJuegoNumerosManual marca, CancellationToken cancellationToken)
    {
        await _db.Database.ExecuteSqlRawAsync(
            "EXEC manual.JuegoNumerosMarcar @VentaId, @ValorJugado, @CantidadNumeros, @CantidadLoterias, @CantidadApuestas, @FechaRegistro",
            [
                new SqlParameter("@VentaId", SqlDbType.UniqueIdentifier) { Value = marca.VentaId },
                new SqlParameter("@ValorJugado", SqlDbType.Decimal) { Value = marca.ValorJugado, Precision = 18, Scale = 2 },
                new SqlParameter("@CantidadNumeros", SqlDbType.Int) { Value = marca.CantidadNumeros },
                new SqlParameter("@CantidadLoterias", SqlDbType.Int) { Value = marca.CantidadLoterias },
                new SqlParameter("@CantidadApuestas", SqlDbType.Int) { Value = marca.CantidadApuestas },
                new SqlParameter("@FechaRegistro", SqlDbType.DateTime2) { Value = marca.FechaRegistro, Scale = 0 }
            ],
            cancellationToken);
    }
}
