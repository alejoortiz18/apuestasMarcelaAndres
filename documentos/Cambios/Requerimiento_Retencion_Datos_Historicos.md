# Requerimiento: Retención y eliminación automática de datos históricos

## 1. Objetivo

Implementar un mecanismo de retención de datos históricos que conserve
información durante un máximo de 6 meses y elimine, de forma automática,
meses calendario completos según la cantidad configurada por el
administrador.

El sistema nunca elimina el mes en curso ni los dos meses calendario
anteriores, aunque la configuración indique eliminar 3 meses.

La eliminación es física (`DELETE`), total, y sin copia de seguridad ni
archivo previo.

## 2. Alcance de los datos

La depuración aplica únicamente a estos datos:

- Ventas (`dbo.Ventas`), con sus boletos y claves de validación, porque el boleto no puede quedar sin la venta.
- Premios (`dbo.CasosGanadores`).
- Pagos (`dbo.EntregasGanadores`), con las evidencias de esa entrega.
- Juegos (`dbo.Juegos`), con el vínculo `dbo.JuegoLoteria`.

La fecha de la venta es `FechaVenta`. La del premio es `FechaReporte`. La del pago es `FechaEntrega`. El juego no tiene fecha propia: sale con la venta. El corte de cada mes es la medianoche de Colombia.

Si un premio o un pago cae en el mes en curso o en los dos anteriores, no se elimina y la venta asociada tampoco. Los códigos offline y las notificaciones no se borran: solo se les quita el vínculo a la venta o al juego eliminado.

También se depuran los registros de auditoría de este mismo proceso
cuando cumplen 6 meses, según la sección 8.

No se modifica ni se elimina ninguna otra tabla ni información fuera de
este alcance. El orden de `DELETE` respeta las relaciones entre ventas,
premios, pagos y juegos para no dejar registros huérfanos.

## 3. Parámetros de configuración

Los parámetros se almacenan en la base de datos y se administran desde
una tarjeta nueva del módulo de configuración del sitio.

| Parámetro | Descripción | Valor | Edición |
| --- | --- | --- | --- |
| Meses máximos de retención | Tiempo máximo de conservación | 6 meses | Visible y fijo. No se puede modificar. |
| Meses a eliminar | Cantidad de meses calendario completos que se eliminan cuando corresponde | 1, 2 o 3 | El administrador elige solo uno de esos tres valores. |

La tarjeta muestra meses a eliminar. No muestra ni permite configurar
días a eliminar. Los 10 días entre ejecuciones exitosas son una regla
interna del proceso, no un parámetro de la tarjeta.

### Relación visible en la tarjeta

El administrador debe poder ver, en la misma tarjeta, la relación entre
las reglas:

- La retención máxima es de 6 meses y no se edita.
- Cuando corresponde depurar, se eliminan 1, 2 o 3 meses calendario
  completos, empezando por el más antiguo.
- Nunca se eliminan el mes en curso ni los dos meses anteriores.

### Restricciones

- El período máximo de retención es 6 meses, fijo.
- El administrador solo puede seleccionar 1, 2 o 3 meses a eliminar.
- No se permiten valores inferiores a 1 ni superiores a 3.
- La validación se aplica en la interfaz y en el backend.
- Los parámetros se persisten en la base de datos.
- Si la configuración no existe o tiene valores inválidos, el proceso no
  elimina información y registra el motivo.
- Cada ejecución lee de nuevo los meses a eliminar. No reutiliza el
  valor de la ejecución anterior, porque el administrador pudo
  cambiarlo.

## 4. Fecha de referencia y meses calendario

- Zona horaria: hora de Colombia (`America/Bogota`).
- El corte del día y del mes es la medianoche (00:00) hora de Colombia.
- La antigüedad se calcula con la fecha del registro, no con la fecha en
  que arranca el proceso.
- La unidad de eliminación es el mes calendario completo. No se eliminan
  registros sueltos por día.

Definiciones:

- **Mes de referencia:** mes calendario de la fecha actual en hora de
  Colombia.
- **Meses protegidos:** el mes de referencia y los dos meses calendario
  anteriores. Ejemplo: si el mes de referencia es septiembre, están
  protegidos julio, agosto y septiembre.
- **Antigüedad de un mes:** cantidad de meses calendario desde ese mes
  hasta el mes de referencia, contando ambos extremos. Abril respecto de
  septiembre cuenta 6 (abril, mayo, junio, julio, agosto, septiembre).
- **Mes que alcanzó el límite de 6 meses:** mes cuya antigüedad es mayor
  o igual a 6 y que no pertenece a los meses protegidos.

## 5. Comportamiento de la eliminación automática

El proceso elimina solo cuando existe al menos un mes que alcanzó el
límite de 6 meses. Si todos los datos tienen menos de 6 meses de
antigüedad, no elimina nada por antigüedad.

Cuando esa condición se cumple, en esa ejecución elimina como máximo la
cantidad configurada (1, 2 o 3) de meses calendario consecutivos,
empezando por el mes más antiguo que ya alcanzó el límite. Un mes sin
registros cuenta dentro de esa cantidad: no se omite para tomar el
siguiente mes que sí tenga datos.

