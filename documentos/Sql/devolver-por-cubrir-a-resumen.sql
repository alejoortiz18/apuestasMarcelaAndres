/*
  Revisada manual ya no tiene la vista Por cubrir: desde Resumen se pasa directo a Cubierto.
  Este script devuelve a Resumen lo que quedó marcado como Por cubrir (sin comisión), para
  volverlo a cubrir desde allí.

  - Borra las operaciones en estado PorCubrir y sus líneas; los registros de origen no se tocan,
    así que vuelven a aparecer en Resumen.
  - No toca las operaciones en estado Cubierto.
  - Se puede ejecutar varias veces.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DELETE l
FROM manual.RevisadaManualLineas l
JOIN manual.RevisadaManualOperaciones o ON o.OperacionId = l.OperacionId
WHERE o.Estado = 'PorCubrir';
DECLARE @lineas int = @@ROWCOUNT;

DELETE FROM manual.RevisadaManualOperaciones
WHERE Estado = 'PorCubrir';
DECLARE @operaciones int = @@ROWCOUNT;

COMMIT TRANSACTION;

SELECT GruposDevueltos = @operaciones, RegistrosDevueltos = @lineas;
