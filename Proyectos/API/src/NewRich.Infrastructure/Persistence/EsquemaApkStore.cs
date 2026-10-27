namespace NewRich.Infrastructure.Persistence;

internal static class EsquemaApkStore
{
    internal const string Tablas = """
        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'apkstore')
            EXEC(N'CREATE SCHEMA apkstore');

        IF OBJECT_ID(N'apkstore.LlaveFirma', N'U') IS NULL
        BEGIN
            CREATE TABLE apkstore.LlaveFirma (
                LlaveFirmaId      UNIQUEIDENTIFIER NOT NULL,
                Alias             NVARCHAR(50)     NOT NULL,
                Almacen           VARBINARY(MAX)   NOT NULL,
                Clave             NVARCHAR(200)    NOT NULL,
                Huella            NVARCHAR(64)     NOT NULL,
                CreadaPor         UNIQUEIDENTIFIER NOT NULL,
                FechaCreacionUtc  DATETIME2        NOT NULL,
                CONSTRAINT PK_apkstore_LlaveFirma PRIMARY KEY (LlaveFirmaId)
            );
        END

        IF OBJECT_ID(N'apkstore.Versiones', N'U') IS NULL
        BEGIN
            CREATE TABLE apkstore.Versiones (
                VersionId            UNIQUEIDENTIFIER NOT NULL,
                NumeroCompilacion    INT              NOT NULL,
                NombreVersion        NVARCHAR(20)     NOT NULL,
                NombreArchivo        NVARCHAR(100)    NOT NULL,
                TamanoBytes          BIGINT           NOT NULL,
                Sha256               NVARCHAR(64)     NOT NULL,
                HuellaFirma          NVARCHAR(64)     NOT NULL,
                PublicadaPor         UNIQUEIDENTIFIER NOT NULL,
                FechaPublicacionUtc  DATETIME2        NOT NULL,
                CONSTRAINT PK_apkstore_Versiones PRIMARY KEY (VersionId)
            );

            CREATE UNIQUE INDEX UX_apkstore_Versiones_NumeroCompilacion
                ON apkstore.Versiones (NumeroCompilacion);
        END

        IF OBJECT_ID(N'apkstore.Auditoria', N'U') IS NULL
        BEGIN
            CREATE TABLE apkstore.Auditoria (
                AuditoriaId  UNIQUEIDENTIFIER NOT NULL,
                UsuarioId    UNIQUEIDENTIFIER NOT NULL,
                Accion       NVARCHAR(40)     NOT NULL,
                Detalle      NVARCHAR(400)    NULL,
                Ip           NVARCHAR(64)     NULL,
                FechaUtc     DATETIME2        NOT NULL,
                CONSTRAINT PK_apkstore_Auditoria PRIMARY KEY (AuditoriaId)
            );

            CREATE INDEX IX_apkstore_Auditoria_FechaUtc ON apkstore.Auditoria (FechaUtc);
        END

        -- Reemplazada por apkstore.Versiones.
        IF OBJECT_ID(N'dbo.VersionesAplicacion', N'U') IS NOT NULL
            DROP TABLE dbo.VersionesAplicacion;
        """;
}
