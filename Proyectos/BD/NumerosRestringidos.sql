/* Números que el administrador restringe para que no se puedan jugar. */
IF OBJECT_ID(N'dbo.NumerosRestringidos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NumerosRestringidos (
        NumeroRestringidoId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        Numero              NVARCHAR(4)      NOT NULL,
        FechaCreacion       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_NumerosRestringidos PRIMARY KEY (NumeroRestringidoId),
        CONSTRAINT UQ_NumerosRestringidos_Numero UNIQUE (Numero)
    );
END
GO
