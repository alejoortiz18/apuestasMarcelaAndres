using NewRich.Domain.Services;

namespace NewRich.Infrastructure.Persistence;

internal static class EsquemaUtilidades
{
    internal static readonly string Tablas = """
        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'utilidades')
            EXEC(N'CREATE SCHEMA utilidades');

        IF OBJECT_ID(N'utilidades.TiposGasto', N'U') IS NULL
        BEGIN
            CREATE TABLE utilidades.TiposGasto (
                TipoGastoId             UNIQUEIDENTIFIER NOT NULL,
                Nombre                  NVARCHAR(80)     NOT NULL,
                Descripcion             NVARCHAR(300)    NULL,
                Activo                  BIT              NOT NULL CONSTRAINT DF_utilidades_TiposGasto_Activo DEFAULT 1,
                RequiereObservacion     BIT              NOT NULL CONSTRAINT DF_utilidades_TiposGasto_Observacion DEFAULT 1,
                FechaCreacion           DATETIME2        NOT NULL,
                UsuarioCreacionId       UNIQUEIDENTIFIER NOT NULL,
                FechaActualizacion      DATETIME2        NULL,
                UsuarioActualizacionId  UNIQUEIDENTIFIER NULL,
                CONSTRAINT PK_utilidades_TiposGasto PRIMARY KEY (TipoGastoId)
            );
            CREATE UNIQUE INDEX UQ_utilidades_TiposGasto_Nombre ON utilidades.TiposGasto (Nombre);
        END

        IF OBJECT_ID(N'utilidades.Gastos', N'U') IS NULL
        BEGIN
            CREATE TABLE utilidades.Gastos (
                GastoId                 UNIQUEIDENTIFIER NOT NULL,
                TipoGastoId             UNIQUEIDENTIFIER NOT NULL,
                FechaGasto              DATE             NOT NULL,
                Valor                   DECIMAL(18,2)    NOT NULL,
                Observacion             NVARCHAR(500)    NULL,
                UsuarioRegistroId       UNIQUEIDENTIFIER NOT NULL,
                Estado                  NVARCHAR(20)     NOT NULL,
                ClaveIdempotencia       NVARCHAR(100)    NOT NULL,
                FechaCreacion           DATETIME2        NOT NULL,
                FechaActualizacion      DATETIME2        NULL,
                UsuarioActualizacionId  UNIQUEIDENTIFIER NULL,
                CONSTRAINT PK_utilidades_Gastos PRIMARY KEY (GastoId),
                CONSTRAINT FK_utilidades_Gastos_TiposGasto FOREIGN KEY (TipoGastoId) REFERENCES utilidades.TiposGasto (TipoGastoId),
                CONSTRAINT CK_utilidades_Gastos_Valor CHECK (Valor > 0),
                CONSTRAINT CK_utilidades_Gastos_Estado CHECK (Estado IN (N'Activo', N'Anulado'))
            );
            CREATE UNIQUE INDEX UQ_utilidades_Gastos_Clave ON utilidades.Gastos (ClaveIdempotencia);
            CREATE INDEX IX_utilidades_Gastos_Consulta ON utilidades.Gastos (UsuarioRegistroId, FechaGasto, Estado);
        END

        IF OBJECT_ID(N'utilidades.AuditoriaCambios', N'U') IS NULL
        BEGIN
            CREATE TABLE utilidades.AuditoriaCambios (
                AuditoriaCambioGastoId  UNIQUEIDENTIFIER NOT NULL,
                GastoId                 UNIQUEIDENTIFIER NOT NULL,
                Accion                  NVARCHAR(20)     NOT NULL,
                ValorAnterior           DECIMAL(18,2)    NOT NULL,
                ValorNuevo              DECIMAL(18,2)    NOT NULL,
                FechaAnterior           DATE             NOT NULL,
                FechaNueva              DATE             NOT NULL,
                TipoAnteriorId          UNIQUEIDENTIFIER NOT NULL,
                TipoNuevoId             UNIQUEIDENTIFIER NOT NULL,
                EstadoAnterior          NVARCHAR(20)     NOT NULL,
                EstadoNuevo             NVARCHAR(20)     NOT NULL,
                Motivo                  NVARCHAR(300)    NOT NULL,
                UsuarioId               UNIQUEIDENTIFIER NOT NULL,
                Fecha                   DATETIME2        NOT NULL,
                CONSTRAINT PK_utilidades_AuditoriaCambios PRIMARY KEY (AuditoriaCambioGastoId),
                CONSTRAINT FK_utilidades_AuditoriaCambios_Gastos FOREIGN KEY (GastoId) REFERENCES utilidades.Gastos (GastoId)
            );
            CREATE INDEX IX_utilidades_AuditoriaCambios_Gasto ON utilidades.AuditoriaCambios (GastoId);
        END
        """ + Semilla();

    private static string Semilla()
    {
        var filas = string.Join(",\n", CatalogoTiposGasto.Inicial.Select(s =>
            $"                ('{s.Id}', N'{s.Nombre.Replace("'", "''")}', 1, 1, SYSUTCDATETIME(), '00000000-0000-0000-0000-000000000000')"));
        return $"""

            IF NOT EXISTS (SELECT 1 FROM utilidades.TiposGasto)
            BEGIN
                INSERT INTO utilidades.TiposGasto
                    (TipoGastoId, Nombre, Activo, RequiereObservacion, FechaCreacion, UsuarioCreacionId)
                VALUES
            {filas};
            END
            """;
    }
}