De cada mes seleccionado se eliminan todos los registros de ventas,
premios, pagos y juegos cuya fecha de negocio caiga en ese mes. Si el
mes no tiene registros, igual consume uno de los cupos configurados.

La regla de protección prevalece. Si al tomar esos meses consecutivos se
alcanzaría el mes en curso o alguno de los dos anteriores, ese mes
protegido no se elimina. Si no queda ningún mes seguro, no se elimina
nada. El resultado se registra en auditoría.

En una ejecución exitosa no se eliminan más meses de los configurados.
Si queda un rezago más antiguo, las ejecuciones siguientes lo siguen
depurando, cada una con el valor de meses a eliminar vigente en ese
momento.

### Ejemplo: septiembre como mes de referencia y 1 mes a eliminar

Datos de los últimos 6 meses:

| Período | Antigüedad inclusive | Tratamiento |
| --- | --- | --- |
| Abril | 6 | Más antiguo que ya alcanzó el límite. Se elimina el mes completo. |
| Mayo | 5 | Se conserva en esta ejecución. |
| Junio | 4 | Se conserva en esta ejecución. |
| Julio | 3 | Protegido. |
| Agosto | 2 | Protegido. |
| Septiembre | 1 | Protegido. Mes en curso. |

### Ejemplo: misma ventana y 3 meses a eliminar

Se eliminan los meses completos de abril, mayo y junio. Quedan julio,
agosto y septiembre. Mayo y junio se eliminan en esa ejecución porque
entran en los 3 meses consecutivos pedidos y están fuera de la
protección, aunque por sí solos todavía no cumplan 6 meses de
antigüedad.

### Ejemplo: un mes sin registros dentro del cupo

Mes de referencia septiembre, configuración de 2 meses. El dato más
antiguo que ya cumplió 6 meses está en enero. Febrero no tiene registros
y marzo sí. Se toman enero y febrero. Marzo no se elimina en esa
ejecución, porque el mes vacío ya consumió el segundo cupo.

### Ejemplo: rezago anterior al límite

Mes de referencia septiembre, configuración de 1 mes, y el dato más
antiguo es enero. La primera ejecución elimina solo enero. La siguiente
ejecución permitida vuelve a leer la configuración y evalúa el mes más
antiguo que siga fuera de la protección y haya alcanzado el límite.

## 6. Regla de protección de los últimos 3 meses

El sistema nunca elimina información del mes en curso ni de los dos
meses calendario anteriores.

Para garantizarlo, el proceso debe:

1. Tomar como fecha de referencia la fecha calendario en hora de
   Colombia, con corte a medianoche.
2. Fijar como protegidos el mes en curso y los dos anteriores.
3. Identificar meses candidatos fuera de ese período que ya alcanzaron
   el límite de 6 meses.
4. Aplicar la cantidad configurada sobre meses calendario consecutivos,
   contando también los meses sin registros.
5. Excluir cualquier mes protegido antes de ejecutar `DELETE`.
6. Aplicar esta validación en el backend, con la configuración leída en
   esa ejecución.

## 7. Escenarios esperados

| Escenario | Comportamiento esperado |
| --- | --- |
| Configuración de 1 mes | Eliminar hasta 1 mes calendario completo elegible, el más antiguo. Un mes vacío cuenta. |
| Configuración de 2 meses | Eliminar hasta 2 meses calendario consecutivos, desde el más antiguo. Un mes vacío cuenta. |
| Configuración de 3 meses | Eliminar hasta 3 meses calendario consecutivos, desde el más antiguo, sin tocar el mes en curso ni los dos anteriores. |
| Datos con menos de 6 meses de antigüedad | No eliminar por antigüedad. |
| Registros del mes en curso o de los dos meses anteriores | Nunca eliminar. |
| Configuración inválida o inexistente | No eliminar y registrar el motivo. |
| No hay suficientes meses seguros | Eliminar solo los meses seguros. Si no hay ninguno, no eliminar. |
| Mes sin registros dentro del cupo | Cuenta como mes eliminado y no se sustituye por un mes posterior con datos. |
| La configuración cambió desde la ejecución anterior | Usar el valor persistido al inicio de la ejecución actual. |
| Auditoría con 6 meses o más | Eliminar esos registros de auditoría en la misma ejecución de mantenimiento. |

## 8. Ejecución automática

Un `BackgroundService` de .NET ejecuta el mantenimiento. La depuración
no se repite todos los días.

- Si no hay una ejecución exitosa previa, puede ejecutarse una vez,
  siempre que se cumplan las reglas de configuración, antigüedad y
  protección.
- Si la ejecución termina con éxito, la siguiente eliminación no puede
  ocurrir antes de 10 días calendario. Si el éxito queda registrado el
  día D (hora de Colombia), la próxima ejecución permitida es a partir
  del día D+10 a las 00:00 hora de Colombia.
- El servicio puede despertar antes solo para comprobar si ese plazo ya
  se cumplió. No elimina de nuevo hasta cumplirlo.
