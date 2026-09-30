/*
  Retención de históricos: auditoría del proceso y parámetros fijos.
  MesesMaximosRetencion queda en 6. MesesAEliminar admite solo 1, 2 o 3.
*/
USE [NewRich];
GO

IF OBJECT_ID(N'dbo.AuditoriaRetencion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditoriaRetencion (
        AuditoriaRetencionId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        FechaEjecucionUtc     DATETIME2        NOT NULL,
        MesesMaximos          INT              NOT NULL,
        MesesAEliminar        INT              NULL,
        PeriodosEvaluados     NVARCHAR(200)    NOT NULL,
        PeriodosEliminados    NVARCHAR(200)    NOT NULL,
        VentasEliminadas      INT              NOT NULL,
        PremiosEliminados     INT              NOT NULL,
        PagosEliminados       INT              NOT NULL,
        JuegosEliminados      INT              NOT NULL,
        Resultado             NVARCHAR(20)     NOT NULL,
        MensajeError          NVARCHAR(500)    NULL,
        CONSTRAINT PK_AuditoriaRetencion PRIMARY KEY (AuditoriaRetencionId)
    );
    CREATE INDEX IX_AuditoriaRetencion_FechaEjecucionUtc ON dbo.AuditoriaRetencion(FechaEjecucionUtc);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Configuraciones WHERE Clave = N'MesesMaximosRetencion')
    INSERT INTO dbo.Configuraciones (ConfiguracionId, Clave, Valor, FechaActualizacion)
    VALUES (NEWID(), N'MesesMaximosRetencion', N'6', SYSUTCDATETIME());
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Configuraciones WHERE Clave = N'MesesAEliminar')
    INSERT INTO dbo.Configuraciones (ConfiguracionId, Clave, Valor, FechaActualizacion)
    VALUES (NEWID(), N'MesesAEliminar', N'1', SYSUTCDATETIME());
GO
