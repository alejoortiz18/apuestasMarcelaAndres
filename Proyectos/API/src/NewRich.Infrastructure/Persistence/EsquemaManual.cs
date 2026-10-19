namespace NewRich.Infrastructure.Persistence;

internal static class EsquemaManual
{
    internal const string Tablas = """
        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'manual')
            EXEC(N'CREATE SCHEMA manual');

        IF OBJECT_ID(N'manual.Ventas', N'U') IS NULL
        BEGIN
            CREATE TABLE manual.Ventas (
                VentaId         UNIQUEIDENTIFIER NOT NULL,
                UsuarioId       UNIQUEIDENTIFIER NOT NULL,
                FechaVenta      DATETIME2        NOT NULL,
                Total           DECIMAL(18,2)    NOT NULL,
                TipoApuesta     NVARCHAR(20)     NOT NULL,
                IdempotencyKey  NVARCHAR(100)    NOT NULL,
                CONSTRAINT PK_manual_Ventas PRIMARY KEY (VentaId),
                CONSTRAINT UQ_manual_Ventas_IdempotencyKey UNIQUE (IdempotencyKey),
                CONSTRAINT FK_manual_Ventas_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId),
                CONSTRAINT CK_manual_Ventas_TipoApuesta CHECK (TipoApuesta IN ('COMBINADO', 'INDIVIDUAL'))
            );
        END

        IF OBJECT_ID(N'manual.Boletos', N'U') IS NULL
        BEGIN
            CREATE TABLE manual.Boletos (
                BoletoId        UNIQUEIDENTIFIER NOT NULL,
                VentaId         UNIQUEIDENTIFIER NOT NULL,
                CodigoPublico   CHAR(7)          NOT NULL,
                FechaCreacion   DATETIME2        NOT NULL,
                VigenciaDias    INT              NOT NULL,
                EstadoBoleto    NVARCHAR(30)     NOT NULL,
                CONSTRAINT PK_manual_Boletos PRIMARY KEY (BoletoId),
                CONSTRAINT UQ_manual_Boletos_CodigoPublico UNIQUE (CodigoPublico),
                CONSTRAINT FK_manual_Boletos_Ventas FOREIGN KEY (VentaId) REFERENCES manual.Ventas(VentaId)
            );
        END

        IF OBJECT_ID(N'manual.Juegos', N'U') IS NULL
        BEGIN
            CREATE TABLE manual.Juegos (
                JuegoId     UNIQUEIDENTIFIER NOT NULL,
                BoletoId    UNIQUEIDENTIFIER NOT NULL,
                Numero      CHAR(4)          NOT NULL,
                Valor       DECIMAL(18,2)    NOT NULL,
                TipoJuego   NVARCHAR(20)     NOT NULL,
                CONSTRAINT PK_manual_Juegos PRIMARY KEY (JuegoId),
                CONSTRAINT FK_manual_Juegos_Boletos FOREIGN KEY (BoletoId) REFERENCES manual.Boletos(BoletoId),
                CONSTRAINT CK_manual_Juegos_TipoJuego CHECK (TipoJuego IN ('COMBINADA', 'INDIVIDUAL'))
            );
        END

        IF OBJECT_ID(N'manual.JuegoLoterias', N'U') IS NULL
        BEGIN
            CREATE TABLE manual.JuegoLoterias (
                JuegoId     UNIQUEIDENTIFIER NOT NULL,
                LoteriaId   UNIQUEIDENTIFIER NOT NULL,
                CONSTRAINT PK_manual_JuegoLoterias PRIMARY KEY (JuegoId, LoteriaId),
                CONSTRAINT FK_manual_JuegoLoterias_Juegos FOREIGN KEY (JuegoId) REFERENCES manual.Juegos(JuegoId),
                CONSTRAINT FK_manual_JuegoLoterias_Loterias FOREIGN KEY (LoteriaId) REFERENCES dbo.Loterias(LoteriaId)
            );
        END
        """;

