/*
  Amplía el CHECK de CapacidadCodigosOffline para permitir cualquier valor >= 1
  (antes: BETWEEN 3000 AND 5000).
*/
USE [NewRich];
GO

IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Dispositivos_Capacidad'
      AND parent_object_id = OBJECT_ID(N'dbo.Dispositivos'))
BEGIN
    ALTER TABLE dbo.Dispositivos DROP CONSTRAINT CK_Dispositivos_Capacidad;
END
GO

ALTER TABLE dbo.Dispositivos
ADD CONSTRAINT CK_Dispositivos_Capacidad
CHECK (CapacidadCodigosOffline >= 1);
GO