- Al inicio de cada ejecución lee los parámetros vigentes. Los 6 meses
  máximos siguen fijos. Los meses a eliminar son los que estén
  guardados en ese momento.
- Verifica si hay meses que alcanzaron los 6 meses de antigüedad.
- Aplica la protección del mes en curso y los dos anteriores.
- Ejecuta los `DELETE` de ventas, premios, pagos y juegos dentro de una
  transacción, sin copia de seguridad.
- Registra la auditoría de esa ejecución.
- En la misma ejecución elimina los registros de auditoría de este
  proceso cuya fecha de ejecución haya alcanzado 6 meses de antigüedad.
  Esa depuración de auditoría no usa el parámetro de 1, 2 o 3 meses:
  salen los registros de auditoría que ya cumplieron 6 meses.
- Evita ejecuciones simultáneas.
- Si ocurre un error, no continúa con eliminaciones inseguras. Lo
  ocurrido dentro de la transacción se revierte.
- Un error no inicia el plazo de 10 días. El servicio vuelve a intentar
  en la siguiente comprobación, cada hora.
- Una ejecución exitosa, aunque no borre filas porque no hay antigüedad
  suficiente, sí inicia el plazo de 10 días.

## 9. Auditoría

Cada ejecución se registra en una tabla de auditoría con, al menos:

- Fecha y hora de ejecución (hora de Colombia).
- Configuración utilizada (6 meses máximos y meses a eliminar leídos en
  esa ejecución).
- Períodos evaluados.
- Períodos efectivamente eliminados, incluyendo meses vacíos que
  contaron dentro del cupo.
- Cantidad de registros eliminados por ventas, premios, pagos y juegos.
- Resultado de la operación.
- Mensaje de error, si aplica.

Esos registros de auditoría también se eliminan con `DELETE` cuando su
fecha de ejecución alcanza 6 meses de antigüedad, con el mismo criterio
de mes calendario y medianoche hora de Colombia. No se conserva copia.
El registro de la ejecución en curso no se elimina en esa misma
corrida, porque todavía no tiene 6 meses.

## 10. Criterios de aceptación

1. El administrador configura únicamente 1, 2 o 3 meses a eliminar.
2. La tarjeta muestra los 6 meses máximos como valor fijo, no editable,
   y la relación con los meses a eliminar y con los 3 meses protegidos.
3. La tarjeta no ofrece una opción de días a eliminar.
4. Los parámetros quedan almacenados en la base de datos.
5. Cada ejecución consulta la configuración vigente antes de eliminar.
6. La eliminación solo empieza si hay al menos un mes que alcanzó los 6
   meses de antigüedad.
7. Se eliminan meses calendario completos, de los datos de ventas,
   premios, pagos y juegos, empezando por el más antiguo.
8. Un mes sin registros cuenta dentro de la cantidad configurada.
9. Ningún registro del mes en curso ni de los dos meses anteriores se
   elimina.
10. Una configuración inválida o ausente nunca provoca una eliminación.
11. La eliminación es `DELETE` transaccional, sin copia de seguridad, y
    respeta las relaciones entre las tablas del alcance.
12. Cada ejecución queda registrada en auditoría.
13. La auditoría de este proceso se depura al cumplir 6 meses.
14. Tras una ejecución exitosa, no hay otra eliminación antes del día
    D+10 a las 00:00 hora de Colombia.
15. No se producen ejecuciones simultáneas del mantenimiento.
16. No se eliminan ni modifican datos fuera de ventas, premios, pagos,
    juegos y la auditoría de este proceso.

## 11. Instrucción para implementación

Implementar esta retención en la aplicación .NET conforme a este
documento, después de que el documento quede validado.

Antes de cambiar código:

1. Revisar la estructura actual del proyecto.
2. Identificar las tablas físicas de ventas, premios, pagos y juegos, y
   la columna de fecha de negocio de cada una.
3. Identificar el mecanismo actual de configuración del administrador
   para agregar la tarjeta nueva.
4. Revisar entidades, `DbContext`, servicios y procesos en segundo plano
   existentes.
5. Confirmar el orden de `DELETE` según las relaciones reales.

Durante la implementación:

- Persistir los parámetros. Los 6 meses máximos quedan fijos y visibles.
- Permitir elegir solo 1, 2 o 3 meses a eliminar.
- Mostrar en la tarjeta la relación descrita en la sección 3.
- Implementar el `BackgroundService` con el plazo de 10 días tras una
  ejecución exitosa.
- Aplicar en backend la antigüedad, los meses completos, el conteo de
  meses vacíos y la protección del mes en curso y los dos anteriores.
- Ejecutar `DELETE` transaccional, sin backup, solo sobre el alcance.
- Evitar ejecuciones concurrentes.
- Implementar la auditoría y su propia depuración a los 6 meses.
- Crear o actualizar pruebas de configuración, antigüedad, meses vacíos,
  protección, plazo de 10 días y eliminación.
- No eliminar ni modificar datos o tablas fuera del alcance.

**Regla innegociable:** nunca eliminar información del mes en curso ni
de los dos meses calendario anteriores, incluso cuando la configuración
indique eliminar 3 meses.
