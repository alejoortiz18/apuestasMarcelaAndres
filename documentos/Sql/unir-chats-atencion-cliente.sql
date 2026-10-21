/*
  Une los chats duplicados de Atención al cliente: cada usuario (vendedor, recaudador u observador)
  queda con un único chat con la administración.

  - Se conserva el chat con la actividad más reciente, el mismo que usa el API desde ahora.
  - Los mensajes (y sus adjuntos) de los demás chats pasan al conservado.
  - El chat conservado toma la fecha de inicio más antigua y queda abierto si alguno lo estaba.
  - Los chats que quedan vacíos se borran.

  Se puede ejecutar varias veces: si no hay duplicados no cambia nada.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF OBJECT_ID('tempdb..#Chats') IS NOT NULL DROP TABLE #Chats;
IF OBJECT_ID('tempdb..#Union') IS NOT NULL DROP TABLE #Union;

;WITH Administracion AS (
    SELECT UsuarioId FROM Usuarios WHERE Rol IN ('Administrador', 'Super')
),
Chats AS (
    SELECT
        c.ConversacionId,
        c.FechaInicio,
        c.Estado,
        Cliente = CASE WHEN c.UsuarioIniciadorId IN (SELECT UsuarioId FROM Administracion)
                       THEN c.UsuarioDestinoId ELSE c.UsuarioIniciadorId END,
        Ultimo = ISNULL((SELECT MAX(m.FechaEnvio) FROM Mensajes m WHERE m.ConversacionId = c.ConversacionId), c.FechaInicio)
    FROM Conversaciones c
    WHERE c.Tipo = 'AtencionCliente'
      AND (c.UsuarioIniciadorId IN (SELECT UsuarioId FROM Administracion)
           OR c.UsuarioDestinoId IN (SELECT UsuarioId FROM Administracion))
      AND NOT (c.UsuarioIniciadorId IN (SELECT UsuarioId FROM Administracion)
               AND c.UsuarioDestinoId IN (SELECT UsuarioId FROM Administracion))
)
SELECT *, Fila = ROW_NUMBER() OVER (PARTITION BY Cliente ORDER BY Ultimo DESC, FechaInicio DESC, ConversacionId)
INTO #Chats
FROM Chats;

SELECT d.ConversacionId AS Duplicada, k.ConversacionId AS Conservada
INTO #Union
FROM #Chats d
JOIN #Chats k ON k.Cliente = d.Cliente AND k.Fila = 1
WHERE d.Fila > 1;

UPDATE c
SET FechaInicio = g.Inicio,
    Estado = CASE WHEN g.Abiertas > 0 THEN 'Abierta' ELSE c.Estado END,
    FechaCierre = CASE WHEN g.Abiertas > 0 THEN NULL ELSE c.FechaCierre END
FROM Conversaciones c
JOIN (
    SELECT k.ConversacionId,
           Inicio = MIN(x.FechaInicio),
           Abiertas = SUM(CASE WHEN x.Estado = 'Abierta' THEN 1 ELSE 0 END)
    FROM #Chats k
    JOIN #Chats x ON x.Cliente = k.Cliente
    WHERE k.Fila = 1
    GROUP BY k.ConversacionId
    HAVING COUNT(*) > 1
) g ON g.ConversacionId = c.ConversacionId;

UPDATE m
SET ConversacionId = u.Conservada
FROM Mensajes m
JOIN #Union u ON u.Duplicada = m.ConversacionId;
DECLARE @mensajes int = @@ROWCOUNT;

DELETE c
FROM Conversaciones c
JOIN #Union u ON u.Duplicada = c.ConversacionId;
DECLARE @chats int = @@ROWCOUNT;

COMMIT TRANSACTION;

SELECT ChatsUnidos = @chats, MensajesMovidos = @mensajes;
