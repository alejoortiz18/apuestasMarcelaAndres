/* ============================================================================
   Actualizacion: la cedula del ganador se fotografia por el frente y por el reverso.

   Ejecutar una sola vez sobre la base de datos existente. Es idempotente: si ya
   se aplico, no hace nada. No modifica ni borra evidencias ya registradas; las
   filas antiguas con 'CedulaIdentidad' siguen siendo la foto del frente.
   ============================================================================ */

USE [NewRich];
GO

IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_EvidenciasGanador_Tipo'
      AND definition NOT LIKE N'%CedulaReverso%')
BEGIN
    ALTER TABLE dbo.EvidenciasGanador DROP CONSTRAINT CK_EvidenciasGanador_Tipo;
    ALTER TABLE dbo.EvidenciasGanador
        ADD CONSTRAINT CK_EvidenciasGanador_Tipo
        CHECK (TipoEvidencia IN ('TicketConQR', 'GanadorConTicket', 'CedulaIdentidad', 'CedulaReverso'));
    PRINT 'CK_EvidenciasGanador_Tipo ahora admite CedulaReverso.';
END
ELSE
BEGIN
    PRINT 'CK_EvidenciasGanador_Tipo ya admitia CedulaReverso. No se hizo ningun cambio.';
END
GO
