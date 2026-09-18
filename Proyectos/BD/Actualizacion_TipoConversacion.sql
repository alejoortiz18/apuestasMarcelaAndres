/* Agrega canal SoporteTecnico a Conversaciones. Filas existentes = AtencionCliente. */
IF COL_LENGTH(N'dbo.Conversaciones', N'Tipo') IS NULL
BEGIN
    ALTER TABLE dbo.Conversaciones
        ADD Tipo NVARCHAR(30) NOT NULL
            CONSTRAINT DF_Conversaciones_Tipo DEFAULT (N'AtencionCliente');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_Conversaciones_Tipo' AND parent_object_id = OBJECT_ID(N'dbo.Conversaciones'))
BEGIN
    ALTER TABLE dbo.Conversaciones
        ADD CONSTRAINT CK_Conversaciones_Tipo
            CHECK (Tipo IN (N'AtencionCliente', N'SoporteTecnico'));
    PRINT 'Columna Conversaciones.Tipo lista.';
END
GO
