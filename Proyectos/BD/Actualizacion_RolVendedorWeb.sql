/*
    Rol VendedorWeb: vende solo desde el sitio web del vendedor.
    No usa PDA, no pertenece a grupos y no ingresa al administrador.
    Script idempotente.
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Usuarios_Rol')
    ALTER TABLE dbo.Usuarios DROP CONSTRAINT CK_Usuarios_Rol;
GO

ALTER TABLE dbo.Usuarios WITH CHECK ADD CONSTRAINT CK_Usuarios_Rol
    CHECK (Rol IN (N'Administrador', N'Vendedor', N'Observador', N'Super', N'Recaudador', N'VendedorWeb'));
GO

ALTER TABLE dbo.Usuarios CHECK CONSTRAINT CK_Usuarios_Rol;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'VendedorWeb')
    INSERT INTO dbo.Roles (Nombre, Descripcion)
    VALUES (N'VendedorWeb', N'Vende unicamente desde el sitio web del vendedor, sin PDA');
GO
