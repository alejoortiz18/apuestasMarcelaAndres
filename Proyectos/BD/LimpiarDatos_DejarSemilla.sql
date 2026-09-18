/* ============================================================
   NEW RICH - Limpieza: deja solo datos semilla + usuario admin
   ============================================================ */
USE [NewRich];
GO

SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRANSACTION;

/* 1. Datos operativos / transaccionales */
DELETE FROM dbo.EvidenciasGanador;
DELETE FROM dbo.EntregasGanadores;
/* Boletos apunta a CasosGanadores: soltar la referencia antes de borrar casos. */
UPDATE dbo.Boletos SET CasoGanadorId = NULL WHERE CasoGanadorId IS NOT NULL;
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
DELETE FROM dbo.Ventas;
DELETE FROM dbo.CodigosPreventaOffline;
DELETE FROM dbo.NumerosGanadores;
DELETE FROM dbo.DispositivosUsuarios;
DELETE FROM dbo.Sesiones;
DELETE FROM dbo.IntentosFallidos;
DELETE FROM dbo.Dispositivos;

/* 2. Usuarios: solo queda admin */
DELETE FROM dbo.UsuariosGrupos
WHERE UsuarioId NOT IN (SELECT UsuarioId FROM dbo.Usuarios WHERE Usuario = N'admin');

DELETE FROM dbo.UsuariosRoles
WHERE UsuarioId NOT IN (SELECT UsuarioId FROM dbo.Usuarios WHERE Usuario = N'admin');

DELETE FROM dbo.Usuarios
WHERE Usuario <> N'admin';

/* 3. Asegurar rol Administrador del admin */
DECLARE @AdminUserId UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE Usuario = N'admin');
DECLARE @AdminRoleId INT = (SELECT RolId FROM dbo.Roles WHERE Nombre = N'Administrador');

IF @AdminUserId IS NOT NULL AND @AdminRoleId IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM dbo.UsuariosRoles
       WHERE UsuarioId = @AdminUserId AND RolId = @AdminRoleId)
BEGIN
    INSERT INTO dbo.UsuariosRoles (UsuarioId, RolId) VALUES (@AdminUserId, @AdminRoleId);
END

/* 4. Restablecer admin a estado semilla (password temporal Admin123!) */
IF @AdminUserId IS NOT NULL
BEGIN
    UPDATE dbo.Usuarios
    SET NombreCompleto = N'Administrador Principal',
        Alias = N'Admin',
        Email = N'admin@newrich.com',
        PasswordHash = N'367C4B2DB148D69B17F617E3CDE3194D4A7D383F03B5C30736CEE7C3E0F80556',
        PasswordSalt = N'D6B428E0B2EE341BE3405E98080A0E2D',
        Rol = N'Administrador',
        Estado = N'Activo',
        EstadoValidado = 1,
        EstadoBloqueado = 0,
        IntentosFallidos = 0
    WHERE UsuarioId = @AdminUserId;
END

/* 5. Restaurar configuraciones semilla */
UPDATE dbo.Configuraciones SET Valor = N'20:00:00' WHERE Clave = N'HoraCierre';
UPDATE dbo.Configuraciones SET Valor = N'10:00:00' WHERE Clave = N'HoraApertura';
UPDATE dbo.Configuraciones SET Valor = N'30' WHERE Clave = N'VigenciaPremiosDias';
UPDATE dbo.Configuraciones SET Valor = N'10' WHERE Clave = N'AlertaRepeticionNumero';
UPDATE dbo.Configuraciones SET Valor = N'10000' WHERE Clave = N'AlertaValorMinimo';
UPDATE dbo.Configuraciones SET Valor = N'3000' WHERE Clave = N'CodigosOfflineCapacidad';
UPDATE dbo.Configuraciones SET Valor = N'Manual' WHERE Clave = N'SincronizacionModo';
UPDATE dbo.Configuraciones
SET Valor = N'CONSERVE SU TICKET EN PERFECTO ESTADO.' + CHAR(10)
    + N'Vigencia: {vigenciaDias} días calendario desde su emisión. Vencido este plazo, el premio caducará y no será pagado.' + CHAR(10)
    + N'La aprobación del premio se realizará después de transcurridas 24 horas desde el momento en que el cliente lo haya reportado como ganador.'
WHERE Clave = N'LeyendaTirilla';

IF NOT EXISTS (SELECT 1 FROM dbo.Configuraciones WHERE Clave = N'HoraApertura')
    INSERT INTO dbo.Configuraciones (Clave, Valor) VALUES (N'HoraApertura', N'10:00:00');

