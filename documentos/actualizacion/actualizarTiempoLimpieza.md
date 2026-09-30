# Actualización: limpieza de datos históricos (6 meses)

Registro del cambio de retención y pasos para aplicarlo en el otro equipo.

La limpieza no está en el celular. No hace falta un APK nuevo. Hay que actualizar la base de datos, la API y el administrador.

## Qué cambió

El administrador, en Configuración, muestra una tarjeta **Retención de históricos** al lado de **Códigos offline**.

- **Meses máximos de retención:** 6. Se ve y no se puede cambiar.
- **Meses a eliminar:** el administrador elige 1, 2 o 3. Si la clave no existe, el valor que se crea es 1.
- Se eliminan meses calendario completos, en bloque, desde el más antiguo que ya cumplió 6 meses. Cada acierto quita solo esa cantidad (1, 2 o 3) y después espera 10 días. Un mes vacío dentro del bloque cuenta igual.
- Nunca se eliminan el mes en curso ni los dos meses anteriores. La hora de corte es la medianoche de Colombia.
- Una venta antigua se conserva si su premio o su pago cae en el mes en curso o en los dos anteriores. Un premio se conserva si su venta o su pago está en esa ventana.
- La API revisa la limpieza unos 30 segundos después de arrancar y luego cada hora. Si el acierto no borra nada, también cuenta: la siguiente no corre antes de 10 días. Si falla, no borra a medias y lo reintenta en la siguiente revisión.
- El borrado es físico (`DELETE`), sin copia de seguridad.

Con la fecha 28 de septiembre de 2026, el mes que ya se puede borrar es abril de 2026. Mayo y junio siguen. Julio, agosto y septiembre no se tocan. Si `MesesAEliminar` es 1 y el dato más viejo es enero, en ese arranque solo sale enero.

Datos que se borran cuando el mes ya cumplió la regla:

| Dato | Tabla | Fecha que se usa |
| --- | --- | --- |
| Ventas | `dbo.Ventas`, con sus boletos y claves de validación | `FechaVenta` |
| Premios | `dbo.CasosGanadores` | `FechaReporte` |
| Pagos | `dbo.EntregasGanadores`, con las evidencias de esa entrega | `FechaEntrega` |
| Juegos | `dbo.Juegos` y `dbo.JuegoLoteria` | La fecha de la venta |

Los códigos offline y las notificaciones no se borran. Solo se les quita el vínculo a la venta o al juego eliminado.

`dbo.AuditoriaRetencion` guarda cada ejecución que de verdad corre: acierto, nada que borrar, configuración ausente o error. Las revisiones de cada hora que caen dentro de los 10 días no escriben fila. Esa auditoría también se borra cuando su mes cumple la misma regla de 6 meses.

## Antes de actualizar el otro equipo

Detén la API de ese equipo antes de publicar y antes de ejecutar el script. El script deja `MesesAEliminar` en 1. Unos 30 segundos después de encender la API nueva, si ya hay un mes con 6 meses o más, borra solo ese mes más antiguo. No hay respaldo automático.

Abrir Configuración también crea las dos claves, con `MesesAEliminar` en 1, si todavía no existen. Hasta ese momento la API no borra y deja el motivo en la auditoría. En cuanto la pantalla carga, la siguiente revisión ya puede borrar.

Si en ese equipo deben salir 2 o 3 meses, deja ese número en la base antes de encender la API y antes de abrir Configuración.

## 1. Actualizar la base de datos

En el SQL Server del otro equipo, sobre la base `NewRich`, ejecuta el script `Proyectos/BD/RetencionHistorica.sql`. Se puede volver a ejecutar: no crea la tabla ni las claves si ya existen, y no cambia un valor que ya esté guardado.

```sql
/*
  Retención de históricos: auditoría del proceso y parámetros fijos.
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
    CREATE INDEX IX_AuditoriaRetencion_FechaEjecucionUtc ON dbo.AuditoriaRetencion(FechaEjecucionUtc);
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

Ese `INSERT` ya deja 1 mes. No hace falta otro script si 1 es el valor deseado. Para dejar 2 o 3 meses, y solo en ese caso, ejecuta uno de estos antes de encender la API:

```sql
USE [NewRich];
GO

UPDATE dbo.Configuraciones
SET Valor = N'2',
    FechaActualizacion = SYSUTCDATETIME()
WHERE Clave = N'MesesAEliminar';
GO
```

```sql
USE [NewRich];
GO

UPDATE dbo.Configuraciones
SET Valor = N'3',
    FechaActualizacion = SYSUTCDATETIME()
WHERE Clave = N'MesesAEliminar';
GO
```

No uses otro número. Si la clave ya existía, el script de creación no la modifica: este `UPDATE` es el que la cambia.

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

Aparta el `appsettings.json` de producción antes de copiar. Ahí está la cadena de conexión. La publicación trae otro y no debe reemplazarlo.

Desde la carpeta del repositorio en el otro equipo:

```powershell
cd D:\Desarrollo\chances\SWApuestas
dotnet publish "Proyectos\API\src\NewRich.Api\NewRich.Api.csproj" -c Release
```

Copia la salida de `publish` sobre la carpeta donde ese equipo ya ejecuta la API y devuelve el `appsettings.json` de producción.

Reinicia el proceso de la API (el sitio de IIS de la API, o el servicio con el que esté publicada) para que cargue la versión nueva. A los 30 segundos corre la primera revisión.

## 3. Publicar el administrador

El administrador se publica en `C:\inetpub\rich`.

Aparta el `appsettings.json` del administrador antes de publicar. `dotnet publish` lo sustituye. Después debe seguir apuntando a la API de ese equipo, no a localhost.

Detén el sitio y el grupo de aplicaciones antes de copiar, para que IIS no bloquee los archivos.

```powershell
Stop-Website -Name "rich"
Stop-WebAppPool -Name "rich"
```

Si el sitio o el grupo tienen otro nombre, usa ese nombre.

```powershell
cd D:\Desarrollo\chances\SWApuestas
dotnet publish "Proyectos\Administrador\NewRich.Admin.csproj" -c Release -o "C:\inetpub\rich"
```

Devuelve el `appsettings.json` de producción y arranca el sitio:

```powershell
Start-WebAppPool -Name "rich"
Start-Website -Name "rich"
```

Si no arranca, reinicia IIS:

```powershell
Restart-Service W3SVC
```

## 4. Comprobar

1. Abre el administrador e inicia sesión.
2. Entra a Configuración. Al cargar, la API crea las claves si el script no se ejecutó, con `MesesAEliminar` en 1.
3. A la derecha de **Códigos offline** debe estar **Retención de históricos**.
4. El máximo debe decir 6 y no dejarse editar.
5. El selector de meses a eliminar debe ofrecer solo 1, 2 y 3.
6. **Guardar configuración** graba toda la pantalla, no solo la retención. Revisa que el resto de valores sea el de ese equipo antes de pulsar.
7. En la base, `MesesAEliminar` debe quedar con el valor guardado y `MesesMaximosRetencion` debe seguir en 6.

La API, al hacer la primera revisión, crea `dbo.AuditoriaRetencion` si el script no se ejecutó. No crea las claves. Esas las deja el script, o la pantalla de Configuración. Si las claves no existen, la API no borra nada y registra el motivo en la auditoría.
