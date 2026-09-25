/* Horario de disponibilidad por lotería (inicio y fin dentro de la ventana del PDA). */
IF COL_LENGTH(N'dbo.Loterias', N'HoraInicio') IS NULL
BEGIN
    ALTER TABLE dbo.Loterias
        ADD HoraInicio TIME(0) NOT NULL
            CONSTRAINT DF_Loterias_HoraInicio DEFAULT ('10:00:00');
    PRINT 'Columna Loterias.HoraInicio agregada (inicial 10:00).';
END
GO

IF COL_LENGTH(N'dbo.Loterias', N'HoraFin') IS NULL
BEGIN
    ALTER TABLE dbo.Loterias
        ADD HoraFin TIME(0) NOT NULL
            CONSTRAINT DF_Loterias_HoraFin DEFAULT ('13:00:00');
    PRINT 'Columna Loterias.HoraFin agregada (inicial 13:00).';
END
GO

UPDATE dbo.Loterias
SET HoraInicio = '10:00:00'
WHERE HoraInicio IS NULL;
GO

UPDATE dbo.Loterias
SET HoraFin = '13:00:00'
WHERE HoraFin IS NULL;
GO
