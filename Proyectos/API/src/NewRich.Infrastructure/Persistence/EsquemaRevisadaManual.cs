namespace NewRich.Infrastructure.Persistence;

internal static class EsquemaRevisadaManual
{
    internal const string Tablas = """
        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'manual')
            EXEC(N'CREATE SCHEMA manual');

        IF OBJECT_ID(N'manual.RevisadaManualOperaciones', N'U') IS NULL
        BEGIN
            CREATE TABLE manual.RevisadaManualOperaciones (
                OperacionId           UNIQUEIDENTIFIER NOT NULL,
                Estado                NVARCHAR(20)     NOT NULL,
                LoteriaId             UNIQUEIDENTIFIER NOT NULL,
                Numero                NVARCHAR(MAX)    NOT NULL,
                UsuarioId             UNIQUEIDENTIFIER NOT NULL,
                FechaOperacion        DATETIME2        NOT NULL,
                Subtotal              DECIMAL(18,2)    NOT NULL,
                ComisionPorcentaje    DECIMAL(18,2)    NULL,
                ComisionValor         DECIMAL(18,2)    NULL,
                TotalConComision      DECIMAL(18,2)    NULL,
                DescuentoPorcentaje   DECIMAL(18,2)    NULL,
                DescuentoValor        DECIMAL(18,2)    NULL,
                TotalPagar            DECIMAL(18,2)    NULL,
                CONSTRAINT PK_manual_RevisadaManualOperaciones PRIMARY KEY (OperacionId),
                CONSTRAINT FK_manual_RevisadaManualOperaciones_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId),
                CONSTRAINT FK_manual_RevisadaManualOperaciones_Loterias FOREIGN KEY (LoteriaId) REFERENCES dbo.Loterias(LoteriaId)
            );
        END

        IF OBJECT_ID(N'manual.RevisadaManualLineas', N'U') IS NULL
        BEGIN
            CREATE TABLE manual.RevisadaManualLineas (
                LineaId         UNIQUEIDENTIFIER NOT NULL,
                OperacionId     UNIQUEIDENTIFIER NOT NULL,
                Origen          NVARCHAR(20)     NOT NULL,
                OrigenId        UNIQUEIDENTIFIER NOT NULL,
                Valor           DECIMAL(18,2)    NOT NULL,
                FechaRegistro   DATETIME2        NOT NULL,
                CONSTRAINT PK_manual_RevisadaManualLineas PRIMARY KEY (LineaId),
                CONSTRAINT UQ_manual_RevisadaManualLineas_Origen UNIQUE (Origen, OrigenId),
                CONSTRAINT FK_manual_RevisadaManualLineas_Operaciones FOREIGN KEY (OperacionId) REFERENCES manual.RevisadaManualOperaciones(OperacionId)
            );
        END
        """;
}
