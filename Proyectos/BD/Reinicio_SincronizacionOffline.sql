/*
  Deja el entorno como si el PDA nunca hubiera sincronizado:
    - Borra la marca de reposicion diaria (permite la "primera del dia").
    - Borra los codigos offline sin usar del PDA.
    - Alinea la capacidad del PDA con el parametro vigente de Configuraciones.
  No toca codigos ya vendidos ni registrados.
*/
USE [NewRich];
GO

SET NOCOUNT ON;

DECLARE @capacidad INT = (
    SELECT TRY_CONVERT(INT, Valor)
    FROM dbo.Configuraciones
    WHERE Clave = 'CodigosOfflineCapacidad');

IF @capacidad IS NULL OR @capacidad < 1
    SET @capacidad = 3000;

DELETE FROM dbo.Sincronizaciones
WHERE Tipo = 'ReposicionOfflineDiaria';
PRINT CONCAT('Marcas de reposicion diaria eliminadas: ', @@ROWCOUNT);

DELETE FROM dbo.CodigosPreventaOffline
WHERE EstadoDelCodigo IN ('Generado', 'Descargado');
PRINT CONCAT('Codigos offline sin usar eliminados: ', @@ROWCOUNT);

UPDATE dbo.Dispositivos
SET CapacidadCodigosOffline = @capacidad;
PRINT CONCAT('PDA alineados a capacidad ', @capacidad, ': ', @@ROWCOUNT);
GO
