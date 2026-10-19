/*
  Alinea una base NewRich con el esquema de Azure (copia del 14/10/2026) sin tocar los datos:
  - Tabla ConfirmacionesAccion.
  - CK_Dispositivos_Capacidad: CapacidadCodigosOffline >= 0 (antes >= 1).
  - UQ_Usuarios_Documento como índice único filtrado: varios usuarios pueden quedar sin documento.
    UsuarioService reconoce el duplicado por este nombre.
  - Procedimiento RestablecerContrasena.
  Se puede ejecutar más de una vez.
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.ConfirmacionesAccion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConfirmacionesAccion (
        Token          NVARCHAR(64)     NOT NULL,
        UsuarioId      UNIQUEIDENTIFIER NOT NULL,
        Accion         NVARCHAR(80)     NOT NULL,
        Expira         DATETIME2(7)     NOT NULL,
        UsosRestantes  INT              NOT NULL,
        CONSTRAINT PK_ConfirmacionesAccion PRIMARY KEY CLUSTERED (Token)
    );
    PRINT 'Tabla ConfirmacionesAccion creada.';
END;

IF EXISTS (SELECT 1 FROM sys.check_constraints
           WHERE name = N'CK_Dispositivos_Capacidad'
             AND parent_object_id = OBJECT_ID(N'dbo.Dispositivos')
             AND definition <> N'([CapacidadCodigosOffline]>=(0))')
BEGIN
    ALTER TABLE dbo.Dispositivos DROP CONSTRAINT CK_Dispositivos_Capacidad;
END;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = N'CK_Dispositivos_Capacidad' AND parent_object_id = OBJECT_ID(N'dbo.Dispositivos'))
BEGIN
    ALTER TABLE dbo.Dispositivos WITH CHECK ADD CONSTRAINT CK_Dispositivos_Capacidad CHECK (CapacidadCodigosOffline >= 0);
    PRINT 'CK_Dispositivos_Capacidad ahora acepta 0.';
END;

IF EXISTS (SELECT 1 FROM sys.key_constraints
           WHERE name = N'UQ_Usuarios_Documento' AND parent_object_id = OBJECT_ID(N'dbo.Usuarios'))
BEGIN
    ALTER TABLE dbo.Usuarios DROP CONSTRAINT UQ_Usuarios_Documento;
END;
IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = N'UQ_Usuarios_Documento' AND object_id = OBJECT_ID(N'dbo.Usuarios') AND has_filter = 0)
BEGIN
    DROP INDEX UQ_Usuarios_Documento ON dbo.Usuarios;
END;
IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = N'UX_Usuarios_Documento' AND object_id = OBJECT_ID(N'dbo.Usuarios'))
BEGIN
    DROP INDEX UX_Usuarios_Documento ON dbo.Usuarios;
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'UQ_Usuarios_Documento' AND object_id = OBJECT_ID(N'dbo.Usuarios'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Usuarios_Documento ON dbo.Usuarios (Documento)
        WHERE Documento IS NOT NULL;
    PRINT 'UQ_Usuarios_Documento ahora es un índice filtrado.';
END;

COMMIT TRANSACTION;
GO

CREATE OR ALTER PROCEDURE dbo.RestablecerContrasena
    @UsuarioId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Usuarios
    SET
        EstadoBloqueado = 0,
        IntentosFallidos = 0
    WHERE UsuarioId = @UsuarioId;

    SELECT *
    FROM Usuarios
    WHERE UsuarioId = @UsuarioId;
END
GO
PRINT 'Procedimiento RestablecerContrasena listo.';
GO
