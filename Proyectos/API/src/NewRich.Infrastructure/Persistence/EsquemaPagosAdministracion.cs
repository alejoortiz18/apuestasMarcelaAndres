namespace NewRich.Infrastructure.Persistence;

internal static class EsquemaPagosAdministracion
{
    internal const string Tablas = """
        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'recaudo')
            EXEC(N'CREATE SCHEMA recaudo');

        IF OBJECT_ID(N'recaudo.PagosAdministracion', N'U') IS NULL
        BEGIN
            CREATE TABLE recaudo.PagosAdministracion (
                PagoId                 UNIQUEIDENTIFIER NOT NULL,
                GrupoId                UNIQUEIDENTIFIER NOT NULL,
                RecaudadorId           UNIQUEIDENTIFIER NOT NULL,
                Valor                  DECIMAL(18,2)    NOT NULL,
                FechaOperacion         DATE             NOT NULL,
                FechaRegistroUtc       DATETIME2        NOT NULL,
                RegistradoPorUsuarioId UNIQUEIDENTIFIER NULL,
                CONSTRAINT PK_PagosAdministracion PRIMARY KEY (PagoId),
                CONSTRAINT FK_PagosAdministracion_Grupos FOREIGN KEY (GrupoId) REFERENCES dbo.Grupos (GrupoId),
                CONSTRAINT FK_PagosAdministracion_Recaudador FOREIGN KEY (RecaudadorId) REFERENCES dbo.Usuarios (UsuarioId),
                CONSTRAINT CK_PagosAdministracion_Valor CHECK (Valor > 0)
            );
            CREATE INDEX IX_PagosAdministracion_Grupo_Fecha
                ON recaudo.PagosAdministracion (GrupoId, FechaOperacion);
        END;
        """;
}