namespace NewRich.Infrastructure.Persistence;

internal static class EsquemaComisionDia
{
    internal const string Tablas = """
        IF OBJECT_ID(N'dbo.UsuarioComisionDia', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.UsuarioComisionDia (
                UsuarioId   UNIQUEIDENTIFIER NOT NULL,
                DiaSemana   TINYINT          NOT NULL,
                Porcentaje  TINYINT          NOT NULL,
                CONSTRAINT PK_UsuarioComisionDia PRIMARY KEY (UsuarioId, DiaSemana),
                CONSTRAINT FK_UsuarioComisionDia_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios (UsuarioId),
                CONSTRAINT CK_UsuarioComisionDia_Dia CHECK (DiaSemana BETWEEN 1 AND 7),
                CONSTRAINT CK_UsuarioComisionDia_Porcentaje CHECK (Porcentaje BETWEEN 0 AND 100)
            );
        END;

        IF OBJECT_ID(N'recaudo.Obligaciones', N'U') IS NOT NULL AND COL_LENGTH(N'recaudo.Obligaciones', N'PorcentajeRecaudador') IS NULL
            ALTER TABLE recaudo.Obligaciones ADD
                PorcentajeRecaudador   INT            NULL,
                ComisionRecaudador     DECIMAL(18,2)  NULL,
                EntregarAdministracion DECIMAL(18,2)  NULL;

        INSERT INTO dbo.UsuarioComisionDia (UsuarioId, DiaSemana, Porcentaje)
        SELECT u.UsuarioId, d.Dia, 1
        FROM dbo.Usuarios u
        CROSS JOIN (VALUES (1),(2),(3),(4),(5),(6),(7)) d(Dia)
        WHERE u.Rol IN (N'Vendedor', N'Recaudador')
          AND NOT EXISTS (
              SELECT 1 FROM dbo.UsuarioComisionDia c WHERE c.UsuarioId = u.UsuarioId AND c.DiaSemana = d.Dia
          );
        """;
}
