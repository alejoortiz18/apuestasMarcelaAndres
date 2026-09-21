/* Llaves USB de administrador y rol Super */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Usuarios_Rol')
BEGIN
    ALTER TABLE dbo.Usuarios DROP CONSTRAINT CK_Usuarios_Rol;
END
GO
ALTER TABLE dbo.Usuarios ADD CONSTRAINT CK_Usuarios_Rol CHECK (Rol IN (N'Administrador', N'Vendedor', N'Observador', N'Super'));
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Super')
    INSERT INTO dbo.Roles (Nombre, Descripcion) VALUES (N'Super', N'Super usuario local para generar llaves de administrador');
GO

IF OBJECT_ID(N'dbo.LlavesAdministrador', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LlavesAdministrador (
        LlaveId             UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        UsuarioId           UNIQUEIDENTIFIER NOT NULL,
        Codigo              NVARCHAR(32)     NOT NULL,
        Estado              NVARCHAR(20)     NOT NULL,
        ClavePublica        NVARCHAR(4000)   NOT NULL,
        HuellaDispositivo   NVARCHAR(128)    NOT NULL,
        FechaCreacion       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        FechaActivacion     DATETIME2        NOT NULL,
        FechaUltimoUso      DATETIME2        NULL,
        FechaRevocacion     DATETIME2        NULL,
        MotivoRevocacion    NVARCHAR(200)    NULL,
        CONSTRAINT PK_LlavesAdministrador PRIMARY KEY (LlaveId),
        CONSTRAINT UQ_LlavesAdministrador_Codigo UNIQUE (Codigo),
        CONSTRAINT FK_LlavesAdministrador_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT CK_LlavesAdministrador_Estado CHECK (Estado IN (N'Activa', N'Revocada', N'Comprometida'))
    );
END
GO
