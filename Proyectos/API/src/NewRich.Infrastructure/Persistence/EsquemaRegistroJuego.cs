namespace NewRich.Infrastructure.Persistence;

internal static class EsquemaRegistroJuego
{
    internal const string Tablas = """
        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'manual')
            EXEC(N'CREATE SCHEMA manual');

        IF OBJECT_ID(N'manual.RegistrosJuego', N'U') IS NULL
        BEGIN
            CREATE TABLE manual.RegistrosJuego (
                RegistroId       UNIQUEIDENTIFIER NOT NULL,
                UsuarioId        UNIQUEIDENTIFIER NOT NULL,
                NombreCompleto   NVARCHAR(200)    NOT NULL,
                FechaRegistro    DATETIME2        NOT NULL,
                CodigoPublico    CHAR(7)          NOT NULL,
                IdempotencyKey   NVARCHAR(100)    NOT NULL,
                CONSTRAINT PK_manual_RegistrosJuego PRIMARY KEY (RegistroId),
                CONSTRAINT UQ_manual_RegistrosJuego_Codigo UNIQUE (CodigoPublico),
                CONSTRAINT UQ_manual_RegistrosJuego_IdempotencyKey UNIQUE (IdempotencyKey),
                CONSTRAINT FK_manual_RegistrosJuego_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId)
            );
        END

        IF OBJECT_ID(N'manual.RegistroJuegoFilas', N'U') IS NULL
        BEGIN
            CREATE TABLE manual.RegistroJuegoFilas (
                FilaId       UNIQUEIDENTIFIER NOT NULL,
                RegistroId   UNIQUEIDENTIFIER NOT NULL,
                LoteriaId    UNIQUEIDENTIFIER NOT NULL,
                Valor        DECIMAL(18,2)    NOT NULL,
                Numero       NVARCHAR(MAX)    NOT NULL,
                CONSTRAINT PK_manual_RegistroJuegoFilas PRIMARY KEY (FilaId),
                CONSTRAINT FK_manual_RegistroJuegoFilas_Registros FOREIGN KEY (RegistroId) REFERENCES manual.RegistrosJuego(RegistroId),
                CONSTRAINT FK_manual_RegistroJuegoFilas_Loterias FOREIGN KEY (LoteriaId) REFERENCES dbo.Loterias(LoteriaId)
            );
        END

        IF COL_LENGTH(N'manual.RegistrosJuego', N'Total') IS NOT NULL
            ALTER TABLE manual.RegistrosJuego DROP COLUMN Total;

        IF COL_LENGTH(N'manual.RegistrosJuego', N'NombreCompleto') IS NULL
            ALTER TABLE manual.RegistrosJuego ADD NombreCompleto NVARCHAR(200) NOT NULL CONSTRAINT DF_manual_RegistrosJuego_NombreCompleto DEFAULT N'';

        EXEC(N'
        UPDATE r
        SET NombreCompleto = u.NombreCompleto
        FROM manual.RegistrosJuego r
        INNER JOIN dbo.Usuarios u ON u.UsuarioId = r.UsuarioId
        WHERE r.NombreCompleto = N'''';
        ');
        """;

    internal const string Registrar = """
        CREATE OR ALTER PROCEDURE manual.RegistrarJuego
            @RegistroId uniqueidentifier,
            @UsuarioId uniqueidentifier,
            @NombreCompleto nvarchar(200),
            @FechaRegistro datetime2,
            @CodigoPublico char(7),
            @IdempotencyKey nvarchar(100),
            @Filas nvarchar(max)
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
            IF EXISTS (SELECT 1 FROM manual.RegistrosJuego WHERE IdempotencyKey = @IdempotencyKey)
                RETURN;

            BEGIN TRANSACTION;
            INSERT INTO manual.RegistrosJuego (RegistroId, UsuarioId, NombreCompleto, FechaRegistro, CodigoPublico, IdempotencyKey)
            VALUES (@RegistroId, @UsuarioId, @NombreCompleto, @FechaRegistro, @CodigoPublico, @IdempotencyKey);

            INSERT INTO manual.RegistroJuegoFilas (FilaId, RegistroId, LoteriaId, Valor, Numero)
            SELECT FilaId, @RegistroId, LoteriaId, Valor, Numero
            FROM OPENJSON(@Filas)
            WITH (
                FilaId uniqueidentifier '$.filaId',
                LoteriaId uniqueidentifier '$.loteriaId',
                Valor decimal(18,2) '$.valor',
                Numero nvarchar(max) '$.numero'
            );
            COMMIT TRANSACTION;
        END
        """;

    internal const string Consultar = """
        CREATE OR ALTER PROCEDURE manual.ConsultarRegistrosJuego
            @UsuarioId uniqueidentifier,
            @Desde datetime2,
            @Hasta datetime2
        AS
        BEGIN
            SET NOCOUNT ON;
            SELECT
                r.FechaRegistro,
                r.CodigoPublico,
                f.Numero,
                f.Valor,
                l.Nombre AS Loteria
            FROM manual.RegistrosJuego r
            INNER JOIN manual.RegistroJuegoFilas f ON f.RegistroId = r.RegistroId
            INNER JOIN dbo.Loterias l ON l.LoteriaId = f.LoteriaId
            WHERE r.UsuarioId = @UsuarioId
              AND r.FechaRegistro >= @Desde
              AND r.FechaRegistro <= @Hasta;
        END
        """;
}
