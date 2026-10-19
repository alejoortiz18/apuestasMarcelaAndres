using Microsoft.EntityFrameworkCore;

namespace NewRich.Infrastructure.Persistence;

/// <summary>
/// Marca de «Juego números» en el esquema manual. La venta, el boleto y los juegos van en las tablas
/// dbo de siempre; aquí solo se indica qué ventas son «Juego números». Las tablas dbo nunca se alteran.
/// Las fechas se guardan en hora de Colombia.
/// </summary>
public static class EsquemaJuegoNumerosManual
{
    public static async Task AsegurarAsync(NewRichDbContext db, CancellationToken cancellationToken)
    {
        if (!db.Database.IsRelational())
        {
            return;
        }

        await db.Database.ExecuteSqlRawAsync(Tabla, cancellationToken);
        await db.Database.ExecuteSqlRawAsync(Marcar, cancellationToken);
    }

    private const string Tabla = """
        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'manual')
            EXEC(N'CREATE SCHEMA manual');

        IF OBJECT_ID(N'manual.VentasJuegoNumeros', N'U') IS NULL
        BEGIN
            CREATE TABLE manual.VentasJuegoNumeros (
                VentaId           UNIQUEIDENTIFIER NOT NULL,
                ValorJugado       DECIMAL(18,2)    NOT NULL,
                CantidadNumeros   INT              NOT NULL,
                CantidadLoterias  INT              NOT NULL,
                CantidadApuestas  INT              NOT NULL,
                FechaRegistro     DATETIME2(0)     NOT NULL
                    CONSTRAINT DF_manual_VentasJuegoNumeros_FechaRegistro
                    DEFAULT (CONVERT(DATETIME2(0), SYSDATETIMEOFFSET() AT TIME ZONE 'SA Pacific Standard Time')),
                CONSTRAINT PK_manual_VentasJuegoNumeros PRIMARY KEY (VentaId),
                CONSTRAINT FK_manual_VentasJuegoNumeros_Ventas FOREIGN KEY (VentaId) REFERENCES dbo.Ventas(VentaId),
                CONSTRAINT CK_manual_VentasJuegoNumeros_Valor CHECK (ValorJugado > 0),
                CONSTRAINT CK_manual_VentasJuegoNumeros_Cantidades CHECK (CantidadApuestas = CantidadNumeros * CantidadLoterias)
            );
        END
        """;

    private const string Marcar = """
        CREATE OR ALTER PROCEDURE manual.JuegoNumerosMarcar
            @VentaId uniqueidentifier,
            @ValorJugado decimal(18,2),
            @CantidadNumeros int,
            @CantidadLoterias int,
            @CantidadApuestas int,
            @FechaRegistro datetime2(0)
        AS
        BEGIN
            SET NOCOUNT ON;
            IF EXISTS (SELECT 1 FROM manual.VentasJuegoNumeros WHERE VentaId = @VentaId)
                RETURN;

            INSERT INTO manual.VentasJuegoNumeros
                (VentaId, ValorJugado, CantidadNumeros, CantidadLoterias, CantidadApuestas, FechaRegistro)
            VALUES
                (@VentaId, @ValorJugado, @CantidadNumeros, @CantidadLoterias, @CantidadApuestas, @FechaRegistro);
        END
        """;
}
