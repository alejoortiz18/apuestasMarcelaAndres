namespace NewRich.Infrastructure.Persistence;

internal static class EsquemaInformes
{
    internal const string Tablas = """
        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'informes')
            EXEC(N'CREATE SCHEMA informes');

        IF OBJECT_ID(N'informes.ConfiguracionesInforme', N'U') IS NULL
        BEGIN
            CREATE TABLE informes.ConfiguracionesInforme (
                ConfiguracionId        UNIQUEIDENTIFIER NOT NULL,
                UsuarioId              UNIQUEIDENTIFIER NOT NULL,
                Categoria              NVARCHAR(40)     NOT NULL,
                Filtros                NVARCHAR(MAX)    NOT NULL,
                FechaActualizacionUtc  DATETIME2        NOT NULL,
                CONSTRAINT PK_informes_ConfiguracionesInforme PRIMARY KEY (ConfiguracionId)
            );

            CREATE UNIQUE INDEX UX_informes_ConfiguracionesInforme_Usuario_Categoria
                ON informes.ConfiguracionesInforme (UsuarioId, Categoria);
        END
        """;
}