    internal const string ConfirmarVenta = """
        CREATE OR ALTER PROCEDURE manual.ConfirmarVenta
            @VentaId uniqueidentifier,
            @UsuarioId uniqueidentifier,
            @FechaVenta datetime2,
            @Total decimal(18,2),
            @TipoApuesta nvarchar(20),
            @IdempotencyKey nvarchar(100),
            @BoletoId uniqueidentifier,
            @CodigoPublico char(7),
            @VigenciaDias int,
            @FechaCreacion datetime2,
            @Juegos nvarchar(max)
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
            IF EXISTS (SELECT 1 FROM manual.Ventas WHERE IdempotencyKey = @IdempotencyKey)
                RETURN;

            BEGIN TRANSACTION;
            INSERT INTO manual.Ventas (VentaId, UsuarioId, FechaVenta, Total, TipoApuesta, IdempotencyKey)
            VALUES (@VentaId, @UsuarioId, @FechaVenta, @Total, @TipoApuesta, @IdempotencyKey);

            INSERT INTO manual.Boletos (BoletoId, VentaId, CodigoPublico, FechaCreacion, VigenciaDias, EstadoBoleto)
            VALUES (@BoletoId, @VentaId, @CodigoPublico, @FechaCreacion, @VigenciaDias, N'Jugado');

            INSERT INTO manual.Juegos (JuegoId, BoletoId, Numero, Valor, TipoJuego)
            SELECT JuegoId, @BoletoId, Numero, Valor, TipoJuego
            FROM OPENJSON(@Juegos)
            WITH (
                JuegoId uniqueidentifier '$.juegoId',
                Numero char(4) '$.numero',
                Valor decimal(18,2) '$.valor',
                TipoJuego nvarchar(20) '$.tipoJuego'
            );

            INSERT INTO manual.JuegoLoterias (JuegoId, LoteriaId)
            SELECT j.JuegoId, TRY_CONVERT(uniqueidentifier, l.value)
            FROM OPENJSON(@Juegos)
            WITH (
                JuegoId uniqueidentifier '$.juegoId',
                Loterias nvarchar(max) '$.loterias' AS JSON
            ) j
            CROSS APPLY OPENJSON(j.Loterias) l;
            COMMIT TRANSACTION;
        END
        """;

    internal const string ConsultarVentas = """
        CREATE OR ALTER PROCEDURE manual.ConsultarVentas
            @UsuarioId uniqueidentifier,
            @Desde datetime2,
            @Hasta datetime2
        AS
        BEGIN
            SET NOCOUNT ON;
            SELECT
                v.VentaId,
                v.UsuarioId,
                v.FechaVenta,
                v.Total,
                v.TipoApuesta,
                v.IdempotencyKey,
                b.BoletoId,
                b.CodigoPublico,
                b.VigenciaDias,
                j.JuegoId,
                j.Numero,
                j.Valor,
                j.TipoJuego,
                jl.LoteriaId,
                lot.Nombre AS LoteriaNombre
            FROM manual.Ventas v
            INNER JOIN manual.Boletos b ON b.VentaId = v.VentaId
            INNER JOIN manual.Juegos j ON j.BoletoId = b.BoletoId
            INNER JOIN manual.JuegoLoterias jl ON jl.JuegoId = j.JuegoId
            INNER JOIN dbo.Loterias lot ON lot.LoteriaId = jl.LoteriaId
            WHERE v.UsuarioId = @UsuarioId
              AND v.FechaVenta >= @Desde
              AND v.FechaVenta <= @Hasta
            ORDER BY v.FechaVenta DESC, j.Numero;
        END
        """;

    internal const string ConsultarPorCodigo = """
        CREATE OR ALTER PROCEDURE manual.ConsultarPorCodigo
            @UsuarioId uniqueidentifier,
            @Codigo char(7)
        AS
        BEGIN
            SET NOCOUNT ON;
            SELECT
                v.VentaId,
                v.UsuarioId,
                v.FechaVenta,
                v.Total,
                v.TipoApuesta,
                v.IdempotencyKey,
                b.BoletoId,
                b.CodigoPublico,
                b.VigenciaDias,
                j.JuegoId,
                j.Numero,
                j.Valor,
                j.TipoJuego,
                jl.LoteriaId,
                lot.Nombre AS LoteriaNombre
            FROM manual.Ventas v
            INNER JOIN manual.Boletos b ON b.VentaId = v.VentaId
            INNER JOIN manual.Juegos j ON j.BoletoId = b.BoletoId
            INNER JOIN manual.JuegoLoterias jl ON jl.JuegoId = j.JuegoId
            INNER JOIN dbo.Loterias lot ON lot.LoteriaId = jl.LoteriaId
            WHERE v.UsuarioId = @UsuarioId
              AND b.CodigoPublico = @Codigo;
        END
        """;
}
