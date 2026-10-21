/*
  Vincula los avisos de chat que ya existían con el chat de la persona que escribió, para que
  aparezcan en el ícono de chat del administrador (los avisos nuevos ya se guardan vinculados).

  - Correr después de unir-chats-atencion-cliente.sql.
  - Solo vincula avisos cuyo nombre corresponde a un único usuario; los demás se dejan como están.
  - Se puede ejecutar varias veces: no repite vínculos.
  - Con sqlcmd, usar -f 65001 para que lea las tildes del archivo en UTF-8.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'chat')
    EXEC(N'CREATE SCHEMA chat');

IF OBJECT_ID(N'chat.AvisosChat', N'U') IS NULL
BEGIN
    CREATE TABLE chat.AvisosChat (
        NotificacionId   UNIQUEIDENTIFIER NOT NULL,
        ConversacionId   UNIQUEIDENTIFIER NOT NULL,
        UsuarioEmisorId  UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_chat_AvisosChat PRIMARY KEY (NotificacionId)
    );

    CREATE INDEX IX_chat_AvisosChat_Conversacion
        ON chat.AvisosChat (ConversacionId);
END

BEGIN TRANSACTION;

INSERT chat.AvisosChat (NotificacionId, ConversacionId, UsuarioEmisorId)
SELECT n.NotificacionId, chat.ConversacionId, u.UsuarioId
FROM Notificaciones n
JOIN Usuarios u
    ON n.Mensaje = u.NombreCompleto + CASE WHEN n.Tipo = 'ChatSoporte'
                                           THEN N' escribió en soporte.'
                                           ELSE N' envió un reporte de soporte técnico.' END
CROSS APPLY (
    SELECT TOP 1 c.ConversacionId
    FROM Conversaciones c
    WHERE c.Tipo = CASE WHEN n.Tipo = 'ChatSoporte' THEN 'AtencionCliente' ELSE 'SoporteTecnico' END
      AND (c.UsuarioIniciadorId = u.UsuarioId OR c.UsuarioDestinoId = u.UsuarioId)
    ORDER BY ISNULL((SELECT MAX(m.FechaEnvio) FROM Mensajes m WHERE m.ConversacionId = c.ConversacionId), c.FechaInicio) DESC
) chat
WHERE n.Tipo IN ('ChatSoporte', 'ChatSoporteTecnico')
  AND NOT EXISTS (SELECT 1 FROM chat.AvisosChat a WHERE a.NotificacionId = n.NotificacionId)
  AND (SELECT COUNT(*) FROM Usuarios otro WHERE otro.NombreCompleto = u.NombreCompleto) = 1;

DECLARE @vinculados int = @@ROWCOUNT;

COMMIT TRANSACTION;

SELECT AvisosVinculados = @vinculados;
