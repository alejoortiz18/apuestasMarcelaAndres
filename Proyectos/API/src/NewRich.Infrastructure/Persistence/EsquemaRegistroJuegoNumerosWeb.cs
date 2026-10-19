namespace NewRich.Infrastructure.Persistence;

internal static class EsquemaRegistroJuegoNumerosWeb
{
    internal const string Tablas = """
        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'manual')
            EXEC(N'CREATE SCHEMA manual');

        IF OBJECT_ID(N'manual.RegistrosJuegoNumerosWeb', N'U') IS NULL
        BEGIN
            CREATE TABLE manual.RegistrosJuegoNumerosWeb (
                RegistroId      UNIQUEIDENTIFIER NOT NULL,
                UsuarioId       UNIQUEIDENTIFIER NOT NULL,
                NombreCompleto  NVARCHAR(200)    NOT NULL,
                FechaRegistro   DATETIME2        NOT NULL,
                CodigoPublico   CHAR(7)          NOT NULL,
                ValorJugado     DECIMAL(18,2)    NOT NULL,
                CONSTRAINT PK_manual_RegistrosJuegoNumerosWeb PRIMARY KEY (RegistroId),
                CONSTRAINT UQ_manual_RegistrosJuegoNumerosWeb_Codigo UNIQUE (CodigoPublico),
                CONSTRAINT FK_manual_RegistrosJuegoNumerosWeb_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId),
                CONSTRAINT CK_manual_RegistrosJuegoNumerosWeb_Valor CHECK (ValorJugado > 0)
            );
        END

        IF OBJECT_ID(N'manual.RegistroJuegoNumerosWebApuestas', N'U') IS NULL
        BEGIN
            CREATE TABLE manual.RegistroJuegoNumerosWebApuestas (
                ApuestaId    UNIQUEIDENTIFIER NOT NULL,
                RegistroId   UNIQUEIDENTIFIER NOT NULL,
                LoteriaId    UNIQUEIDENTIFIER NOT NULL,
                Numero       NVARCHAR(4)      NOT NULL,
                Valor        DECIMAL(18,2)    NOT NULL,
                CONSTRAINT PK_manual_RegistroJuegoNumerosWebApuestas PRIMARY KEY (ApuestaId),
                CONSTRAINT FK_manual_RegistroJuegoNumerosWebApuestas_Registros FOREIGN KEY (RegistroId) REFERENCES manual.RegistrosJuegoNumerosWeb(RegistroId),
                CONSTRAINT FK_manual_RegistroJuegoNumerosWebApuestas_Loterias FOREIGN KEY (LoteriaId) REFERENCES dbo.Loterias(LoteriaId),
                CONSTRAINT CK_manual_RegistroJuegoNumerosWebApuestas_Valor CHECK (Valor > 0)
            );
        END
        """;

    internal const string Registrar = """
        CREATE OR ALTER PROCEDURE manual.RegistrarJuegoNumerosWeb
            @RegistroId uniqueidentifier,
            @UsuarioId uniqueidentifier,
            @NombreCompleto nvarchar(200),
            @FechaRegistro datetime2,
            @CodigoPublico char(7),
            @ValorJugado decimal(18,2),
            @Apuestas nvarchar(max)
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
            IF EXISTS (SELECT 1 FROM manual.RegistrosJuegoNumerosWeb WHERE RegistroId = @RegistroId)
                RETURN;

            BEGIN TRANSACTION;
            INSERT INTO manual.RegistrosJuegoNumerosWeb
                (RegistroId, UsuarioId, NombreCompleto, FechaRegistro, CodigoPublico, ValorJugado)
            VALUES
                (@RegistroId, @UsuarioId, @NombreCompleto, @FechaRegistro, @CodigoPublico, @ValorJugado);

            INSERT INTO manual.RegistroJuegoNumerosWebApuestas (ApuestaId, RegistroId, LoteriaId, Numero, Valor)
            SELECT ApuestaId, @RegistroId, LoteriaId, Numero, Valor
            FROM OPENJSON(@Apuestas)
            WITH (
                ApuestaId uniqueidentifier '$.apuestaId',
                LoteriaId uniqueidentifier '$.loteriaId',
                Numero nvarchar(4) '$.numero',
                Valor decimal(18,2) '$.valor'
            );
            COMMIT TRANSACTION;
        END
        """;
}
