/*
    Modulo Recaudo. Solo se ejecuta en la base local de este equipo.
    No altera tablas ni procedimientos de ventas, boletos, grupos ni dispositivos.
    Amplia CK_Usuarios_Rol y CK_Dispositivos_Tipo para admitir Recaudador
    junto a los valores actuales.
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF @@SERVERNAME <> N'DESKTOP-JLLF9LK'
BEGIN
    THROW 50001, N'Este script solo corre en la base local DESKTOP-JLLF9LK.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'recaudo')
    EXEC(N'CREATE SCHEMA recaudo');
GO

IF OBJECT_ID(N'recaudo.AsignacionesGrupo', N'U') IS NULL
BEGIN
    CREATE TABLE recaudo.AsignacionesGrupo
    (
        AsignacionId uniqueidentifier NOT NULL CONSTRAINT PK_Recaudo_AsignacionesGrupo PRIMARY KEY,
        RecaudadorId uniqueidentifier NOT NULL,
        GrupoId uniqueidentifier NOT NULL,
        Porcentaje int NOT NULL,
        Estado nvarchar(20) NOT NULL CONSTRAINT DF_Recaudo_AsignacionesGrupo_Estado DEFAULT N'Activa',
        FechaCreacion datetime2 NOT NULL,
        FechaModificacion datetime2 NOT NULL,
        CONSTRAINT CK_Recaudo_AsignacionesGrupo_Porcentaje CHECK (Porcentaje BETWEEN 1 AND 100)
    );
    CREATE UNIQUE INDEX UQ_Recaudo_AsignacionesGrupo_Activa
        ON recaudo.AsignacionesGrupo (GrupoId)
        WHERE Estado = N'Activa';
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Recaudo_AsignacionesGrupo_Activa')
    CREATE UNIQUE INDEX UQ_Recaudo_AsignacionesGrupo_Activa
        ON recaudo.AsignacionesGrupo (GrupoId)
        WHERE Estado = N'Activa';
GO

IF OBJECT_ID(N'recaudo.AsignacionesVendedor', N'U') IS NULL
BEGIN
    CREATE TABLE recaudo.AsignacionesVendedor
    (
        AsignacionId uniqueidentifier NOT NULL CONSTRAINT PK_Recaudo_AsignacionesVendedor PRIMARY KEY,
        RecaudadorId uniqueidentifier NOT NULL,
        VendedorId uniqueidentifier NOT NULL,
        Porcentaje int NOT NULL,
        Estado nvarchar(20) NOT NULL CONSTRAINT DF_Recaudo_AsignacionesVendedor_Estado DEFAULT N'Activa',
        FechaCreacion datetime2 NOT NULL,
        FechaModificacion datetime2 NOT NULL,
        CONSTRAINT CK_Recaudo_AsignacionesVendedor_Porcentaje CHECK (Porcentaje BETWEEN 1 AND 100)
    );
    CREATE UNIQUE INDEX UQ_Recaudo_AsignacionesVendedor_Activa
        ON recaudo.AsignacionesVendedor (VendedorId)
        WHERE Estado = N'Activa';
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Recaudo_AsignacionesVendedor_Activa')
    CREATE UNIQUE INDEX UQ_Recaudo_AsignacionesVendedor_Activa
        ON recaudo.AsignacionesVendedor (VendedorId)
        WHERE Estado = N'Activa';
GO

IF OBJECT_ID(N'recaudo.Obligaciones', N'U') IS NULL
BEGIN
    CREATE TABLE recaudo.Obligaciones
    (
        ObligacionId uniqueidentifier NOT NULL CONSTRAINT PK_Recaudo_Obligaciones PRIMARY KEY,
        VendedorId uniqueidentifier NOT NULL,
        RecaudadorId uniqueidentifier NOT NULL,
        Fecha date NOT NULL,
        TotalVendido decimal(18, 2) NOT NULL,
        Porcentaje int NOT NULL,
        ValorGenerado decimal(18, 2) NOT NULL,
        SaldoAnterior decimal(18, 2) NOT NULL,
        FechaGeneracion datetime2 NOT NULL,
        CONSTRAINT UQ_Recaudo_Obligaciones_VendedorFecha UNIQUE (VendedorId, Fecha)
    );
END;
GO

IF OBJECT_ID(N'recaudo.Pagos', N'U') IS NULL
BEGIN
    CREATE TABLE recaudo.Pagos
    (
        PagoId uniqueidentifier NOT NULL CONSTRAINT PK_Recaudo_Pagos PRIMARY KEY,
        ObligacionId uniqueidentifier NOT NULL,
        RecaudadorId uniqueidentifier NOT NULL,
        VendedorId uniqueidentifier NOT NULL,
        Valor decimal(18, 2) NOT NULL,
        SaldoResultante decimal(18, 2) NOT NULL,
        ClaveIdempotencia nvarchar(80) NOT NULL,
        FechaHora datetime2 NOT NULL,
        CONSTRAINT UQ_Recaudo_Pagos_Clave UNIQUE (ClaveIdempotencia),
        CONSTRAINT FK_Recaudo_Pagos_Obligacion FOREIGN KEY (ObligacionId) REFERENCES recaudo.Obligaciones (ObligacionId)
    );
END;
GO

IF OBJECT_ID(N'recaudo.TirillasCobro', N'U') IS NULL
BEGIN
    CREATE TABLE recaudo.TirillasCobro
    (
        TirillaId uniqueidentifier NOT NULL CONSTRAINT PK_Recaudo_TirillasCobro PRIMARY KEY,
        PagoId uniqueidentifier NOT NULL,
        Consecutivo int NOT NULL IDENTITY(1, 1),
        RecaudadorNombre nvarchar(200) NOT NULL,
        VendedorNombre nvarchar(200) NOT NULL,
        FechaHora datetime2 NOT NULL,
        ValorRecibido decimal(18, 2) NOT NULL,
        SaldoRestante decimal(18, 2) NOT NULL,
        CONSTRAINT UQ_Recaudo_TirillasCobro_Pago UNIQUE (PagoId),
        CONSTRAINT FK_Recaudo_TirillasCobro_Pago FOREIGN KEY (PagoId) REFERENCES recaudo.Pagos (PagoId)
    );
END;
GO

IF OBJECT_ID(N'recaudo.HistorialConfiguracion', N'U') IS NULL
BEGIN
    CREATE TABLE recaudo.HistorialConfiguracion
    (
        HistorialId uniqueidentifier NOT NULL CONSTRAINT PK_Recaudo_HistorialConfiguracion PRIMARY KEY,
        RecaudadorId uniqueidentifier NOT NULL,
        Tipo nvarchar(40) NOT NULL,
        Detalle nvarchar(400) NOT NULL,
        UsuarioId uniqueidentifier NOT NULL,
        FechaHora datetime2 NOT NULL
    );
END;
GO

IF OBJECT_ID(N'recaudo.Auditoria', N'U') IS NULL
BEGIN
    CREATE TABLE recaudo.Auditoria
    (
        AuditoriaId uniqueidentifier NOT NULL CONSTRAINT PK_Recaudo_Auditoria PRIMARY KEY,
        UsuarioId uniqueidentifier NOT NULL,
        FechaHora datetime2 NOT NULL,
        TipoOperacion nvarchar(40) NOT NULL,
        Valor decimal(18, 2) NULL,
        ObligacionId uniqueidentifier NULL,
        MovimientoId uniqueidentifier NOT NULL
    );
END;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Usuarios_Rol')
    ALTER TABLE dbo.Usuarios DROP CONSTRAINT CK_Usuarios_Rol;
GO

ALTER TABLE dbo.Usuarios WITH CHECK ADD CONSTRAINT CK_Usuarios_Rol
    CHECK (Rol IN (N'Administrador', N'Vendedor', N'Observador', N'Super', N'Recaudador'));
GO

ALTER TABLE dbo.Usuarios CHECK CONSTRAINT CK_Usuarios_Rol;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Dispositivos_Tipo')
    ALTER TABLE dbo.Dispositivos DROP CONSTRAINT CK_Dispositivos_Tipo;
GO

ALTER TABLE dbo.Dispositivos WITH CHECK ADD CONSTRAINT CK_Dispositivos_Tipo
    CHECK (Tipo IN (N'Vendedor', N'Observador', N'Recaudador'));
GO

ALTER TABLE dbo.Dispositivos CHECK CONSTRAINT CK_Dispositivos_Tipo;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Recaudador')
    INSERT INTO dbo.Roles (Nombre, Descripcion)
    VALUES (N'Recaudador', N'Recoge el dinero que los vendedores deben entregar');
GO

CREATE OR ALTER PROCEDURE recaudo.sp_recaudo_consultar_ventas
    @VendedorId uniqueidentifier,
    @Fecha date
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @InicioUtc datetime2 = DATEADD(HOUR, 5, CAST(@Fecha AS datetime2));
    DECLARE @FinUtc datetime2 = DATEADD(DAY, 1, @InicioUtc);
    SELECT ISNULL(SUM(Total), 0) AS TotalVendido
    FROM dbo.Ventas
    WHERE UsuarioId = @VendedorId
      AND FechaVenta >= @InicioUtc
      AND FechaVenta < @FinUtc;
END;
GO

CREATE OR ALTER PROCEDURE recaudo.sp_recaudo_configurar_recaudador
    @RecaudadorId uniqueidentifier,
    @UsuarioId uniqueidentifier,
    @Detalle nvarchar(400)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE UsuarioId = @RecaudadorId AND Rol = N'Recaudador')
    BEGIN
        THROW 50010, N'El usuario no es un recaudador.', 1;
    END;
    INSERT INTO recaudo.HistorialConfiguracion (HistorialId, RecaudadorId, Tipo, Detalle, UsuarioId, FechaHora)
    VALUES (NEWID(), @RecaudadorId, N'Configuracion', ISNULL(@Detalle, N'Configuracion de recaudador'), @UsuarioId, SYSUTCDATETIME());
END;
GO

CREATE OR ALTER PROCEDURE recaudo.sp_recaudo_asignar_grupo
    @RecaudadorId uniqueidentifier,
    @GrupoId uniqueidentifier,
    @Porcentaje int,
    @UsuarioId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    IF @Porcentaje < 1 OR @Porcentaje > 100
        THROW 50011, N'El porcentaje debe estar entre 1 y 100.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE UsuarioId = @RecaudadorId AND Rol = N'Recaudador')
        THROW 50010, N'El usuario no es un recaudador.', 1;
    IF EXISTS (
        SELECT 1 FROM recaudo.AsignacionesGrupo
        WHERE GrupoId = @GrupoId AND Estado = N'Activa' AND RecaudadorId <> @RecaudadorId)
        THROW 50012, N'El grupo ya esta asignado a otro recaudador.', 1;

    DECLARE @Ahora datetime2 = SYSUTCDATETIME();
    DECLARE @AsignacionId uniqueidentifier;
    SELECT @AsignacionId = AsignacionId
    FROM recaudo.AsignacionesGrupo
    WHERE GrupoId = @GrupoId AND RecaudadorId = @RecaudadorId AND Estado = N'Activa';

    IF @AsignacionId IS NULL
    BEGIN
        INSERT INTO recaudo.AsignacionesGrupo (AsignacionId, RecaudadorId, GrupoId, Porcentaje, Estado, FechaCreacion, FechaModificacion)
        VALUES (NEWID(), @RecaudadorId, @GrupoId, @Porcentaje, N'Activa', @Ahora, @Ahora);
    END
    ELSE
    BEGIN
        UPDATE recaudo.AsignacionesGrupo
        SET Porcentaje = @Porcentaje, FechaModificacion = @Ahora
        WHERE AsignacionId = @AsignacionId;
    END;

    INSERT INTO recaudo.HistorialConfiguracion (HistorialId, RecaudadorId, Tipo, Detalle, UsuarioId, FechaHora)
    VALUES (NEWID(), @RecaudadorId, N'AsignarGrupo', CONVERT(nvarchar(36), @GrupoId), @UsuarioId, @Ahora);
END;
GO

CREATE OR ALTER PROCEDURE recaudo.sp_recaudo_asignar_vendedor
    @RecaudadorId uniqueidentifier,
    @VendedorId uniqueidentifier,
    @Porcentaje int,
    @UsuarioId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    IF @Porcentaje < 1 OR @Porcentaje > 100
        THROW 50011, N'El porcentaje debe estar entre 1 y 100.', 1;
    IF EXISTS (SELECT 1 FROM dbo.UsuariosGrupos WHERE UsuarioId = @VendedorId)
        THROW 50013, N'El vendedor pertenece a un grupo. Asigne el grupo.', 1;
    IF EXISTS (
        SELECT 1 FROM recaudo.AsignacionesVendedor
        WHERE VendedorId = @VendedorId AND Estado = N'Activa' AND RecaudadorId <> @RecaudadorId)
        THROW 50014, N'El vendedor ya esta asignado a otro recaudador.', 1;

    DECLARE @Ahora datetime2 = SYSUTCDATETIME();
    DECLARE @AsignacionId uniqueidentifier;
    SELECT @AsignacionId = AsignacionId
    FROM recaudo.AsignacionesVendedor
    WHERE VendedorId = @VendedorId AND Estado = N'Activa';

    IF @AsignacionId IS NULL
    BEGIN
        INSERT INTO recaudo.AsignacionesVendedor (AsignacionId, RecaudadorId, VendedorId, Porcentaje, Estado, FechaCreacion, FechaModificacion)
        VALUES (NEWID(), @RecaudadorId, @VendedorId, @Porcentaje, N'Activa', @Ahora, @Ahora);
    END
    ELSE
    BEGIN
        UPDATE recaudo.AsignacionesVendedor
        SET Porcentaje = @Porcentaje, RecaudadorId = @RecaudadorId, FechaModificacion = @Ahora
        WHERE AsignacionId = @AsignacionId;
    END;

    INSERT INTO recaudo.HistorialConfiguracion (HistorialId, RecaudadorId, Tipo, Detalle, UsuarioId, FechaHora)
    VALUES (NEWID(), @RecaudadorId, N'AsignarVendedor', CONVERT(nvarchar(36), @VendedorId), @UsuarioId, @Ahora);
END;
GO

CREATE OR ALTER PROCEDURE recaudo.sp_recaudo_configurar_porcentaje
    @GrupoId uniqueidentifier = NULL,
    @VendedorId uniqueidentifier = NULL,
    @Porcentaje int,
    @UsuarioId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    IF @Porcentaje < 1 OR @Porcentaje > 100
        THROW 50011, N'El porcentaje debe estar entre 1 y 100.', 1;
    DECLARE @Ahora datetime2 = SYSUTCDATETIME();
    IF @GrupoId IS NOT NULL
    BEGIN
        UPDATE recaudo.AsignacionesGrupo
        SET Porcentaje = @Porcentaje, FechaModificacion = @Ahora
        WHERE GrupoId = @GrupoId AND Estado = N'Activa';
        IF @@ROWCOUNT = 0
            THROW 50015, N'El grupo no tiene una asignacion activa.', 1;
    END
    ELSE IF @VendedorId IS NOT NULL
    BEGIN
        UPDATE recaudo.AsignacionesVendedor
        SET Porcentaje = @Porcentaje, FechaModificacion = @Ahora
        WHERE VendedorId = @VendedorId AND Estado = N'Activa';
        IF @@ROWCOUNT = 0
            THROW 50016, N'El vendedor no tiene una asignacion activa.', 1;
    END
    ELSE
        THROW 50017, N'Debe indicar un grupo o un vendedor.', 1;
END;
GO

CREATE OR ALTER PROCEDURE recaudo.sp_recaudo_consultar_grupos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        g.GrupoId,
        g.Nombre,
        ISNULL(a.Porcentaje, 0) AS Porcentaje,
        a.RecaudadorId,
        r.NombreCompleto AS RecaudadorNombre,
        CAST(CASE WHEN a.AsignacionId IS NULL THEN 1 ELSE 0 END AS bit) AS SinConfigurar
    FROM dbo.Grupos g
    LEFT JOIN recaudo.AsignacionesGrupo a ON a.GrupoId = g.GrupoId AND a.Estado = N'Activa'
    LEFT JOIN dbo.Usuarios r ON r.UsuarioId = a.RecaudadorId
    ORDER BY g.Nombre;
END;
GO

CREATE OR ALTER PROCEDURE recaudo.sp_recaudo_consultar_vendedores
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        u.UsuarioId AS VendedorId,
        u.NombreCompleto AS Nombre,
        ISNULL(a.Porcentaje, 0) AS Porcentaje,
        a.RecaudadorId,
        r.NombreCompleto AS RecaudadorNombre
    FROM dbo.Usuarios u
    LEFT JOIN recaudo.AsignacionesVendedor a ON a.VendedorId = u.UsuarioId AND a.Estado = N'Activa'
    LEFT JOIN dbo.Usuarios r ON r.UsuarioId = a.RecaudadorId
    WHERE u.Rol = N'Vendedor'
      AND NOT EXISTS (SELECT 1 FROM dbo.UsuariosGrupos ug WHERE ug.UsuarioId = u.UsuarioId)
    ORDER BY u.NombreCompleto;
END;
GO

CREATE OR ALTER PROCEDURE recaudo.sp_recaudo_generar_obligaciones
    @VendedorId uniqueidentifier,
    @Fecha date = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @Fecha IS NULL
        SET @Fecha = CAST(DATEADD(HOUR, -5, SYSUTCDATETIME()) AS date);

    DECLARE @GrupoId uniqueidentifier;
    DECLARE @RecaudadorId uniqueidentifier;
    DECLARE @Porcentaje int;

    SELECT TOP (1) @GrupoId = GrupoId FROM dbo.UsuariosGrupos WHERE UsuarioId = @VendedorId;
    IF @GrupoId IS NOT NULL
        SELECT @RecaudadorId = RecaudadorId, @Porcentaje = Porcentaje
        FROM recaudo.AsignacionesGrupo
        WHERE GrupoId = @GrupoId AND Estado = N'Activa';
    ELSE
        SELECT @RecaudadorId = RecaudadorId, @Porcentaje = Porcentaje
        FROM recaudo.AsignacionesVendedor
        WHERE VendedorId = @VendedorId AND Estado = N'Activa';

    IF @RecaudadorId IS NULL OR ISNULL(@Porcentaje, 0) <= 0
        RETURN;

    DECLARE @InicioUtc datetime2 = DATEADD(HOUR, 5, CAST(@Fecha AS datetime2));
    DECLARE @FinUtc datetime2 = DATEADD(DAY, 1, @InicioUtc);
    DECLARE @Total decimal(18, 2) = (
        SELECT ISNULL(SUM(Total), 0)
        FROM dbo.Ventas
        WHERE UsuarioId = @VendedorId
          AND FechaVenta >= @InicioUtc
          AND FechaVenta < @FinUtc);
    DECLARE @Generado decimal(18, 2) = CEILING(@Total * @Porcentaje / 100.0);
    DECLARE @Anterior decimal(18, 2) = (
        SELECT ISNULL(SUM(ValorGenerado), 0)
        FROM recaudo.Obligaciones
        WHERE VendedorId = @VendedorId AND Fecha < @Fecha)
        - (
        SELECT ISNULL(SUM(Valor), 0)
        FROM recaudo.Pagos
        WHERE VendedorId = @VendedorId AND FechaHora < @InicioUtc);
    IF @Anterior < 0 SET @Anterior = 0;

    MERGE recaudo.Obligaciones AS destino
    USING (SELECT @VendedorId AS VendedorId, @Fecha AS Fecha) AS origen
        ON destino.VendedorId = origen.VendedorId AND destino.Fecha = origen.Fecha
    WHEN MATCHED THEN
        UPDATE SET TotalVendido = @Total, Porcentaje = @Porcentaje, ValorGenerado = @Generado
    WHEN NOT MATCHED THEN
        INSERT (ObligacionId, VendedorId, RecaudadorId, Fecha, TotalVendido, Porcentaje, ValorGenerado, SaldoAnterior, FechaGeneracion)
        VALUES (NEWID(), @VendedorId, @RecaudadorId, @Fecha, @Total, @Porcentaje, @Generado, @Anterior, SYSUTCDATETIME());
END;
GO

CREATE OR ALTER PROCEDURE recaudo.sp_recaudo_registrar_pago
    @RecaudadorId uniqueidentifier,
    @VendedorId uniqueidentifier,
    @Valor decimal(18, 2),
    @ClaveIdempotencia nvarchar(80)
AS
BEGIN
    SET NOCOUNT ON;
    IF @Valor <= 0
        THROW 50018, N'El valor del pago no es valido.', 1;
    IF EXISTS (SELECT 1 FROM recaudo.Pagos WHERE ClaveIdempotencia = @ClaveIdempotencia)
    BEGIN
        SELECT PagoId, SaldoResultante, CAST(1 AS bit) AS Reintento, FechaHora
        FROM recaudo.Pagos
        WHERE ClaveIdempotencia = @ClaveIdempotencia;
        RETURN;
    END;

    DECLARE @Fecha date = CAST(DATEADD(HOUR, -5, SYSUTCDATETIME()) AS date);
    EXEC recaudo.sp_recaudo_generar_obligaciones @VendedorId, @Fecha;

    DECLARE @ObligacionId uniqueidentifier;
    DECLARE @Pendiente decimal(18, 2);
    SELECT @ObligacionId = ObligacionId,
           @Pendiente = ValorGenerado + SaldoAnterior - ISNULL((
               SELECT SUM(p.Valor) FROM recaudo.Pagos p WHERE p.ObligacionId = o.ObligacionId), 0)
    FROM recaudo.Obligaciones o
    WHERE o.VendedorId = @VendedorId AND o.Fecha = @Fecha AND o.RecaudadorId = @RecaudadorId;

    IF @ObligacionId IS NULL
        THROW 50019, N'El vendedor no esta asignado a este recaudador.', 1;
    IF @Valor > @Pendiente
        THROW 50020, N'El valor no puede ser mayor que el pendiente.', 1;

    DECLARE @PagoId uniqueidentifier = NEWID();
    DECLARE @Saldo decimal(18, 2) = @Pendiente - @Valor;
    DECLARE @Ahora datetime2 = SYSUTCDATETIME();
    INSERT INTO recaudo.Pagos (PagoId, ObligacionId, RecaudadorId, VendedorId, Valor, SaldoResultante, ClaveIdempotencia, FechaHora)
    VALUES (@PagoId, @ObligacionId, @RecaudadorId, @VendedorId, @Valor, @Saldo, @ClaveIdempotencia, @Ahora);

    INSERT INTO recaudo.TirillasCobro (TirillaId, PagoId, RecaudadorNombre, VendedorNombre, FechaHora, ValorRecibido, SaldoRestante)
    SELECT NEWID(), @PagoId, r.NombreCompleto, v.NombreCompleto, @Ahora, @Valor, @Saldo
    FROM dbo.Usuarios r
    CROSS JOIN dbo.Usuarios v
    WHERE r.UsuarioId = @RecaudadorId AND v.UsuarioId = @VendedorId;

    INSERT INTO recaudo.Auditoria (AuditoriaId, UsuarioId, FechaHora, TipoOperacion, Valor, ObligacionId, MovimientoId)
    VALUES (NEWID(), @RecaudadorId, @Ahora, N'Pago', @Valor, @ObligacionId, @PagoId);

    SELECT @PagoId AS PagoId, @Saldo AS SaldoResultante, CAST(0 AS bit) AS Reintento, @Ahora AS FechaHora;
END;
GO

CREATE OR ALTER PROCEDURE recaudo.sp_recaudo_consultar_historial
    @RecaudadorId uniqueidentifier = NULL,
    @VendedorId uniqueidentifier = NULL,
    @GrupoId uniqueidentifier = NULL,
    @Desde date,
    @Hasta date
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @InicioUtc datetime2 = DATEADD(HOUR, 5, CAST(@Desde AS datetime2));
    DECLARE @FinUtc datetime2 = DATEADD(HOUR, 5, CAST(DATEADD(DAY, 1, @Hasta) AS datetime2));
    SELECT
        p.FechaHora,
        r.NombreCompleto AS Recaudador,
        v.NombreCompleto AS Vendedor,
        ISNULL(g.Nombre, N'Sin grupo') AS Grupo,
        p.Valor AS ValorRecibido,
        p.SaldoResultante,
        CASE WHEN p.SaldoResultante = 0 THEN N'Al dia' ELSE N'Deudado' END AS Estado
    FROM recaudo.Pagos p
    INNER JOIN dbo.Usuarios r ON r.UsuarioId = p.RecaudadorId
    INNER JOIN dbo.Usuarios v ON v.UsuarioId = p.VendedorId
    LEFT JOIN dbo.UsuariosGrupos ug ON ug.UsuarioId = p.VendedorId
    LEFT JOIN dbo.Grupos g ON g.GrupoId = ug.GrupoId
    WHERE p.FechaHora >= @InicioUtc
      AND p.FechaHora < @FinUtc
      AND (@RecaudadorId IS NULL OR p.RecaudadorId = @RecaudadorId)
      AND (@VendedorId IS NULL OR p.VendedorId = @VendedorId)
      AND (@GrupoId IS NULL OR ug.GrupoId = @GrupoId)
    ORDER BY p.FechaHora DESC;
END;
GO

