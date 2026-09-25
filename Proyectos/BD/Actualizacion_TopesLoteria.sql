/* Topes diarios por lotería (acumulado por número = directo + combinado). */
IF COL_LENGTH(N'dbo.Loterias', N'Tope') IS NULL
BEGIN
    ALTER TABLE dbo.Loterias
        ADD Tope DECIMAL(18, 2) NOT NULL
            CONSTRAINT DF_Loterias_Tope DEFAULT (1000);
    PRINT 'Columna Loterias.Tope agregada (inicial 1000).';
END
GO

UPDATE dbo.Loterias
SET Tope = 1000
WHERE Tope IS NULL OR Tope < 0;
GO

/* Flag nuevo: permitir juegos offline en el PDA. */
IF NOT EXISTS (SELECT 1 FROM dbo.Configuraciones WHERE Clave = N'PermitirJuegosOffline')
BEGIN
    INSERT INTO dbo.Configuraciones (ConfiguracionId, Clave, Valor, FechaActualizacion)
    VALUES (NEWID(), N'PermitirJuegosOffline', N'true', SYSUTCDATETIME());
    PRINT 'Configuración PermitirJuegosOffline insertada.';
END
GO
