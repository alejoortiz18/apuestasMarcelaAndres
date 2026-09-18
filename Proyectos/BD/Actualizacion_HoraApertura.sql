-- Hora de apertura operativa del PDA (configuración operativa).
IF NOT EXISTS (SELECT 1 FROM dbo.Configuraciones WHERE Clave = 'HoraApertura')
    INSERT INTO dbo.Configuraciones (Clave, Valor) VALUES ('HoraApertura', '10:00:00');
