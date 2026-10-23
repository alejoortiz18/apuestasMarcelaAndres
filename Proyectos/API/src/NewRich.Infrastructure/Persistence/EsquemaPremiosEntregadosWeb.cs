namespace NewRich.Infrastructure.Persistence;

internal static class EsquemaPremiosEntregadosWeb
{
    internal const string Tablas = """
        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'manual')
            EXEC(N'CREATE SCHEMA manual');

        IF OBJECT_ID(N'manual.PremiosEntregadosWeb', N'U') IS NULL
        BEGIN
            CREATE TABLE manual.PremiosEntregadosWeb (
                PremioEntregadoWebId UNIQUEIDENTIFIER NOT NULL,
                VendedorId           UNIQUEIDENTIFIER NOT NULL,
                LoteriaId            UNIQUEIDENTIFIER NOT NULL,
                Numero               NVARCHAR(4)      NOT NULL,
                ValorApostado        DECIMAL(18,2)    NOT NULL,
                ValorPremio          DECIMAL(18,2)    NOT NULL,
                FechaPremio          DATE             NOT NULL,
                FechaRegistro        DATETIME2        NOT NULL,
                CONSTRAINT PK_manual_PremiosEntregadosWeb PRIMARY KEY (PremioEntregadoWebId),
                CONSTRAINT FK_manual_PremiosEntregadosWeb_Usuarios FOREIGN KEY (VendedorId) REFERENCES dbo.Usuarios(UsuarioId),
                CONSTRAINT FK_manual_PremiosEntregadosWeb_Loterias FOREIGN KEY (LoteriaId) REFERENCES dbo.Loterias(LoteriaId),
                CONSTRAINT CK_manual_PremiosEntregadosWeb_Valores CHECK (ValorApostado > 0 AND ValorPremio > 0)
            );

            CREATE INDEX IX_manual_PremiosEntregadosWeb_Fecha
                ON manual.PremiosEntregadosWeb (FechaPremio) INCLUDE (VendedorId, ValorPremio);

            CREATE INDEX IX_manual_PremiosEntregadosWeb_Vendedor
                ON manual.PremiosEntregadosWeb (VendedorId, FechaRegistro DESC);
        END
        """;
}