UPDATE dbo.ConfiguracionesTipoApuesta SET Maximo = 1 WHERE TipoApuesta = N'COMBINADO';
UPDATE dbo.ConfiguracionesTipoApuesta SET Maximo = 6 WHERE TipoApuesta = N'INDIVIDUAL';

/* 6. Loterías: solo las semilla; días 1-7 */
DELETE FROM dbo.LoteriasDiasSemana
WHERE LoteriaId NOT IN (
    SELECT LoteriaId FROM dbo.Loterias
    WHERE Nombre IN (N'Bogotá', N'Medellín', N'Cali', N'Armenia', N'Pasto')
);

DELETE FROM dbo.Loterias
WHERE Nombre NOT IN (N'Bogotá', N'Medellín', N'Cali', N'Armenia', N'Pasto');

IF NOT EXISTS (SELECT 1 FROM dbo.Loterias WHERE Nombre = N'Bogotá')
    INSERT INTO dbo.Loterias (Nombre, Estado) VALUES (N'Bogotá', N'Activo');
IF NOT EXISTS (SELECT 1 FROM dbo.Loterias WHERE Nombre = N'Medellín')
    INSERT INTO dbo.Loterias (Nombre, Estado) VALUES (N'Medellín', N'Activo');
IF NOT EXISTS (SELECT 1 FROM dbo.Loterias WHERE Nombre = N'Cali')
    INSERT INTO dbo.Loterias (Nombre, Estado) VALUES (N'Cali', N'Activo');
IF NOT EXISTS (SELECT 1 FROM dbo.Loterias WHERE Nombre = N'Armenia')
    INSERT INTO dbo.Loterias (Nombre, Estado) VALUES (N'Armenia', N'Activo');
IF NOT EXISTS (SELECT 1 FROM dbo.Loterias WHERE Nombre = N'Pasto')
    INSERT INTO dbo.Loterias (Nombre, Estado) VALUES (N'Pasto', N'Activo');

UPDATE dbo.Loterias SET Estado = N'Activo'
WHERE Nombre IN (N'Bogotá', N'Medellín', N'Cali', N'Armenia', N'Pasto');

DELETE FROM dbo.LoteriasDiasSemana;

INSERT INTO dbo.LoteriasDiasSemana (LoteriaId, DiaSemana)
SELECT l.LoteriaId, d.DiaSemana
FROM dbo.Loterias l
CROSS JOIN (VALUES (1), (2), (3), (4), (5), (6), (7)) d(DiaSemana)
WHERE l.Nombre IN (N'Bogotá', N'Medellín', N'Cali', N'Armenia', N'Pasto');

/* 7. Grupos semilla */
DELETE FROM dbo.UsuariosGrupos;

DELETE FROM dbo.Grupos
WHERE Nombre NOT IN (N'Grupo Norte', N'Grupo Sur', N'Grupo Centro');

IF NOT EXISTS (SELECT 1 FROM dbo.Grupos WHERE Nombre = N'Grupo Norte')
    INSERT INTO dbo.Grupos (Nombre, Descripcion) VALUES (N'Grupo Norte', N'Vendedores de la zona norte');
IF NOT EXISTS (SELECT 1 FROM dbo.Grupos WHERE Nombre = N'Grupo Sur')
    INSERT INTO dbo.Grupos (Nombre, Descripcion) VALUES (N'Grupo Sur', N'Vendedores de la zona sur');
IF NOT EXISTS (SELECT 1 FROM dbo.Grupos WHERE Nombre = N'Grupo Centro')
    INSERT INTO dbo.Grupos (Nombre, Descripcion) VALUES (N'Grupo Centro', N'Vendedores de la zona centro');

COMMIT TRANSACTION;

PRINT 'Limpieza completada: solo datos semilla y usuario admin.';
GO

SELECT Usuario, Rol, Estado, EstadoBloqueado, IntentosFallidos FROM dbo.Usuarios;
SELECT Nombre, Estado FROM dbo.Loterias ORDER BY Nombre;
SELECT Nombre FROM dbo.Grupos ORDER BY Nombre;
SELECT
    (SELECT COUNT(*) FROM dbo.Ventas) AS Ventas,
    (SELECT COUNT(*) FROM dbo.Boletos) AS Boletos,
    (SELECT COUNT(*) FROM dbo.Sesiones) AS Sesiones,
    (SELECT COUNT(*) FROM dbo.Dispositivos) AS Dispositivos,
    (SELECT COUNT(*) FROM dbo.Usuarios) AS Usuarios;
GO
