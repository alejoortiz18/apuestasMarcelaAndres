/* ============================================================
   NEW RICH - Base de Datos Completa
   ============================================================
   Nombre BD : NewRich
   Motor     : Microsoft SQL Server 2025 (Enterprise Developer)
   Autor     : Plan de Desarrollo NewRich
   Fecha     : 2026-09-08
   Descripcion: Script completo de creacion de la base de datos,
                tablas, indices, restricciones, procedimientos
                almacenados y datos maestros para las apuestas A1-A6.
   ============================================================ */

PRINT '==============================================';
PRINT 'NEW RICH - CREACION DE BASE DE DATOS';
PRINT '==============================================';

/* ============================================================
   1. CREACION DE LA BASE DE DATOS
   ============================================================ */
IF DB_ID(N'NewRich') IS NULL
BEGIN
    CREATE DATABASE NewRich;
    PRINT 'Base de datos NewRich creada correctamente.';
END
ELSE
BEGIN
    PRINT 'La base de datos NewRich ya existe.';
END
GO

USE [NewRich];
GO

/* ============================================================
   2. TABLAS DE IDENTIDAD Y ACCESO
   ============================================================ */

/* 2.1. Roles */
IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles (
        RolId           INT IDENTITY(1,1) NOT NULL,
        Nombre          NVARCHAR(50)      NOT NULL,
        Descripcion     NVARCHAR(255)     NULL,
        CONSTRAINT PK_Roles PRIMARY KEY (RolId),
        CONSTRAINT UQ_Roles_Nombre UNIQUE (Nombre)
    );
    PRINT 'Tabla Roles creada.';
END
GO

/* 2.2. Usuarios */
IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuarios (
        UsuarioId           UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        NombreCompleto      NVARCHAR(200)    NOT NULL,
        Usuario             NVARCHAR(50)     NOT NULL,
        Alias               NVARCHAR(50)     NULL,
        Documento           NVARCHAR(20)     NULL,
        Celular             NVARCHAR(20)     NULL,
        Email               NVARCHAR(100)    NULL,
        PasswordHash        NVARCHAR(200)    NOT NULL,
        PasswordSalt        NVARCHAR(100)    NOT NULL,
        Rol                 NVARCHAR(20)     NOT NULL DEFAULT 'Vendedor', -- Administrador, Vendedor, Observador
        Estado              NVARCHAR(20)     NOT NULL DEFAULT 'Activo',   -- Activo, Inactivo
        EstadoValidado      BIT              NOT NULL DEFAULT 1,          -- true = debe cambiar password
        EstadoBloqueado     BIT              NOT NULL DEFAULT 0,          -- true = bloqueado por 3 intentos
        IntentosFallidos    INT              NOT NULL DEFAULT 0,
        FechaCreacion       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        FechaUltimoAcceso   DATETIME2        NULL,
        CONSTRAINT PK_Usuarios PRIMARY KEY (UsuarioId),
        CONSTRAINT UQ_Usuarios_Usuario UNIQUE (Usuario),
        CONSTRAINT UQ_Usuarios_Documento UNIQUE (Documento),
        CONSTRAINT CK_Usuarios_Rol CHECK (Rol IN ('Administrador', 'Vendedor', 'Observador')),
        CONSTRAINT CK_Usuarios_Estado CHECK (Estado IN ('Activo', 'Inactivo'))
    );
    PRINT 'Tabla Usuarios creada.';
END
GO

/* 2.3. UsuariosRoles */
IF OBJECT_ID(N'dbo.UsuariosRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsuariosRoles (
        UsuarioId   UNIQUEIDENTIFIER NOT NULL,
        RolId       INT              NOT NULL,
        CONSTRAINT PK_UsuariosRoles PRIMARY KEY (UsuarioId, RolId),
        CONSTRAINT FK_UsuariosRoles_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT FK_UsuariosRoles_Roles FOREIGN KEY (RolId) REFERENCES dbo.Roles(RolId)
    );
    PRINT 'Tabla UsuariosRoles creada.';
END
GO

/* 2.4. Grupos */
IF OBJECT_ID(N'dbo.Grupos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Grupos (
        GrupoId         UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        Nombre          NVARCHAR(100)    NOT NULL,
        Descripcion     NVARCHAR(255)    NULL,
        FechaCreacion   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Grupos PRIMARY KEY (GrupoId),
        CONSTRAINT UQ_Grupos_Nombre UNIQUE (Nombre)
    );
    PRINT 'Tabla Grupos creada.';
END
GO

/* 2.5. UsuariosGrupos (solo vendedores pertenecen a grupos, RN-046) */
IF OBJECT_ID(N'dbo.UsuariosGrupos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsuariosGrupos (
        UsuarioId   UNIQUEIDENTIFIER NOT NULL,
        GrupoId     UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_UsuariosGrupos PRIMARY KEY (UsuarioId, GrupoId),
        CONSTRAINT FK_UsuariosGrupos_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT FK_UsuariosGrupos_Grupos FOREIGN KEY (GrupoId) REFERENCES dbo.Grupos(GrupoId)
    );
    PRINT 'Tabla UsuariosGrupos creada.';
END
GO

/* 2.6. IntentosFallidos */
IF OBJECT_ID(N'dbo.IntentosFallidos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IntentosFallidos (
        IntentoId       INT IDENTITY(1,1) NOT NULL,
        UsuarioId       UNIQUEIDENTIFIER NOT NULL,
        FechaIntento    DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        Exitoso         BIT              NOT NULL DEFAULT 0,
        CONSTRAINT PK_IntentosFallidos PRIMARY KEY (IntentoId),
        CONSTRAINT FK_IntentosFallidos_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId)
    );
    CREATE INDEX IX_IntentosFallidos_UsuarioId ON dbo.IntentosFallidos(UsuarioId);
    PRINT 'Tabla IntentosFallidos creada.';
END
GO

/* ============================================================
   3. TABLAS DE DISPOSITIVOS
   ============================================================ */

/* 3.1. Dispositivos */
IF OBJECT_ID(N'dbo.Dispositivos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Dispositivos (
        DispositivoId            UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        CodigoDispositivo        NVARCHAR(20)     NOT NULL,
        Tipo                     NVARCHAR(20)     NOT NULL DEFAULT 'Vendedor', -- Vendedor, Observador
        Estado                   NVARCHAR(20)     NOT NULL DEFAULT 'Activo',   -- Activo, Inactivo
        Modelo                   NVARCHAR(100)    NULL,
        NumeroSerie              NVARCHAR(100)    NULL,
        CapacidadCodigosOffline  INT              NOT NULL DEFAULT 3000,      -- 3000 a 5000
        FechaRegistro            DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Dispositivos PRIMARY KEY (DispositivoId),
        CONSTRAINT UQ_Dispositivos_Codigo UNIQUE (CodigoDispositivo),
        CONSTRAINT UQ_Dispositivos_NumeroSerie UNIQUE (NumeroSerie),
        CONSTRAINT CK_Dispositivos_Tipo CHECK (Tipo IN ('Vendedor', 'Observador')),
        CONSTRAINT CK_Dispositivos_Estado CHECK (Estado IN ('Activo', 'Inactivo')),
        CONSTRAINT CK_Dispositivos_Capacidad CHECK (CapacidadCodigosOffline BETWEEN 3000 AND 5000)
    );
    PRINT 'Tabla Dispositivos creada.';
END
GO

/* 3.2. DispositivosUsuarios */
IF OBJECT_ID(N'dbo.DispositivosUsuarios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DispositivosUsuarios (
        DispositivoId   UNIQUEIDENTIFIER NOT NULL,
        UsuarioId       UNIQUEIDENTIFIER NOT NULL,
        FechaAsociacion DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        Activo          BIT              NOT NULL DEFAULT 1,
        CONSTRAINT PK_DispositivosUsuarios PRIMARY KEY (DispositivoId, UsuarioId),
        CONSTRAINT FK_DispositivosUsuarios_Dispositivos FOREIGN KEY (DispositivoId) REFERENCES dbo.Dispositivos(DispositivoId),
        CONSTRAINT FK_DispositivosUsuarios_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId)
    );
    PRINT 'Tabla DispositivosUsuarios creada.';
END
GO

