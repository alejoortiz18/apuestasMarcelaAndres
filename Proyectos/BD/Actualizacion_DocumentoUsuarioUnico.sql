-- El documento es opcional, pero UQ_Usuarios_Documento trata dos nulos como iguales:
-- con un solo usuario sin documento, crear el siguiente fallaba con HTTP 500.
-- Se reemplaza por un índice único filtrado, que exige documentos distintos solo cuando existen.
SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS (SELECT 1 FROM sys.key_constraints
           WHERE name = 'UQ_Usuarios_Documento' AND parent_object_id = OBJECT_ID(N'dbo.Usuarios'))
BEGIN
    ALTER TABLE dbo.Usuarios DROP CONSTRAINT UQ_Usuarios_Documento;
    PRINT 'Restricción UQ_Usuarios_Documento eliminada.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UX_Usuarios_Documento' AND object_id = OBJECT_ID(N'dbo.Usuarios'))
BEGIN
    CREATE UNIQUE INDEX UX_Usuarios_Documento
        ON dbo.Usuarios (Documento)
        WHERE Documento IS NOT NULL;
    PRINT 'Índice UX_Usuarios_Documento creado.';
END
GO
