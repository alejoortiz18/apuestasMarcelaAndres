# Actualización: limpieza de datos históricos (6 meses)

Registro del cambio de retención y pasos para aplicarlo en el otro equipo.

La limpieza no está en el celular. No hace falta un APK nuevo. Hay que actualizar la base de datos, la API y el administrador.

## Qué cambió

El administrador, en Configuración, muestra una tarjeta **Retención de históricos** al lado de **Códigos offline**.

- **Meses máximos de retención:** 6. Se ve y no se puede cambiar.
- **Meses a eliminar:** el administrador elige 1, 2 o 3. El valor inicial, si no existe, es 1.
- Se eliminan meses calendario completos, desde el más antiguo.
- Nunca se eliminan el mes en curso ni los dos meses anteriores. La hora de corte es la medianoche de Colombia.
- La API ejecuta la limpieza. Si sale bien, la siguiente no corre antes de 10 días. Si falla, no borra a medias y lo reintenta en la siguiente revisión.
- El borrado es físico (`DELETE`), sin copia de seguridad.

Datos que se borran cuando el mes ya cumplió la regla:

| Dato | Tabla | Fecha que se usa |
| --- | --- | --- |
| Ventas | `dbo.Ventas`, con sus boletos y claves de validación | `FechaVenta` |
| Premios | `dbo.CasosGanadores` | `FechaReporte` |
| Pagos | `dbo.EntregasGanadores`, con las evidencias de esa entrega | `FechaEntrega` |
| Juegos | `dbo.Juegos` y `dbo.JuegoLoteria` | La fecha de la venta |

Los códigos offline y las notificaciones no se borran. Solo se les quita el vínculo a la venta o al juego eliminado.

Cada ejecución queda en `dbo.AuditoriaRetencion`. Esa auditoría también se borra cuando cumple 6 meses.

## Antes de actualizar el otro equipo

Detén la API de ese equipo antes de publicar y antes de ejecutar el script. Si la API nueva arranca con la clave `MesesAEliminar` en 1 y ya hay datos con 6 meses o más, borra el mes más antiguo. No hay respaldo automático.

## 1. Actualizar la base de datos

En el SQL Server del otro equipo, sobre la base `NewRich`, ejecuta este script. Se puede volver a ejecutar: no crea la tabla ni las claves si ya existen.

El mismo script está en `Proyectos/BD/RetencionHistorica.sql`.

```sql
/*
  Retención de históricos: auditoría del proceso y parámetros.
  MesesMaximosRetencion queda en 6. MesesAEliminar admite solo 1, 2 o 3.
*/
USE [NewRich];
GO

IF OBJECT_ID(N'dbo.AuditoriaRetencion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditoriaRetencion (
        AuditoriaRetencionId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        FechaEjecucionUtc     DATETIME2        NOT NULL,
        MesesMaximos          INT              NOT NULL,
        MesesAEliminar        INT              NULL,
        PeriodosEvaluados     NVARCHAR(200)    NOT NULL,
        PeriodosEliminados    NVARCHAR(200)    NOT NULL,
        VentasEliminadas      INT              NOT NULL,
        PremiosEliminados     INT              NOT NULL,
        PagosEliminados       INT              NOT NULL,
        JuegosEliminados      INT              NOT NULL,
        Resultado             NVARCHAR(20)     NOT NULL,
        MensajeError          NVARCHAR(500)    NULL,
        CONSTRAINT PK_AuditoriaRetencion PRIMARY KEY (AuditoriaRetencionId)
    );
    CREATE INDEX IX_AuditoriaRetencion_FechaEjecucionUtc
        ON dbo.AuditoriaRetencion(FechaEjecucionUtc);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Configuraciones WHERE Clave = N'MesesMaximosRetencion')
    INSERT INTO dbo.Configuraciones (ConfiguracionId, Clave, Valor, FechaActualizacion)
    VALUES (NEWID(), N'MesesMaximosRetencion', N'6', SYSUTCDATETIME());
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Configuraciones WHERE Clave = N'MesesAEliminar')
    INSERT INTO dbo.Configuraciones (ConfiguracionId, Clave, Valor, FechaActualizacion)
    VALUES (NEWID(), N'MesesAEliminar', N'1', SYSUTCDATETIME());
GO
```

Si en ese equipo deben eliminarse 2 o 3 meses, y no 1, cámbialo antes de encender la API:

```sql
USE [NewRich];
GO

UPDATE dbo.Configuraciones
SET Valor = N'1',
    FechaActualizacion = SYSUTCDATETIME()
WHERE Clave = N'MesesAEliminar';
GO
```

Sustituye `N'1'` por `N'2'` o `N'3'`. No uses otro número.

Comprueba:

```sql
USE [NewRich];
GO

SELECT Clave, Valor
FROM dbo.Configuraciones
WHERE Clave IN (N'MesesMaximosRetencion', N'MesesAEliminar');

SELECT OBJECT_ID(N'dbo.AuditoriaRetencion', N'U') AS TablaAuditoria;
GO
```

El resultado esperado es `MesesMaximosRetencion = 6`, `MesesAEliminar` en 1, 2 o 3, y `TablaAuditoria` distinto de NULL.

## 2. Publicar la API

La limpieza vive en la API. Sin esta publicación, la tarjeta puede verse, pero el borrado automático no corre.

Desde la carpeta del repositorio en el otro equipo:

```powershell
cd D:\Desarrollo\chances\SWApuestas
dotnet publish "Proyectos\API\src\NewRich.Api\NewRich.Api.csproj" -c Release
```

Copia la salida de `publish` sobre la carpeta donde ese equipo ya ejecuta la API. No reemplaces el `appsettings.json` de producción: ahí está la cadena de conexión.

Reinicia el proceso de la API (el sitio de IIS de la API, o el servicio con el que esté publicada) para que cargue la versión nueva.

## 3. Publicar el administrador

El administrador se publica en `C:\inetpub\rich`.

Detén el sitio o el grupo de aplicaciones antes de copiar, para que IIS no bloquee los archivos.

```powershell
cd D:\Desarrollo\chances\SWApuestas
dotnet publish "Proyectos\Administrador\NewRich.Admin.csproj" -c Release -o "C:\inetpub\rich"
```

No dejes el `appsettings.json` del administrador apuntando a localhost. Debe seguir apuntando a la API de ese equipo.

Recicla el sitio:

```powershell
Restart-WebAppPool -Name "rich"
```

Si el grupo de aplicaciones tiene otro nombre, usa ese nombre. Si no recicla, reinicia IIS:

```powershell
Restart-Service W3SVC
```

## 4. Comprobar

1. Abre el administrador e inicia sesión.
2. Entra a Configuración.
3. A la derecha de **Códigos offline** debe estar **Retención de históricos**.
4. El máximo debe decir 6 y no dejarse editar.
5. El selector de meses a eliminar debe ofrecer solo 1, 2 y 3.
6. Pulsa **Guardar configuración**.
7. En la base, `MesesAEliminar` debe quedar con el valor guardado y `MesesMaximosRetencion` debe seguir en 6.

La API, al arrancar, crea `dbo.AuditoriaRetencion` si el script no se ejecutó. Aun así, ejecuta el script: es el que deja las dos claves en la base. Si las claves no existen, la API no borra nada y registra el motivo en la auditoría.
