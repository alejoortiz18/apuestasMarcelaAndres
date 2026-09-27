/* Versiones del APK que el administrador publica para vendedores y observadores. */
IF OBJECT_ID(N'dbo.VersionesAplicacion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VersionesAplicacion (
        VersionAplicacionId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        NumeroCompilacion    INT              NOT NULL,
        NombreVersion        NVARCHAR(20)     NOT NULL,
        NombreArchivo        NVARCHAR(32)     NOT NULL,
        TamanoBytes          BIGINT           NOT NULL,
        FechaPublicacion     DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_VersionesAplicacion PRIMARY KEY (VersionAplicacionId),
        CONSTRAINT UQ_VersionesAplicacion_Compilacion UNIQUE (NumeroCompilacion)
    );
END
GO
