/* ============================================================
   NEW RICH - Limpia datos operativos.
   Conserva usuarios super y admin, y los datos maestros.
   No modifica configuraciones, loterías, grupos, roles,
   estados de boleto, números restringidos ni versiones.
   ============================================================ */
USE [NewRich];
GO

SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRANSACTION;

/* Datos operativos, en orden de claves foráneas. */
DELETE FROM dbo.EvidenciasGanador;
DELETE FROM dbo.EntregasGanadores;

UPDATE dbo.Boletos
SET CasoGanadorId = NULL
WHERE CasoGanadorId IS NOT NULL;

DELETE FROM dbo.CasosGanadores;
DELETE FROM dbo.AdjuntosChat;
DELETE FROM dbo.Mensajes;
DELETE FROM dbo.Conversaciones;
DELETE FROM dbo.Notificaciones;
DELETE FROM dbo.Sincronizaciones;
DELETE FROM dbo.ClavesValidacionBoleto;
DELETE FROM dbo.JuegoLoteria;
DELETE FROM dbo.Juegos;
DELETE FROM dbo.Boletos;
DELETE FROM dbo.CodigosPreventaOffline;
DELETE FROM dbo.Ventas;
DELETE FROM dbo.NumerosGanadores;
DELETE FROM dbo.DispositivosUsuarios;
DELETE FROM dbo.Sesiones;
DELETE FROM dbo.IntentosFallidos;
DELETE FROM dbo.LlavesAdministrador;
DELETE FROM dbo.Dispositivos;

/* Asignaciones y usuarios que no son super ni admin. */
DELETE FROM dbo.UsuariosGrupos;

DELETE FROM dbo.UsuariosRoles
WHERE UsuarioId NOT IN (
    SELECT UsuarioId
    FROM dbo.Usuarios
    WHERE Usuario IN (N'admin', N'super')
);

DELETE FROM dbo.Usuarios
WHERE Usuario NOT IN (N'admin', N'super');

COMMIT TRANSACTION;
GO

SELECT Usuario, Rol, Estado
FROM dbo.Usuarios
ORDER BY Usuario;

SELECT
    (SELECT COUNT(*) FROM dbo.Ventas) AS Ventas,
    (SELECT COUNT(*) FROM dbo.Boletos) AS Boletos,
    (SELECT COUNT(*) FROM dbo.Sesiones) AS Sesiones,
    (SELECT COUNT(*) FROM dbo.Dispositivos) AS Dispositivos,
    (SELECT COUNT(*) FROM dbo.Usuarios) AS Usuarios,
    (SELECT COUNT(*) FROM dbo.Loterias) AS Loterias,
    (SELECT COUNT(*) FROM dbo.Configuraciones) AS Configuraciones,
    (SELECT COUNT(*) FROM dbo.Roles) AS Roles;
GO
