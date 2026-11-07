namespace NewRich.Infrastructure.Persistence;

internal static class CierreEsquema
{
    internal const string Sql = """
        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'cierre')
            EXEC(N'CREATE SCHEMA cierre');

        IF OBJECT_ID(N'cierre.Cierres', N'U') IS NULL
        BEGIN
            CREATE TABLE cierre.Cierres (
                CierreId            UNIQUEIDENTIFIER NOT NULL,
                FechaInicio         DATETIME2        NULL,
                FechaFin            DATETIME2        NOT NULL,
                Dias                INT              NOT NULL,
                FechaEjecucion      DATETIME2        NOT NULL,
                UsuarioId           UNIQUEIDENTIFIER NOT NULL,
                TotalVentas         DECIMAL(18,2)    NOT NULL,
                CantidadVentas      INT              NOT NULL,
                TotalRecaudos       DECIMAL(18,2)    NOT NULL,
                CantidadRecaudos    INT              NOT NULL,
                TotalPremios        DECIMAL(18,2)    NOT NULL,
                CantidadPremios     INT              NOT NULL,
                TotalGastos         DECIMAL(18,2)    NOT NULL,
                CantidadGastos      INT              NOT NULL,
                TotalCubierto       DECIMAL(18,2)    NOT NULL,
                CantidadCubierto    INT              NOT NULL,
                TotalComisiones     DECIMAL(18,2)    NOT NULL,
                CantidadComisiones  INT              NOT NULL,
                TotalSalarios       DECIMAL(18,2)    NOT NULL,
                CantidadSalarios    INT              NOT NULL,
                TotalGanancia       DECIMAL(18,2)    NOT NULL,
                CONSTRAINT PK_Cierres PRIMARY KEY (CierreId),
                CONSTRAINT FK_Cierres_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios (UsuarioId)
            );
            CREATE INDEX IX_Cierres_FechaFin ON cierre.Cierres (FechaFin);
        END;

        EXEC(N'
            CREATE OR ALTER PROCEDURE cierre.AgregarCierreId
                @esquema sysname,
                @tabla sysname
            AS
            BEGIN
                DECLARE @objeto nvarchar(300) = QUOTENAME(@esquema) + N''.'' + QUOTENAME(@tabla);
                IF COL_LENGTH(@esquema + N''.'' + @tabla, N''CierreId'') IS NULL
                    EXEC(N''ALTER TABLE '' + @objeto + N'' ADD CierreId UNIQUEIDENTIFIER NULL'');
                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = N''FK_'' + @tabla + N''_Cierre''
                      AND parent_object_id = OBJECT_ID(@esquema + N''.'' + @tabla))
                    EXEC(N''ALTER TABLE '' + @objeto + N'' ADD CONSTRAINT FK_'' + @tabla + N''_Cierre FOREIGN KEY (CierreId) REFERENCES cierre.Cierres (CierreId)'');
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N''IX_'' + @tabla + N''_CierreId''
                      AND object_id = OBJECT_ID(@esquema + N''.'' + @tabla))
                    EXEC(N''CREATE INDEX IX_'' + @tabla + N''_CierreId ON '' + @objeto + N'' (CierreId)'');
            END
        ');

        EXEC cierre.AgregarCierreId N'dbo', N'Ventas';
        EXEC cierre.AgregarCierreId N'manual', N'Ventas';
        EXEC cierre.AgregarCierreId N'manual', N'VentasRegistradasWeb';
        EXEC cierre.AgregarCierreId N'manual', N'RegistrosJuego';
        EXEC cierre.AgregarCierreId N'manual', N'RegistrosJuegoNumerosWeb';
        EXEC cierre.AgregarCierreId N'recaudo', N'Pagos';
        EXEC cierre.AgregarCierreId N'dbo', N'EntregasGanadores';
        EXEC cierre.AgregarCierreId N'manual', N'PremiosEntregadosWeb';
        EXEC cierre.AgregarCierreId N'utilidades', N'Gastos';
        EXEC cierre.AgregarCierreId N'manual', N'RevisadaManualOperaciones';
        """;
}