/* 2.7. Sesiones */
IF OBJECT_ID(N'dbo.Sesiones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sesiones (
        SesionId        UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        UsuarioId       UNIQUEIDENTIFIER NOT NULL,
        DispositivoId   UNIQUEIDENTIFIER NULL,
        Token           NVARCHAR(500)    NOT NULL,
        FechaInicio     DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        FechaExpiracion DATETIME2        NOT NULL,
        Activa          BIT              NOT NULL DEFAULT 1,
        CONSTRAINT PK_Sesiones PRIMARY KEY (SesionId),
        CONSTRAINT FK_Sesiones_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT FK_Sesiones_Dispositivos FOREIGN KEY (DispositivoId) REFERENCES dbo.Dispositivos(DispositivoId)
    );
    CREATE INDEX IX_Sesiones_Token ON dbo.Sesiones(Token);
    CREATE INDEX IX_Sesiones_UsuarioId ON dbo.Sesiones(UsuarioId);
    PRINT 'Tabla Sesiones creada.';
END
GO

/* ============================================================
   4. TABLAS DE JUEGO Y VENTAS
   ============================================================ */

/* 4.1. Loterias */
IF OBJECT_ID(N'dbo.Loterias', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Loterias (
        LoteriaId       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        Nombre          NVARCHAR(100)    NOT NULL,
        Estado          NVARCHAR(20)     NOT NULL DEFAULT 'Activo',
        FechaCreacion   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Loterias PRIMARY KEY (LoteriaId),
        CONSTRAINT UQ_Loterias_Nombre UNIQUE (Nombre),
        CONSTRAINT CK_Loterias_Estado CHECK (Estado IN ('Activo', 'Inactivo'))
    );
    PRINT 'Tabla Loterias creada.';
END
GO

/* 4.2. Ventas */
IF OBJECT_ID(N'dbo.Ventas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ventas (
        VentaId             UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        UsuarioId           UNIQUEIDENTIFIER NOT NULL,
        DispositivoId       UNIQUEIDENTIFIER NULL,
        FechaVenta          DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        Total               DECIMAL(18,2)    NOT NULL,
        TipoApuesta         NVARCHAR(20)     NOT NULL, -- COMBINADO, INDIVIDUAL
        EstadoSincronizacion NVARCHAR(20)    NULL,     -- Online, OfflinePendiente, OfflineRegistrado
        IdempotencyKey      NVARCHAR(100)    NULL,     -- Control de duplicidad (RS-084)
        FechaSincronizacion DATETIME2        NULL,
        CONSTRAINT PK_Ventas PRIMARY KEY (VentaId),
        CONSTRAINT FK_Ventas_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT FK_Ventas_Dispositivos FOREIGN KEY (DispositivoId) REFERENCES dbo.Dispositivos(DispositivoId),
        CONSTRAINT CK_Ventas_TipoApuesta CHECK (TipoApuesta IN ('COMBINADO', 'INDIVIDUAL'))
    );
    CREATE INDEX IX_Ventas_FechaVenta_UsuarioId ON dbo.Ventas(FechaVenta, UsuarioId);
    CREATE INDEX IX_Ventas_IdempotencyKey ON dbo.Ventas(IdempotencyKey);
    PRINT 'Tabla Ventas creada.';
END
GO

/* 4.3. Boletos */
IF OBJECT_ID(N'dbo.Boletos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Boletos (
        BoletoId            UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        VentaId             UNIQUEIDENTIFIER NOT NULL,
        CodigoPublico       CHAR(7)          NOT NULL,
        ClaveValidacionHash NVARCHAR(200)    NOT NULL,
        QrCifrado           NVARCHAR(MAX)    NOT NULL DEFAULT '',
        EstadoBoleto        NVARCHAR(30)     NOT NULL DEFAULT 'Jugado', -- Por jugar, Jugado, Ganador, No ganador, Vencido, Pagado/cobrado, Premio entregado
        EstadoDelPremio     NVARCHAR(30)     NULL,     -- Vigente, PremioEntregado, Rechazado
        FechaEntregaPremio  DATETIME2        NULL,
        CasoGanadorId       UNIQUEIDENTIFIER NULL,     -- FK a CasosGanadores (RS-116)
        FechaCreacion       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        VigenciaDias        INT              NOT NULL DEFAULT 30,
        CONSTRAINT PK_Boletos PRIMARY KEY (BoletoId),
        CONSTRAINT UQ_Boletos_CodigoPublico UNIQUE (CodigoPublico),
        CONSTRAINT FK_Boletos_Ventas FOREIGN KEY (VentaId) REFERENCES dbo.Ventas(VentaId),
        CONSTRAINT CK_Boletos_EstadoBoleto CHECK (EstadoBoleto IN ('Por jugar', 'Jugado', 'Ganador', 'No ganador', 'Vencido', 'Pagado/cobrado', 'Premio entregado'))
    );
    CREATE INDEX IX_Boletos_EstadoBoleto ON dbo.Boletos(EstadoBoleto);
    CREATE INDEX IX_Boletos_VentaId ON dbo.Boletos(VentaId);
    PRINT 'Tabla Boletos creada.';
END
GO

/* 4.4. Juegos */
IF OBJECT_ID(N'dbo.Juegos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Juegos (
        JuegoId     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        BoletoId    UNIQUEIDENTIFIER NOT NULL,
        Numero      CHAR(4)          NOT NULL,
        Valor       DECIMAL(18,2)    NOT NULL,
        TipoJuego   NVARCHAR(20)     NOT NULL, -- COMBINADA, INDIVIDUAL
        CONSTRAINT PK_Juegos PRIMARY KEY (JuegoId),
        CONSTRAINT FK_Juegos_Boletos FOREIGN KEY (BoletoId) REFERENCES dbo.Boletos(BoletoId),
        CONSTRAINT CK_Juegos_TipoJuego CHECK (TipoJuego IN ('COMBINADA', 'INDIVIDUAL'))
    );
    CREATE INDEX IX_Juegos_Numero ON dbo.Juegos(Numero);
    CREATE INDEX IX_Juegos_BoletoId ON dbo.Juegos(BoletoId);
    PRINT 'Tabla Juegos creada.';
END
GO

/* 4.5. JuegoLoteria */
IF OBJECT_ID(N'dbo.JuegoLoteria', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.JuegoLoteria (
        JuegoId     UNIQUEIDENTIFIER NOT NULL,
        LoteriaId   UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_JuegoLoteria PRIMARY KEY (JuegoId, LoteriaId),
        CONSTRAINT FK_JuegoLoteria_Juegos FOREIGN KEY (JuegoId) REFERENCES dbo.Juegos(JuegoId),
        CONSTRAINT FK_JuegoLoteria_Loterias FOREIGN KEY (LoteriaId) REFERENCES dbo.Loterias(LoteriaId)
    );
    PRINT 'Tabla JuegoLoteria creada.';
END
GO

/* 4.6. EstadosBoleto (catálogo) */
IF OBJECT_ID(N'dbo.EstadosBoleto', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EstadosBoleto (
        EstadoBoletoId  INT IDENTITY(1,1) NOT NULL,
        Nombre          NVARCHAR(30)      NOT NULL,
        CONSTRAINT PK_EstadosBoleto PRIMARY KEY (EstadoBoletoId),
        CONSTRAINT UQ_EstadosBoleto_Nombre UNIQUE (Nombre)
    );
    PRINT 'Tabla EstadosBoleto creada.';
END
GO

/* 4.7. NumerosGanadores */
IF OBJECT_ID(N'dbo.NumerosGanadores', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NumerosGanadores (
        NumeroGanadorId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        LoteriaId       UNIQUEIDENTIFIER NOT NULL,
        FechaJuego      DATE             NOT NULL,
        Numero          CHAR(4)          NOT NULL,
        FechaRegistro   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_NumerosGanadores PRIMARY KEY (NumeroGanadorId),
        CONSTRAINT UQ_NumerosGanadores_FechaLoteria UNIQUE (FechaJuego, LoteriaId), -- Restriccion critica (RS-013)
        CONSTRAINT FK_NumerosGanadores_Loterias FOREIGN KEY (LoteriaId) REFERENCES dbo.Loterias(LoteriaId)
    );
    PRINT 'Tabla NumerosGanadores creada.';
END
GO

/* ============================================================
   5. TABLAS DE SEGURIDAD DE BOLETO
   ============================================================ */

/* 5.1. ClavesValidacionBoleto */
IF OBJECT_ID(N'dbo.ClavesValidacionBoleto', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClavesValidacionBoleto (
        ClaveId               UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        BoletoId              UNIQUEIDENTIFIER NOT NULL,
        ClaveHash             NVARCHAR(200)    NOT NULL,
        Version               INT              NOT NULL DEFAULT 1,
        IdentificadorClave    UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        FechaCreacion         DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_ClavesValidacionBoleto PRIMARY KEY (ClaveId),
        CONSTRAINT UQ_ClavesValidacionBoleto_BoletoId UNIQUE (BoletoId),
        CONSTRAINT UQ_ClavesValidacionBoleto_Identificador UNIQUE (IdentificadorClave),
        CONSTRAINT FK_ClavesValidacionBoleto_Boletos FOREIGN KEY (BoletoId) REFERENCES dbo.Boletos(BoletoId)
    );
    PRINT 'Tabla ClavesValidacionBoleto creada.';
END
GO

/* 5.2. CodigosPreventaOffline (A5) */
IF OBJECT_ID(N'dbo.CodigosPreventaOffline', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CodigosPreventaOffline (
        CodigoId             UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        ConsecutivoUnico     NVARCHAR(30)     NOT NULL,  -- Ej: NR-000001, unico e inmutable (RS-089, RS-100)
        UsuarioId            UNIQUEIDENTIFIER NOT NULL,
        DispositivoId        UNIQUEIDENTIFIER NOT NULL,
        PayloadCifrado       VARBINARY(MAX)   NOT NULL,
        EstadoDelCodigo      NVARCHAR(20)     NOT NULL DEFAULT 'Generado', -- Generado, Descargado, Utilizado, Registrado
        FechaCreacion        DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        FechaDescarga        DATETIME2        NULL,
        FechaVentaOffline    DATETIME2        NULL,
        FechaRegistro        DATETIME2        NULL,
        AdminQueRegistro     UNIQUEIDENTIFIER NULL,
        VentaId              UNIQUEIDENTIFIER NULL,
        CONSTRAINT PK_CodigosPreventaOffline PRIMARY KEY (CodigoId),
        CONSTRAINT UQ_CodigosPreventaOffline_Consecutivo UNIQUE (ConsecutivoUnico),
        CONSTRAINT FK_CodigosPreventaOffline_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT FK_CodigosPreventaOffline_Dispositivos FOREIGN KEY (DispositivoId) REFERENCES dbo.Dispositivos(DispositivoId),
        CONSTRAINT FK_CodigosPreventaOffline_Admin FOREIGN KEY (AdminQueRegistro) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT FK_CodigosPreventaOffline_Ventas FOREIGN KEY (VentaId) REFERENCES dbo.Ventas(VentaId),
        CONSTRAINT CK_CodigosPreventaOffline_Estado CHECK (EstadoDelCodigo IN ('Generado', 'Descargado', 'Utilizado', 'Registrado'))
    );
    CREATE INDEX IX_CodigosPreventaOffline_Estado_Usuario_Dispositivo ON dbo.CodigosPreventaOffline(EstadoDelCodigo, UsuarioId, DispositivoId);
    CREATE INDEX IX_CodigosPreventaOffline_VentaId ON dbo.CodigosPreventaOffline(VentaId);
    PRINT 'Tabla CodigosPreventaOffline creada.';
END
GO

/* ============================================================
   6. TABLAS DE CONFIGURACION Y OPERACION
   ============================================================ */

/* 6.1. Configuraciones */
IF OBJECT_ID(N'dbo.Configuraciones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Configuraciones (
        ConfiguracionId      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        Clave                NVARCHAR(100)    NOT NULL,
        Valor                NVARCHAR(MAX)    NOT NULL,
        FechaActualizacion   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Configuraciones PRIMARY KEY (ConfiguracionId),
        CONSTRAINT UQ_Configuraciones_Clave UNIQUE (Clave)
    );
    PRINT 'Tabla Configuraciones creada.';
END
ELSE IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Configuraciones')
      AND name = N'Valor'
      AND max_length <> -1
)
BEGIN
    ALTER TABLE dbo.Configuraciones ALTER COLUMN Valor NVARCHAR(MAX) NOT NULL;
    PRINT 'Columna Configuraciones.Valor ampliada a NVARCHAR(MAX).';
END
GO

/* 6.2. ConfiguracionesTipoApuesta (RS-036, RS-040A) */
IF OBJECT_ID(N'dbo.ConfiguracionesTipoApuesta', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConfiguracionesTipoApuesta (
        ConfiguracionTipoApuestaId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        TipoApuesta                NVARCHAR(20)     NOT NULL, -- COMBINADO, INDIVIDUAL
        Maximo                     INT              NOT NULL, -- Max juegos/líneas
        FechaActualizacion         DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_ConfiguracionesTipoApuesta PRIMARY KEY (ConfiguracionTipoApuestaId),
        CONSTRAINT UQ_ConfiguracionesTipoApuesta_Tipo UNIQUE (TipoApuesta),
        CONSTRAINT CK_ConfiguracionesTipoApuesta_Tipo CHECK (TipoApuesta IN ('COMBINADO', 'INDIVIDUAL')),
        CONSTRAINT CK_ConfiguracionesTipoApuesta_Maximo CHECK (Maximo > 0)
    );
    PRINT 'Tabla ConfiguracionesTipoApuesta creada.';
END
GO

/* 6.3. Notificaciones */
IF OBJECT_ID(N'dbo.Notificaciones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notificaciones (
        NotificacionId  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        UsuarioId       UNIQUEIDENTIFIER NOT NULL,
        Tipo            NVARCHAR(30)     NOT NULL, -- RepeticionNumero, ValorAlto, CasoGanador, Sistema, Premio
        Mensaje         NVARCHAR(500)    NOT NULL,
        Leida           BIT              NOT NULL DEFAULT 0,
        FechaCreacion   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Notificaciones PRIMARY KEY (NotificacionId),
        CONSTRAINT FK_Notificaciones_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId)
    );
    CREATE INDEX IX_Notificaciones_UsuarioId_Leida ON dbo.Notificaciones(UsuarioId, Leida);
    PRINT 'Tabla Notificaciones creada.';
END
GO

/* 6.4. Sincronizaciones */
IF OBJECT_ID(N'dbo.Sincronizaciones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sincronizaciones (
        SincronizacionId     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        DispositivoId        UNIQUEIDENTIFIER NOT NULL,
        FechaSincronizacion  DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        Tipo                 NVARCHAR(30)     NOT NULL, -- CodigosOffline, HoraCierre, Datos
        Resultado            NVARCHAR(20)     NOT NULL, -- Exitosa, Fallida
        CONSTRAINT PK_Sincronizaciones PRIMARY KEY (SincronizacionId),
        CONSTRAINT FK_Sincronizaciones_Dispositivos FOREIGN KEY (DispositivoId) REFERENCES dbo.Dispositivos(DispositivoId)
    );
    CREATE INDEX IX_Sincronizaciones_DispositivoId ON dbo.Sincronizaciones(DispositivoId);
    PRINT 'Tabla Sincronizaciones creada.';
END
GO

/* ============================================================
   7. TABLAS DE SOPORTE (CHAT)
   ============================================================ */

/* 7.1. Conversaciones */
IF OBJECT_ID(N'dbo.Conversaciones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Conversaciones (
        ConversacionId      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        UsuarioIniciadorId  UNIQUEIDENTIFIER NOT NULL,
        UsuarioDestinoId    UNIQUEIDENTIFIER NOT NULL,
        FechaInicio         DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        FechaCierre         DATETIME2        NULL,
        Estado              NVARCHAR(20)     NOT NULL DEFAULT 'Abierta', -- Abierta, Cerrada
        CONSTRAINT PK_Conversaciones PRIMARY KEY (ConversacionId),
        CONSTRAINT FK_Conversaciones_Iniciador FOREIGN KEY (UsuarioIniciadorId) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT FK_Conversaciones_Destino FOREIGN KEY (UsuarioDestinoId) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT CK_Conversaciones_Estado CHECK (Estado IN ('Abierta', 'Cerrada'))
    );
    CREATE INDEX IX_Conversaciones_Iniciador ON dbo.Conversaciones(UsuarioIniciadorId);
    CREATE INDEX IX_Conversaciones_Destino ON dbo.Conversaciones(UsuarioDestinoId);
    PRINT 'Tabla Conversaciones creada.';
END
GO

/* 7.2. Mensajes */
IF OBJECT_ID(N'dbo.Mensajes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Mensajes (
        MensajeId       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        ConversacionId  UNIQUEIDENTIFIER NOT NULL,
        UsuarioEmisorId UNIQUEIDENTIFIER NOT NULL,
        Texto           NVARCHAR(2000)   NOT NULL,
        FechaEnvio      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        Permanente      BIT              NOT NULL DEFAULT 0, -- true = evidencia offline no se elimina
        CONSTRAINT PK_Mensajes PRIMARY KEY (MensajeId),
        CONSTRAINT FK_Mensajes_Conversaciones FOREIGN KEY (ConversacionId) REFERENCES dbo.Conversaciones(ConversacionId),
        CONSTRAINT FK_Mensajes_Usuarios FOREIGN KEY (UsuarioEmisorId) REFERENCES dbo.Usuarios(UsuarioId)
    );
    CREATE INDEX IX_Mensajes_ConversacionId ON dbo.Mensajes(ConversacionId);
    PRINT 'Tabla Mensajes creada.';
END
GO

/* 7.3. AdjuntosChat */
IF OBJECT_ID(N'dbo.AdjuntosChat', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdjuntosChat (
        AdjuntoId       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        MensajeId       UNIQUEIDENTIFIER NOT NULL,
        RutaArchivo     NVARCHAR(500)    NOT NULL,
        NombreOriginal  NVARCHAR(255)    NOT NULL,
        FechaCarga      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_AdjuntosChat PRIMARY KEY (AdjuntoId),
        CONSTRAINT FK_AdjuntosChat_Mensajes FOREIGN KEY (MensajeId) REFERENCES dbo.Mensajes(MensajeId)
    );
    CREATE INDEX IX_AdjuntosChat_MensajeId ON dbo.AdjuntosChat(MensajeId);
    PRINT 'Tabla AdjuntosChat creada.';
END
GO

/* ============================================================
   8. TABLAS DE PREMIOS (A6)
   ============================================================ */

/* 8.1. CasosGanadores */
IF OBJECT_ID(N'dbo.CasosGanadores', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CasosGanadores (
        CasoId               UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        BoletoId             UNIQUEIDENTIFIER NOT NULL,
        TicketCode           CHAR(7)          NOT NULL,
        Estado               NVARCHAR(20)     NOT NULL DEFAULT 'Reportado', -- Reportado, Validado, Asignado, EnProceso, Registrado, Rechazado
        FechaReporte         DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        FechaValidacionAdmin DATETIME2        NULL,
        FechaAsignacion      DATETIME2        NULL,
        FechaRegistro        DATETIME2        NULL,
        VendedorQueReporto   UNIQUEIDENTIFIER NOT NULL,
        AdminQueValido       UNIQUEIDENTIFIER NULL,
        AdminQueAsigno       UNIQUEIDENTIFIER NULL,
        ObservadorAsignado   UNIQUEIDENTIFIER NULL,
        CONSTRAINT PK_CasosGanadores PRIMARY KEY (CasoId),
        CONSTRAINT UQ_CasosGanadores_BoletoId UNIQUE (BoletoId), -- Un boleto solo puede tener un caso
        CONSTRAINT FK_CasosGanadores_Boletos FOREIGN KEY (BoletoId) REFERENCES dbo.Boletos(BoletoId),
        CONSTRAINT FK_CasosGanadores_Vendedor FOREIGN KEY (VendedorQueReporto) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT FK_CasosGanadores_AdminValido FOREIGN KEY (AdminQueValido) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT FK_CasosGanadores_AdminAsigno FOREIGN KEY (AdminQueAsigno) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT FK_CasosGanadores_Observador FOREIGN KEY (ObservadorAsignado) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT CK_CasosGanadores_Estado CHECK (Estado IN ('Reportado', 'Validado', 'Asignado', 'EnProceso', 'Registrado', 'Rechazado'))
    );
    CREATE INDEX IX_CasosGanadores_Estado ON dbo.CasosGanadores(Estado);
    CREATE INDEX IX_CasosGanadores_FechaReporte ON dbo.CasosGanadores(FechaReporte);
    PRINT 'Tabla CasosGanadores creada.';
END
GO

/* 8.2. EntregasGanadores */
IF OBJECT_ID(N'dbo.EntregasGanadores', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EntregasGanadores (
        EntregaId          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        CasoId             UNIQUEIDENTIFIER NOT NULL,
        NombreGanador      NVARCHAR(150)    NOT NULL,
        ApellidoGanador    NVARCHAR(150)    NOT NULL,
        NumeroContacto     NVARCHAR(30)     NOT NULL,
        LugarGano          NVARCHAR(255)    NOT NULL,
        NombreVendedor     NVARCHAR(200)    NOT NULL,
        ValorTotalGanado   DECIMAL(18,2)    NOT NULL,
        PersonaQueEntrega  UNIQUEIDENTIFIER NOT NULL,
        FechaEntrega       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_EntregasGanadores PRIMARY KEY (EntregaId),
        CONSTRAINT UQ_EntregasGanadores_CasoId UNIQUE (CasoId),
        CONSTRAINT FK_EntregasGanadores_Casos FOREIGN KEY (CasoId) REFERENCES dbo.CasosGanadores(CasoId),
        CONSTRAINT FK_EntregasGanadores_Usuarios FOREIGN KEY (PersonaQueEntrega) REFERENCES dbo.Usuarios(UsuarioId)
    );
    PRINT 'Tabla EntregasGanadores creada.';
END
GO

/* 8.3. EvidenciasGanador */
IF OBJECT_ID(N'dbo.EvidenciasGanador', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EvidenciasGanador (
        EvidenciaId     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        EntregaId       UNIQUEIDENTIFIER NOT NULL,
        TipoEvidencia   NVARCHAR(30)     NOT NULL, -- TicketConQR, GanadorConTicket, CedulaIdentidad
        RutaImagen      NVARCHAR(500)    NOT NULL,
        FechaCaptura    DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_EvidenciasGanador PRIMARY KEY (EvidenciaId),
        CONSTRAINT FK_EvidenciasGanador_Entregas FOREIGN KEY (EntregaId) REFERENCES dbo.EntregasGanadores(EntregaId),
        CONSTRAINT CK_EvidenciasGanador_Tipo CHECK (TipoEvidencia IN ('TicketConQR', 'GanadorConTicket', 'CedulaIdentidad'))
    );
    CREATE INDEX IX_EvidenciasGanador_EntregaId ON dbo.EvidenciasGanador(EntregaId);
    PRINT 'Tabla EvidenciasGanador creada.';
END
GO

/* Vinculo Boletos.CasoGanadorId con CasosGanadores */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Boletos_CasosGanadores')
BEGIN
    ALTER TABLE dbo.Boletos
        ADD CONSTRAINT FK_Boletos_CasosGanadores
        FOREIGN KEY (CasoGanadorId) REFERENCES dbo.CasosGanadores(CasoId);
    PRINT 'FK_Boletos_CasosGanadores creada.';
END
GO

IF COL_LENGTH(N'dbo.Boletos', N'QrCifrado') IS NULL
BEGIN
    ALTER TABLE dbo.Boletos
        ADD QrCifrado NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Boletos_QrCifrado DEFAULT ('');
    PRINT 'Columna Boletos.QrCifrado creada.';
END
GO

/* ============================================================
   9. PROCEDIMIENTOS ALMACENADOS
   ============================================================ */

/* 9.1. sp_ConfirmarVenta - Transaccion atomica de venta */
IF OBJECT_ID(N'dbo.sp_ConfirmarVenta', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_ConfirmarVenta;
GO

CREATE PROCEDURE dbo.sp_ConfirmarVenta
    @UsuarioId             UNIQUEIDENTIFIER,
    @DispositivoId         UNIQUEIDENTIFIER,
    @TipoApuesta           NVARCHAR(20),
    @Total                 DECIMAL(18,2),
    @IdempotencyKey        NVARCHAR(100),
    @Numero                CHAR(4),
    @Valor                 DECIMAL(18,2),
    @Loterias              NVARCHAR(MAX),  -- Lista separada por coma de LoteriaId
    @FechaVenta            DATETIME2,
    @VentaId               UNIQUEIDENTIFIER OUTPUT,
    @BoletoId              UNIQUEIDENTIFIER OUTPUT,
    @CodigoPublico         CHAR(7) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Control de duplicidad (RS-084): si ya existe venta con esta IdempotencyKey, devolver la existente
        IF EXISTS (SELECT 1 FROM dbo.Ventas WHERE IdempotencyKey = @IdempotencyKey)
        BEGIN
            SELECT TOP 1 @VentaId = V.VentaId, @BoletoId = B.BoletoId, @CodigoPublico = B.CodigoPublico
            FROM dbo.Ventas V
            INNER JOIN dbo.Boletos B ON B.VentaId = V.VentaId
            WHERE V.IdempotencyKey = @IdempotencyKey;

            COMMIT TRANSACTION;
            RETURN 0; -- Respuesta idempotente
        END

        -- Generar codigo publico unico de 7 digitos con reintento ante colision
        DECLARE @NuevoCodigo CHAR(7);
        DECLARE @Intentos INT = 0;
        DECLARE @CodigoGenerado BIT = 0;

        WHILE @CodigoGenerado = 0 AND @Intentos < 10
        BEGIN
            SET @NuevoCodigo = RIGHT('0000000' + CAST(CAST(RAND() * 10000000 AS INT) AS VARCHAR(7)), 7);

            IF NOT EXISTS (SELECT 1 FROM dbo.Boletos WHERE CodigoPublico = @NuevoCodigo)
            BEGIN
                SET @CodigoGenerado = 1;
            END
            SET @Intentos = @Intentos + 1;
        END

        IF @CodigoGenerado = 0
        BEGIN
            RAISERROR(N'No se pudo generar un codigo publico unico despues de 10 intentos.', 16, 1);
        END

        -- Insertar venta
        SET @VentaId = NEWID();
        INSERT INTO dbo.Ventas (VentaId, UsuarioId, DispositivoId, FechaVenta, Total, TipoApuesta, EstadoSincronizacion, IdempotencyKey)
        VALUES (@VentaId, @UsuarioId, @DispositivoId, @FechaVenta, @Total, @TipoApuesta, 'Online', @IdempotencyKey);

        -- Insertar boleto
        SET @BoletoId = NEWID();
        INSERT INTO dbo.Boletos (BoletoId, VentaId, CodigoPublico, ClaveValidacionHash, EstadoBoleto, VigenciaDias)
        VALUES (@BoletoId, @VentaId, @NuevoCodigo, '', 'Jugado', 30);

        -- Insertar juego
        DECLARE @JuegoId UNIQUEIDENTIFIER = NEWID();
        INSERT INTO dbo.Juegos (JuegoId, BoletoId, Numero, Valor, TipoJuego)
        VALUES (@JuegoId, @BoletoId, @Numero, @Valor, CASE WHEN @TipoApuesta = 'COMBINADO' THEN 'COMBINADA' ELSE 'INDIVIDUAL' END);

        -- Insertar relaciones juego-loteria
        INSERT INTO dbo.JuegoLoteria (JuegoId, LoteriaId)
        SELECT @JuegoId, CAST(value AS UNIQUEIDENTIFIER)
        FROM STRING_SPLIT(@Loterias, ',');

        SET @CodigoPublico = @NuevoCodigo;

        COMMIT TRANSACTION;
        PRINT 'Venta confirmada correctamente.';
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
PRINT 'Procedimiento sp_ConfirmarVenta creado.';

/* 9.2. sp_RegistrarResultado */
IF OBJECT_ID(N'dbo.sp_RegistrarResultado', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_RegistrarResultado;
GO

CREATE PROCEDURE dbo.sp_RegistrarResultado
    @LoteriaId     UNIQUEIDENTIFIER,
    @FechaJuego    DATE,
    @Numero        CHAR(4),
    @NumeroGanadorId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        -- Verificar duplicado (UNIQUE FechaJuego + LoteriaId)
        IF EXISTS (SELECT 1 FROM dbo.NumerosGanadores WHERE FechaJuego = @FechaJuego AND LoteriaId = @LoteriaId)
        BEGIN
            RAISERROR(N'Ya existe un numero ganador para esta fecha y loteria.', 16, 1);
            RETURN 1;
        END

        SET @NumeroGanadorId = NEWID();
        INSERT INTO dbo.NumerosGanadores (NumeroGanadorId, LoteriaId, FechaJuego, Numero)
        VALUES (@NumeroGanadorId, @LoteriaId, @FechaJuego, @Numero);

        PRINT 'Resultado registrado correctamente.';
        RETURN 0;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO
PRINT 'Procedimiento sp_RegistrarResultado creado.';

/* 9.3. sp_ValidarBoletoQR */
IF OBJECT_ID(N'dbo.sp_ValidarBoletoQR', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_ValidarBoletoQR;
GO

CREATE PROCEDURE dbo.sp_ValidarBoletoQR
    @CodigoPublico CHAR(7),
    @ClaveHash     NVARCHAR(200),
    @Valido        BIT OUTPUT,
    @Estado        NVARCHAR(30) OUTPUT,
    @Mensaje       NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @BoletoId UNIQUEIDENTIFIER;
    DECLARE @HashAlmacenado NVARCHAR(200);

    SELECT @BoletoId = B.BoletoId, @HashAlmacenado = B.ClaveValidacionHash
    FROM dbo.Boletos B
    WHERE B.CodigoPublico = @CodigoPublico;

    IF @BoletoId IS NULL
    BEGIN
        SET @Valido = 0;
        SET @Estado = 'NO EXISTE';
        SET @Mensaje = N'NO SE ENCONTRÓ INFORMACIÓN';
        RETURN;
    END

    -- Verificar hash de clave de validacion
    IF @HashAlmacenado <> @ClaveHash
    BEGIN
        SET @Valido = 0;
        SET @Estado = 'CLAVE INVALIDA';
        SET @Mensaje = N'NO SE ENCONTRÓ INFORMACIÓN';
        RETURN;
    END

    SELECT @Estado = B.EstadoBoleto
    FROM dbo.Boletos B
    WHERE B.BoletoId = @BoletoId;

    SET @Valido = 1;
    SET @Mensaje = N'Boleto validado: ' + @Estado;
END
GO
PRINT 'Procedimiento sp_ValidarBoletoQR creado.';

/* 9.4. sp_GenerarCodigosOffline */
IF OBJECT_ID(N'dbo.sp_GenerarCodigosOffline', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GenerarCodigosOffline;
GO

CREATE PROCEDURE dbo.sp_GenerarCodigosOffline
    @UsuarioId      UNIQUEIDENTIFIER,
    @DispositivoId  UNIQUEIDENTIFIER,
    @Cantidad       INT,
    @AdminId        UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF @Cantidad <= 0 OR @Cantidad > 5000
        BEGIN
            RAISERROR(N'Cantidad invalida. Debe ser entre 1 y 5000.', 16, 1);
            RETURN 1;
        END

        BEGIN TRANSACTION;

        DECLARE @Iteracion INT = 0;
        DECLARE @UltimoConsecutivo INT;
        DECLARE @NuevoConsecutivo NVARCHAR(30);

        -- Obtener el ultimo consecutivo para generar el siguiente
        SELECT @UltimoConsecutivo = ISNULL(MAX(CAST(SUBSTRING(ConsecutivoUnico, 4, LEN(ConsecutivoUnico)) AS INT)), 0)
        FROM dbo.CodigosPreventaOffline;

        WHILE @Iteracion < @Cantidad
        BEGIN
            SET @Iteracion = @Iteracion + 1;
            SET @UltimoConsecutivo = @UltimoConsecutivo + 1;
            SET @NuevoConsecutivo = 'NR-' + RIGHT('000000' + CAST(@UltimoConsecutivo AS VARCHAR(6)), 6);

            INSERT INTO dbo.CodigosPreventaOffline (ConsecutivoUnico, UsuarioId, DispositivoId, PayloadCifrado, EstadoDelCodigo)
            VALUES (@NuevoConsecutivo, @UsuarioId, @DispositivoId, 0x00, 'Generado');
        END

        COMMIT TRANSACTION;
        PRINT CAST(@Cantidad AS VARCHAR(10)) + N' codigos offline generados correctamente.';
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
PRINT 'Procedimiento sp_GenerarCodigosOffline creado.';

/* 9.5. sp_RegistrarCodigoOffline */
IF OBJECT_ID(N'dbo.sp_RegistrarCodigoOffline', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_RegistrarCodigoOffline;
GO

CREATE PROCEDURE dbo.sp_RegistrarCodigoOffline
    @ConsecutivoUnico NVARCHAR(30),
    @AdminQueRegistro UNIQUEIDENTIFIER,
    @FechaVentaOffline DATETIME2,
    @Total             DECIMAL(18,2),
    @TipoApuesta       NVARCHAR(20),
    @Numero            CHAR(4),
    @Valor             DECIMAL(18,2),
    @Loterias          NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CodigoId       UNIQUEIDENTIFIER;
    DECLARE @UsuarioId      UNIQUEIDENTIFIER;
    DECLARE @DispositivoId  UNIQUEIDENTIFIER;
    DECLARE @EstadoActual   NVARCHAR(20);
    DECLARE @VentaId        UNIQUEIDENTIFIER;
    DECLARE @BoletoId       UNIQUEIDENTIFIER;
    DECLARE @CodigoPublico  CHAR(7);

    BEGIN TRY
        -- Validar si el codigo existe y su estado
        SELECT @CodigoId = CodigoId, @UsuarioId = UsuarioId, @DispositivoId = DispositivoId, @EstadoActual = EstadoDelCodigo
        FROM dbo.CodigosPreventaOffline
        WHERE ConsecutivoUnico = @ConsecutivoUnico;

        IF @CodigoId IS NULL
        BEGIN
            RAISERROR(N'El codigo offline no existe.', 16, 1);
            RETURN 1;
        END

        -- Prevencion de duplicidad (RS-096): si ya esta Registrado, rechazar
        IF @EstadoActual = 'Registrado'
        BEGIN
            RAISERROR(N'QR ya registrado. No se permite registrar el mismo codigo nuevamente.', 16, 1);
            RETURN 2;
        END

        BEGIN TRANSACTION;

        -- Generar codigo publico de 7 digitos
        DECLARE @Intentos INT = 0;
        DECLARE @CodigoGenerado BIT = 0;

        WHILE @CodigoGenerado = 0 AND @Intentos < 10
        BEGIN
            SET @CodigoPublico = RIGHT('0000000' + CAST(CAST(RAND() * 10000000 AS INT) AS VARCHAR(7)), 7);
            IF NOT EXISTS (SELECT 1 FROM dbo.Boletos WHERE CodigoPublico = @CodigoPublico)
                SET @CodigoGenerado = 1;
            SET @Intentos = @Intentos + 1;
        END

        -- Crear venta oficial
        SET @VentaId = NEWID();
        INSERT INTO dbo.Ventas (VentaId, UsuarioId, DispositivoId, FechaVenta, Total, TipoApuesta, EstadoSincronizacion)
        VALUES (@VentaId, @UsuarioId, @DispositivoId, @FechaVentaOffline, @Total, @TipoApuesta, 'OfflineRegistrado');

        -- Crear boleto oficial
        SET @BoletoId = NEWID();
        INSERT INTO dbo.Boletos (BoletoId, VentaId, CodigoPublico, ClaveValidacionHash, EstadoBoleto, VigenciaDias)
        VALUES (@BoletoId, @VentaId, @CodigoPublico, '', 'Jugado', 30);

        -- Crear juego
        DECLARE @JuegoId UNIQUEIDENTIFIER = NEWID();
        INSERT INTO dbo.Juegos (JuegoId, BoletoId, Numero, Valor, TipoJuego)
        VALUES (@JuegoId, @BoletoId, @Numero, @Valor, CASE WHEN @TipoApuesta = 'COMBINADO' THEN 'COMBINADA' ELSE 'INDIVIDUAL' END);

        -- Relaciones juego-loteria
        INSERT INTO dbo.JuegoLoteria (JuegoId, LoteriaId)
        SELECT @JuegoId, CAST(value AS UNIQUEIDENTIFIER)
        FROM STRING_SPLIT(@Loterias, ',');

        -- Actualizar codigo a Registrado (RS-095)
        UPDATE dbo.CodigosPreventaOffline
        SET EstadoDelCodigo = 'Registrado',
            FechaRegistro = SYSUTCDATETIME(),
            AdminQueRegistro = @AdminQueRegistro,
            VentaId = @VentaId
        WHERE CodigoId = @CodigoId;

        COMMIT TRANSACTION;
        PRINT 'Codigo offline registrado correctamente.';
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
PRINT 'Procedimiento sp_RegistrarCodigoOffline creado.';

/* 9.6. sp_DesbloquearUsuario */
IF OBJECT_ID(N'dbo.sp_DesbloquearUsuario', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DesbloquearUsuario;
GO

CREATE PROCEDURE dbo.sp_DesbloquearUsuario
    @UsuarioId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Usuarios
    SET EstadoBloqueado = 0,
        EstadoValidado = 1,          -- Exige cambio de contrasena
        IntentosFallidos = 0
    WHERE UsuarioId = @UsuarioId;

    -- Cerrar todas las sesiones abiertas del usuario
    UPDATE dbo.Sesiones
    SET Activa = 0
    WHERE UsuarioId = @UsuarioId AND Activa = 1;

    PRINT 'Usuario desbloqueado correctamente.';
END
GO
PRINT 'Procedimiento sp_DesbloquearUsuario creado.';

/* 9.7. sp_RestablecerContrasena */
IF OBJECT_ID(N'dbo.sp_RestablecerContrasena', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_RestablecerContrasena;
GO

CREATE PROCEDURE dbo.sp_RestablecerContrasena
    @UsuarioId     UNIQUEIDENTIFIER,
    @NuevoHash     NVARCHAR(200),
    @NuevoSalt     NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    -- Solo funciona si el usuario esta ACTIVO (requisito)
    IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE UsuarioId = @UsuarioId AND Estado = 'Activo')
    BEGIN
        RAISERROR(N'El usuario debe estar ACTIVO para restablecer la contrasena.', 16, 1);
        RETURN 1;
    END

    UPDATE dbo.Usuarios
    SET PasswordHash = @NuevoHash,
        PasswordSalt = @NuevoSalt,
        EstadoValidado = 1,           -- Exige cambio de contrasena
        EstadoBloqueado = 0,
        IntentosFallidos = 0
    WHERE UsuarioId = @UsuarioId;

    -- Cerrar todas las sesiones abiertas (requisito)
    UPDATE dbo.Sesiones
    SET Activa = 0
    WHERE UsuarioId = @UsuarioId AND Activa = 1;

    PRINT 'Contrasena restablecida correctamente.';
END
GO
PRINT 'Procedimiento sp_RestablecerContrasena creado.';

/* 9.8. sp_CambiarGrupoVendedor */
IF OBJECT_ID(N'dbo.sp_CambiarGrupoVendedor', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CambiarGrupoVendedor;
GO

CREATE PROCEDURE dbo.sp_CambiarGrupoVendedor
    @UsuarioId  UNIQUEIDENTIFIER,
    @NuevoGrupoId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Eliminar grupo anterior
        DELETE FROM dbo.UsuariosGrupos WHERE UsuarioId = @UsuarioId;

        -- Asignar nuevo grupo
        INSERT INTO dbo.UsuariosGrupos (UsuarioId, GrupoId)
        VALUES (@UsuarioId, @NuevoGrupoId);

        COMMIT TRANSACTION;
        PRINT 'Grupo del vendedor actualizado correctamente.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
PRINT 'Procedimiento sp_CambiarGrupoVendedor creado.';

/* 9.9. sp_EliminarUsuario - Eliminacion en cascada (RS-027) */
IF OBJECT_ID(N'dbo.sp_EliminarUsuario', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_EliminarUsuario;
GO

CREATE PROCEDURE dbo.sp_EliminarUsuario
    @UsuarioId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Eliminar en orden de dependencias
        -- 1. EvidenciasGanador (via EntregasGanadores -> CasosGanadores)
        DELETE EG FROM dbo.EvidenciasGanador EG
        INNER JOIN dbo.EntregasGanadores E ON E.EntregaId = EG.EntregaId
        INNER JOIN dbo.CasosGanadores CG ON CG.CasoId = E.CasoId
        WHERE CG.VendedorQueReporto = @UsuarioId
           OR CG.AdminQueValido = @UsuarioId
           OR CG.AdminQueAsigno = @UsuarioId
           OR CG.ObservadorAsignado = @UsuarioId;

        -- 2. EntregasGanadores
        DELETE E FROM dbo.EntregasGanadores E
        INNER JOIN dbo.CasosGanadores CG ON CG.CasoId = E.CasoId
        WHERE CG.VendedorQueReporto = @UsuarioId
           OR CG.AdminQueValido = @UsuarioId
           OR CG.AdminQueAsigno = @UsuarioId
           OR CG.ObservadorAsignado = @UsuarioId;

        -- 3. CasosGanadores (como admin/vendedor/observador)
        DELETE FROM dbo.CasosGanadores
        WHERE VendedorQueReporto = @UsuarioId
           OR AdminQueValido = @UsuarioId
           OR AdminQueAsigno = @UsuarioId
           OR ObservadorAsignado = @UsuarioId;

        -- 4. AdjuntosChat (via Mensajes -> Conversaciones)
        DELETE AC FROM dbo.AdjuntosChat AC
        INNER JOIN dbo.Mensajes M ON M.MensajeId = AC.MensajeId
        INNER JOIN dbo.Conversaciones C ON C.ConversacionId = M.ConversacionId
        WHERE C.UsuarioIniciadorId = @UsuarioId OR C.UsuarioDestinoId = @UsuarioId;

        -- 5. Mensajes (directos del usuario o en sus conversaciones)
        DELETE M FROM dbo.Mensajes M
        INNER JOIN dbo.Conversaciones C ON C.ConversacionId = M.ConversacionId
        WHERE C.UsuarioIniciadorId = @UsuarioId OR C.UsuarioDestinoId = @UsuarioId;

        -- 6. Conversaciones
        DELETE FROM dbo.Conversaciones
        WHERE UsuarioIniciadorId = @UsuarioId OR UsuarioDestinoId = @UsuarioId;

        -- 7. JuegoLoteria (via Juegos -> Boletos -> Ventas)
        DELETE JL FROM dbo.JuegoLoteria JL
        INNER JOIN dbo.Juegos J ON J.JuegoId = JL.JuegoId
        INNER JOIN dbo.Boletos B ON B.BoletoId = J.BoletoId
        INNER JOIN dbo.Ventas V ON V.VentaId = B.VentaId
        WHERE V.UsuarioId = @UsuarioId;

        -- 8. Juegos
        DELETE J FROM dbo.Juegos J
        INNER JOIN dbo.Boletos B ON B.BoletoId = J.BoletoId
        INNER JOIN dbo.Ventas V ON V.VentaId = B.VentaId
        WHERE V.UsuarioId = @UsuarioId;

        -- 9. ClavesValidacionBoleto
        DELETE CV FROM dbo.ClavesValidacionBoleto CV
        INNER JOIN dbo.Boletos B ON B.BoletoId = CV.BoletoId
        INNER JOIN dbo.Ventas V ON V.VentaId = B.VentaId
        WHERE V.UsuarioId = @UsuarioId;

        -- 10. Boletos
        DELETE B FROM dbo.Boletos B
        INNER JOIN dbo.Ventas V ON V.VentaId = B.VentaId
        WHERE V.UsuarioId = @UsuarioId;

        -- 11. CodigosPreventaOffline
        DELETE FROM dbo.CodigosPreventaOffline
        WHERE UsuarioId = @UsuarioId OR AdminQueRegistro = @UsuarioId;

        -- 12. Notificaciones
        DELETE FROM dbo.Notificaciones WHERE UsuarioId = @UsuarioId;

        -- 13. Ventas
        DELETE FROM dbo.Ventas WHERE UsuarioId = @UsuarioId;

        -- 14. UsuariosGrupos
        DELETE FROM dbo.UsuariosGrupos WHERE UsuarioId = @UsuarioId;

        -- 15. DispositivosUsuarios
        DELETE FROM dbo.DispositivosUsuarios WHERE UsuarioId = @UsuarioId;

        -- 16. Sesiones
        DELETE FROM dbo.Sesiones WHERE UsuarioId = @UsuarioId;

        -- 17. IntentosFallidos
        DELETE FROM dbo.IntentosFallidos WHERE UsuarioId = @UsuarioId;

        -- 18. UsuariosRoles
        DELETE FROM dbo.UsuariosRoles WHERE UsuarioId = @UsuarioId;

        -- 19. Usuario
        DELETE FROM dbo.Usuarios WHERE UsuarioId = @UsuarioId;

        COMMIT TRANSACTION;
        PRINT 'Usuario eliminado en cascada correctamente.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
PRINT 'Procedimiento sp_EliminarUsuario creado.';

/* 9.10. sp_RegistrarEntregaPremio */
IF OBJECT_ID(N'dbo.sp_RegistrarEntregaPremio', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_RegistrarEntregaPremio;
GO

CREATE PROCEDURE dbo.sp_RegistrarEntregaPremio
    @CasoId              UNIQUEIDENTIFIER,
    @NombreGanador       NVARCHAR(150),
    @ApellidoGanador     NVARCHAR(150),
    @NumeroContacto      NVARCHAR(30),
    @LugarGano           NVARCHAR(255),
    @ValorTotalGanado    DECIMAL(18,2),
    @PersonaQueEntrega   UNIQUEIDENTIFIER,
    @EntregaId           UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @BoletoId UNIQUEIDENTIFIER;
    DECLARE @EstadoCaso NVARCHAR(20);
    DECLARE @NombreVendedor NVARCHAR(200);

    BEGIN TRY
        -- Obtener datos del caso
        SELECT @BoletoId = CG.BoletoId,
               @EstadoCaso = CG.Estado,
               @NombreVendedor = U.NombreCompleto
        FROM dbo.CasosGanadores CG
        INNER JOIN dbo.Usuarios U ON U.UsuarioId = CG.VendedorQueReporto
        WHERE CG.CasoId = @CasoId;

        IF @BoletoId IS NULL
        BEGIN
            RAISERROR(N'El caso de ganador no existe.', 16, 1);
            RETURN 1;
        END

        -- Verificar que no esté en estado irreversible
        IF @EstadoCaso IN ('Registrado', 'Rechazado')
        BEGIN
            RAISERROR(N'El caso no puede ser registrado porque ya tiene un estado final.', 16, 1);
            RETURN 2;
        END

        BEGIN TRANSACTION;

        -- Crear entrega
        SET @EntregaId = NEWID();
        INSERT INTO dbo.EntregasGanadores (EntregaId, CasoId, NombreGanador, ApellidoGanador, NumeroContacto, LugarGano, NombreVendedor, ValorTotalGanado, PersonaQueEntrega, FechaEntrega)
        VALUES (@EntregaId, @CasoId, @NombreGanador, @ApellidoGanador, @NumeroContacto, @LugarGano, @NombreVendedor, @ValorTotalGanado, @PersonaQueEntrega, SYSUTCDATETIME());

        -- Actualizar caso a Registrado
        UPDATE dbo.CasosGanadores
        SET Estado = 'Registrado',
            FechaRegistro = SYSUTCDATETIME()
        WHERE CasoId = @CasoId;

        -- Marcar boleto como Premio entregado (RN-072, RN-073)
        UPDATE dbo.Boletos
        SET EstadoBoleto = 'Premio entregado',
            EstadoDelPremio = 'PremioEntregado',
            FechaEntregaPremio = SYSUTCDATETIME()
        WHERE BoletoId = @BoletoId;

        COMMIT TRANSACTION;
        PRINT 'Entrega de premio registrada correctamente.';
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
PRINT 'Procedimiento sp_RegistrarEntregaPremio creado.';

/* ============================================================
   10. DATOS MAESTROS (SEED)
   ============================================================ */

PRINT '==============================================';
PRINT 'INSERTANDO DATOS MAESTROS';
PRINT '==============================================';

/* 10.1. Roles */
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Administrador')
    INSERT INTO dbo.Roles (Nombre, Descripcion) VALUES ('Administrador', 'Administra integralmente la plataforma');
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Vendedor')
    INSERT INTO dbo.Roles (Nombre, Descripcion) VALUES ('Vendedor', 'Responsable de registrar y vender apuestas');
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Observador')
    INSERT INTO dbo.Roles (Nombre, Descripcion) VALUES ('Observador', 'Supervisa la operacion y valida boletos');
GO
PRINT 'Datos maestros de Roles insertados.';

/* 10.2. EstadosBoleto */
IF NOT EXISTS (SELECT 1 FROM dbo.EstadosBoleto WHERE Nombre = 'Por jugar')
    INSERT INTO dbo.EstadosBoleto (Nombre) VALUES ('Por jugar');
IF NOT EXISTS (SELECT 1 FROM dbo.EstadosBoleto WHERE Nombre = 'Jugado')
    INSERT INTO dbo.EstadosBoleto (Nombre) VALUES ('Jugado');
IF NOT EXISTS (SELECT 1 FROM dbo.EstadosBoleto WHERE Nombre = 'Ganador')
    INSERT INTO dbo.EstadosBoleto (Nombre) VALUES ('Ganador');
IF NOT EXISTS (SELECT 1 FROM dbo.EstadosBoleto WHERE Nombre = 'No ganador')
    INSERT INTO dbo.EstadosBoleto (Nombre) VALUES ('No ganador');
IF NOT EXISTS (SELECT 1 FROM dbo.EstadosBoleto WHERE Nombre = 'Vencido')
    INSERT INTO dbo.EstadosBoleto (Nombre) VALUES ('Vencido');
IF NOT EXISTS (SELECT 1 FROM dbo.EstadosBoleto WHERE Nombre = 'Pagado/cobrado')
    INSERT INTO dbo.EstadosBoleto (Nombre) VALUES ('Pagado/cobrado');
IF NOT EXISTS (SELECT 1 FROM dbo.EstadosBoleto WHERE Nombre = 'Premio entregado')
    INSERT INTO dbo.EstadosBoleto (Nombre) VALUES ('Premio entregado');
GO
PRINT 'Datos maestros de EstadosBoleto insertados.';

/* 10.3. Configuraciones */
IF NOT EXISTS (SELECT 1 FROM dbo.Configuraciones WHERE Clave = 'HoraCierre')
    INSERT INTO dbo.Configuraciones (Clave, Valor) VALUES ('HoraCierre', '20:00:00');
IF NOT EXISTS (SELECT 1 FROM dbo.Configuraciones WHERE Clave = 'VigenciaPremiosDias')
    INSERT INTO dbo.Configuraciones (Clave, Valor) VALUES ('VigenciaPremiosDias', '30');
IF NOT EXISTS (SELECT 1 FROM dbo.Configuraciones WHERE Clave = 'AlertaRepeticionNumero')
    INSERT INTO dbo.Configuraciones (Clave, Valor) VALUES ('AlertaRepeticionNumero', '10');
IF NOT EXISTS (SELECT 1 FROM dbo.Configuraciones WHERE Clave = 'AlertaValorMinimo')
    INSERT INTO dbo.Configuraciones (Clave, Valor) VALUES ('AlertaValorMinimo', '10000');
IF NOT EXISTS (SELECT 1 FROM dbo.Configuraciones WHERE Clave = 'CodigosOfflineCapacidad')
    INSERT INTO dbo.Configuraciones (Clave, Valor) VALUES ('CodigosOfflineCapacidad', '3000');
IF NOT EXISTS (SELECT 1 FROM dbo.Configuraciones WHERE Clave = 'SincronizacionModo')
    INSERT INTO dbo.Configuraciones (Clave, Valor) VALUES ('SincronizacionModo', 'Manual');
IF NOT EXISTS (SELECT 1 FROM dbo.Configuraciones WHERE Clave = 'LeyendaTirilla')
    INSERT INTO dbo.Configuraciones (Clave, Valor) VALUES (
        'LeyendaTirilla',
        N'CONSERVE SU TICKET EN PERFECTO ESTADO.' + CHAR(10)
        + N'Vigencia: {vigenciaDias} días calendario desde su emisión. Vencido este plazo, el premio caducará y no será pagado.' + CHAR(10)
        + N'La aprobación del premio se realizará después de transcurridas 24 horas desde el momento en que el cliente lo haya reportado como ganador.');
GO
PRINT 'Datos maestros de Configuraciones insertados.';

/* 10.4. ConfiguracionesTipoApuesta */
IF NOT EXISTS (SELECT 1 FROM dbo.ConfiguracionesTipoApuesta WHERE TipoApuesta = 'COMBINADO')
    INSERT INTO dbo.ConfiguracionesTipoApuesta (TipoApuesta, Maximo) VALUES ('COMBINADO', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.ConfiguracionesTipoApuesta WHERE TipoApuesta = 'INDIVIDUAL')
    INSERT INTO dbo.ConfiguracionesTipoApuesta (TipoApuesta, Maximo) VALUES ('INDIVIDUAL', 6);
GO
PRINT 'Datos maestros de ConfiguracionesTipoApuesta insertados.';

/* 10.5. Loterias */
IF NOT EXISTS (SELECT 1 FROM dbo.Loterias WHERE Nombre = N'Bogotá')
    INSERT INTO dbo.Loterias (Nombre, Estado) VALUES (N'Bogotá', 'Activo');
IF NOT EXISTS (SELECT 1 FROM dbo.Loterias WHERE Nombre = N'Medellín')
    INSERT INTO dbo.Loterias (Nombre, Estado) VALUES (N'Medellín', 'Activo');
IF NOT EXISTS (SELECT 1 FROM dbo.Loterias WHERE Nombre = N'Cali')
    INSERT INTO dbo.Loterias (Nombre, Estado) VALUES ('Cali', 'Activo');
IF NOT EXISTS (SELECT 1 FROM dbo.Loterias WHERE Nombre = N'Armenia')
    INSERT INTO dbo.Loterias (Nombre, Estado) VALUES (N'Armenia', 'Activo');
IF NOT EXISTS (SELECT 1 FROM dbo.Loterias WHERE Nombre = 'Pasto')
    INSERT INTO dbo.Loterias (Nombre, Estado) VALUES ('Pasto', 'Activo');
GO
PRINT 'Datos maestros de Loterias insertados.';

/* 10.6. Grupos */
IF NOT EXISTS (SELECT 1 FROM dbo.Grupos WHERE Nombre = N'Grupo Norte')
    INSERT INTO dbo.Grupos (Nombre, Descripcion) VALUES (N'Grupo Norte', N'Vendedores de la zona norte');
IF NOT EXISTS (SELECT 1 FROM dbo.Grupos WHERE Nombre = N'Grupo Sur')
    INSERT INTO dbo.Grupos (Nombre, Descripcion) VALUES (N'Grupo Sur', N'Vendedores de la zona sur');
IF NOT EXISTS (SELECT 1 FROM dbo.Grupos WHERE Nombre = N'Grupo Centro')
    INSERT INTO dbo.Grupos (Nombre, Descripcion) VALUES (N'Grupo Centro', N'Vendedores de la zona centro');
GO
PRINT 'Datos maestros de Grupos insertados.';

/* 10.7. Usuario Administrador Inicial */
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Usuario = 'admin')
BEGIN
    INSERT INTO dbo.Usuarios (NombreCompleto, Usuario, Alias, Email, PasswordHash, PasswordSalt, Rol, Estado, EstadoValidado, EstadoBloqueado, IntentosFallidos)
    VALUES (N'Administrador Principal', 'admin', 'Admin', 'admin@newrich.com',
            '367C4B2DB148D69B17F617E3CDE3194D4A7D383F03B5C30736CEE7C3E0F80556',  -- PasswordHash de "Admin123!" (PBKDF2 SHA256, 100k iteraciones)
            'D6B428E0B2EE341BE3405E98080A0E2D',  -- Salt
            'Administrador', 'Activo', 1, 0, 0);

    -- Asociar rol administrador
    DECLARE @AdminUserId UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE Usuario = 'admin');
    DECLARE @AdminRoleId INT = (SELECT RolId FROM dbo.Roles WHERE Nombre = 'Administrador');

    INSERT INTO dbo.UsuariosRoles (UsuarioId, RolId) VALUES (@AdminUserId, @AdminRoleId);

    PRINT 'Usuario administrador inicial creado. Usuario: admin / Password temporal: Admin123!';
END
GO

/* ============================================================
   RESUMEN DE ELEMENTOS CREADOS
   ============================================================ */
PRINT '';
PRINT '==============================================';
PRINT 'RESUMEN DE LA BASE DE DATOS NEW RICH';
PRINT '==============================================';
SELECT 'TABLAS' AS Tipo, COUNT(*) AS Cantidad FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo');
SELECT 'PROCEDIMIENTOS' AS Tipo, COUNT(*) AS Cantidad FROM sys.procedures WHERE schema_id = SCHEMA_ID('dbo');
PRINT '==============================================';
PRINT 'BASE DE DATOS NEW RICH CREADA CORRECTAMENTE';
PRINT '==============================================';
GO