namespace NewRich.Infrastructure.Persistence;

internal static class EsquemaAvisosChat
{
    internal const string Tablas = """
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
        """;
}
