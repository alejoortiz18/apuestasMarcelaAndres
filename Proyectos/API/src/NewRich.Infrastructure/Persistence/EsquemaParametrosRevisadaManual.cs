namespace NewRich.Infrastructure.Persistence;

internal static class EsquemaParametrosRevisadaManual
{
    internal const string Tablas = """
        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'manual')
            EXEC(N'CREATE SCHEMA manual');

        IF OBJECT_ID(N'manual.ParametrosRevisadaManual', N'U') IS NULL
        BEGIN
            CREATE TABLE manual.ParametrosRevisadaManual (
                ParametroId           UNIQUEIDENTIFIER NOT NULL,
                ComisionPorcentaje    DECIMAL(5,2)     NULL,
                DescuentoPorcentaje   DECIMAL(5,2)     NULL,
                CONSTRAINT PK_manual_ParametrosRevisadaManual PRIMARY KEY (ParametroId)
            );
        END
        """;
}
