# Requerimiento: Implementación de cierres operacionales y períodos de análisis contable del negocio

## 1. Objetivo del cambio

Actualmente, el sistema registra diferentes movimientos y operaciones del negocio, entre ellos:

- Ventas de loterías.
- Recaudos.
- Premios entregados.
- Gastos personales.
- Gastos por tipo.
- Números cubiertos.
- Salarios.
- Otros movimientos y operaciones que se encuentren registrados en el sistema.

La mejora consiste en implementar una funcionalidad de cierres operacionales del negocio, que permita agrupar todos los movimientos realizados durante un período determinado, tomando como referencia la fecha y hora exacta en que el administrador ejecuta cada cierre.

La finalidad es crear bloques de información independientes, similares a períodos contables, que permitan consultar, analizar y comparar el comportamiento financiero y operacional del negocio.

Cada cierre deberá representar un período comprendido entre el cierre anterior y el nuevo cierre ejecutado.

Es importante aclarar que la operación del negocio continuará funcionando normalmente. La ejecución de un cierre no deberá detener las ventas, los recaudos ni ninguna otra actividad del sistema.

## 2. Funcionamiento general de los cierres

El sistema deberá registrar una fecha y hora exacta cada vez que el administrador ejecute un cierre del negocio.

Todos los movimientos realizados entre dos cierres deberán quedar asociados al cierre correspondiente.

Los períodos se calcularán automáticamente utilizando la siguiente lógica:

- El primer cierre establece la fecha y hora inicial de referencia.
- El siguiente período comienza exactamente un segundo después de la fecha y hora del cierre anterior.
- El nuevo período termina en la fecha y hora en que el administrador ejecuta el siguiente cierre.
- Cada movimiento deberá quedar asociado al período que corresponda según su fecha y hora de registro.

No se deberá crear un registro de apertura. El sistema determinará automáticamente la fecha y hora de inicio de cada período consultando el último cierre realizado y sumándole un segundo.

De esta manera, se evita tener que ejecutar manualmente una apertura para comenzar un nuevo período.

## 3. Ejemplo práctico del funcionamiento

Supongamos que el administrador ejecuta el primer cierre el día 01 de octubre de 2026 a las 14:00:00.

En ese momento, todos los movimientos registrados hasta esa fecha y hora quedarán asociados a ese cierre.

Posteriormente, el negocio continuará operando con normalidad y todos los nuevos movimientos comenzarán a formar parte del siguiente período.

El día 06 de noviembre de 2026 a las 16:00:00, el administrador ejecuta un nuevo cierre.

Ejemplo de períodos generados

Cierre anterior

01/10/2026 - 14:00:00

Fecha y hora en que se ejecutó el primer cierre.

Comienza el siguiente período

Período operacional

Desde: 01/10/2026 - 14:00:01

Hasta: 06/11/2026 - 16:00:00

Todos los movimientos registrados durante este intervalo pertenecen a este bloque.

Se ejecuta un nuevo cierre

Nuevo período

Desde: 06/11/2026 - 16:00:01

El negocio continúa operando y comienza automáticamente el siguiente bloque.

Importante: El sistema deberá utilizar fecha y hora completas, incluyendo horas, minutos y segundos, para evitar que existan movimientos duplicados entre períodos o registros que queden por fuera de algún cierre.

## 4. Ejecución del cierre por parte del administrador

La funcionalidad de ejecutar cierres estará disponible exclusivamente para el usuario administrador.

Para ejecutar un nuevo cierre, el sistema deberá solicitar confirmación mediante contraseña.

Antes de confirmar, deberá mostrar un modal informativo que permita al administrador conocer exactamente qué período está a punto de cerrar.

El modal deberá mostrar:

- Fecha y hora del último cierre realizado.
- Fecha y hora actual del nuevo cierre.
- Fecha y hora de inicio del período que se va a cerrar.
- Fecha y hora de finalización del período.
- Cantidad total de días comprendidos en el período.
- Resumen general de los movimientos que se van a cerrar.
- Cantidad de registros por cada categoría.
- Valores acumulados por cada categoría, cuando corresponda.
- Campo para ingresar la contraseña del administrador.
- Botón para confirmar el cierre.
- Botón para cancelar la operación.

El resumen deberá incluir las siguientes categorías:

1. Ventas de loterías.
2. Recaudos.
3. Premios entregados.
4. Gastos personales.
5. Gastos por tipo.
6. Números cubiertos.
7. Salarios.
8. Otros movimientos.

Al confirmar el cierre:

- El sistema validará la contraseña del administrador.
- Registrará la fecha y hora exacta del cierre.
- Asociará todos los movimientos correspondientes al período con el identificador del cierre generado.
- Guardará los totales y cantidades correspondientes al cierre.
- Actualizará la referencia del último cierre realizado.
- El siguiente período comenzará automáticamente un segundo después.

La operación deberá ejecutarse de forma controlada para garantizar que ningún movimiento quede sin asociar o asignado a dos cierres diferentes.

## 5. Actualización de la página de informes

En la página de informes se deberá implementar una nueva pestaña llamada Cierres, conservando la funcionalidad actual de los informes.

La distribución será similar a la estructura de pestañas que actualmente existe en la página de ventas.

Se tendrán dos pestañas:

Informes

Tab 1

Cierres

Tab 2

Tab 1. Informes

Se conservará el funcionamiento actual de los informes, con sus filtros, indicadores y visualizaciones.

Tab 2. Cierres

Será la nueva sección para ejecutar cierres, consultar el historial y analizar los períodos operacionales.

## 6. Nueva pestaña: Cierres

Esta pestaña deberá permitir administrar y consultar todos los cierres realizados por el administrador.

Se dividirá en tres funcionalidades principales:

### 6.1. Generar nuevo cierre

En la parte superior deberá existir un botón llamado Nuevo cierre.

Al seleccionarlo, se abrirá el modal de confirmación descrito anteriormente, mostrando el último cierre realizado, la fecha y hora actual, el período completo que se va a cerrar y el resumen de los movimientos involucrados.

Una vez confirmado, se generará el nuevo bloque operacional y quedará disponible en el historial.

### 6.2. Historial de cierres

En esta misma pestaña se deberá mostrar un histórico de todos los cierres realizados.

La información deberá permitir identificar fácilmente cada bloque operacional.

Como mínimo, cada registro deberá mostrar:

- Identificador del cierre.
- Fecha y hora de inicio.
- Fecha y hora de finalización.
- Cantidad de días del período.
- Total de ventas.
- Total de recaudos.
- Total de premios entregados.
- Total de gastos.
- Total de salarios.
- Total de ganancias.
- Fecha y hora en que se ejecutó el cierre.
- Administrador que realizó el cierre.

Se deberá permitir seleccionar cualquier cierre histórico para consultar toda la información correspondiente a ese período.

### 6.3. Visualización de los bloques operacionales

La idea es que el administrador pueda consultar inicialmente los cierres de manera general y posteriormente expandir cada bloque para visualizar el detalle de sus movimientos.

El comportamiento será el siguiente:

Nivel 1: Vista general de cierres

Se mostrarán todos los bloques operacionales creados, permitiendo identificar sus fechas, duración, ventas, gastos, ganancias y demás indicadores principales.

Nivel 2: Detalle del cierre seleccionado

Al seleccionar un bloque, el sistema deberá expandir su información y mostrar los grupos de movimientos que pertenecen a ese cierre.

Los grupos serán:

1. Ventas de loterías.
2. Recaudos.
3. Premios entregados.
4. Gastos personales.
5. Gastos por tipo.
6. Números cubiertos.
7. Salarios.
8. Otros movimientos.

Cada grupo deberá permitir consultar sus respectivos registros, cantidades, valores acumulados y demás detalles disponibles en el sistema.

La información presentada deberá corresponder exclusivamente al período seleccionado.

### 6.4. Organización y personalización de los bloques

Cada bloque operacional de la página de Cierres deberá permitir modificar su posición dentro de la página, utilizando el mismo comportamiento que actualmente está implementado en la vista de Informes.

Es decir, el administrador deberá poder organizar los bloques visuales según su preferencia, permitiendo cambiar su orden y distribución dentro de la interfaz.

Reglas de funcionamiento:

- Cada bloque de cierre deberá poder moverse de posición.
- Se deberá conservar el mismo comportamiento de organización que actualmente tiene la vista de Informes.
- La organización deberá ser independiente de la información del cierre.
- Cambiar la posición de un bloque no deberá modificar sus datos, registros, cálculos ni relaciones.
- Se deberá conservar la organización establecida por el usuario, siguiendo el funcionamiento actual de la vista de Informes.

## 7. Actualización de la pestaña Informes: análisis comparativo de cierres

Además de conservar los informes actuales, se deberá incorporar información financiera basada en los bloques operacionales generados.

La finalidad es que el administrador pueda analizar el comportamiento del negocio a través del tiempo y comparar los resultados de cada período.

Se deberá incorporar una sección de indicadores generales que permita visualizar:

- Total de ganancias por período.
- Total de ventas por período.
- Total de recaudos.
- Total de premios entregados.
- Total de gastos.
- Total de salarios.
- Balance general del período.
- Comparación del comportamiento entre cierres.

### 7.1. Gráfico comparativo de ganancias

Se deberá implementar un gráfico temporal que permita visualizar las ganancias obtenidas en cada bloque operacional.

El gráfico deberá permitir identificar:

- Ganancia de cada cierre.
- Fecha de inicio y finalización del período.
- Comportamiento de las ganancias entre períodos.
- Incrementos o disminuciones respecto al cierre anterior.
- Comparación general de los resultados.

### 7.2. Gráfico comparativo de gastos

Se deberá implementar otro gráfico temporal para visualizar los gastos de cada bloque operacional.

Este gráfico deberá permitir:

- Identificar el total de gastos por cierre.
- Comparar los gastos entre diferentes períodos.
- Identificar incrementos y disminuciones.
- Analizar el comportamiento de los gastos a través del tiempo.

### 7.3. Análisis general del comportamiento del negocio

Los informes deberán permitir interpretar cómo se comportó el negocio en cada bloque operacional.

La intención es que el administrador pueda identificar fácilmente:

- Qué períodos generaron mayores ganancias.
- En qué períodos se presentaron mayores gastos.
- Cómo evolucionaron las ventas.
- Qué tanto representan los gastos frente a los ingresos.
- Qué períodos tuvieron mejores y peores resultados.
- Cómo ha evolucionado financieramente el negocio a través de los cierres.

## 8. Estructura de base de datos: nuevo esquema Cierre

Se deberá crear un nuevo esquema en la base de datos llamado `cierre`, destinado a centralizar la administración y el almacenamiento de los cierres operacionales del negocio.

La finalidad es evitar almacenar repetidamente la fecha y hora del cierre en cada una de las tablas que contienen los movimientos del sistema.

### 8.1. Funcionamiento y estructura

1. Crear el nuevo esquema `cierre` en la base de datos.
2. Crear dentro de este esquema la tabla principal encargada de almacenar los cierres operacionales.
3. Esta tabla deberá almacenar la información general de cada cierre, incluyendo:
   - Identificador único del cierre.
   - Fecha y hora de inicio del período.
   - Fecha y hora de finalización del período.
   - Cantidad de días del período.
   - Fecha y hora de ejecución del cierre.
   - Administrador que ejecutó el cierre.
   - Totales consolidados de los movimientos, cuando corresponda.
   - Demás información necesaria para identificar y consultar cada cierre.
4. Las demás tablas que almacenan movimientos del negocio deberán relacionarse con esta nueva estructura mediante el identificador único del cierre (`CierreId`).

### 8.2. Relaciones con las tablas existentes

La fecha y hora general del cierre deberá almacenarse únicamente en la tabla principal de cierres, evitando duplicar esta información en todas las tablas de movimientos.

Las tablas de ventas, recaudos, premios, gastos, números cubiertos, salarios y demás módulos deberán utilizar la relación mediante `CierreId` para identificar a qué período pertenece cada registro.

Se deberá conservar estrictamente la tercera forma normal (3FN) en la estructura de la base de datos, evitando redundancia de información, duplicidad de datos y dependencias innecesarias.

### 8.3. Reglas de integridad

La implementación deberá garantizar:

- Relaciones mediante claves primarias y foráneas.
- Integridad referencial entre los movimientos y los cierres.
- Un único cierre asociado a cada movimiento.
- Consultas eficientes para obtener los movimientos de un cierre específico.
- Conservación de la información histórica.
- Coherencia entre los totales consolidados y los movimientos asociados.

La estructura deberá diseñarse de manera escalable, permitiendo incorporar nuevos módulos o tipos de movimientos al sistema sin tener que duplicar la información general de los cierres.

## 9. Actualización del API: endpoints, controladores y servicios

Se deberá actualizar el API para soportar la nueva funcionalidad de cierres operacionales, respetando la arquitectura, convenciones de nomenclatura y organización que actualmente maneja el proyecto.

Se deberán crear los nuevos componentes necesarios para administrar los cierres.

### 9.1. Estructura requerida

- Crear los nuevos endpoints relacionados con cierres.
- Crear los controladores correspondientes utilizando el nombre `Cierre`.
- Crear los servicios y demás componentes necesarios para gestionar la lógica de negocio.
- Implementar las consultas necesarias para obtener los cierres y sus movimientos asociados.
- Implementar el proceso de creación y confirmación de nuevos cierres.
- Implementar las validaciones de contraseña y permisos del administrador.
- Implementar los procesos de asociación de movimientos mediante `CierreId`.
- Implementar los endpoints necesarios para consultar el historial y el detalle de cada cierre.
- Implementar los endpoints necesarios para obtener los indicadores y datos comparativos utilizados en los informes.

### 9.2. Convención de nomenclatura

Todos los endpoints, controladores, servicios y componentes relacionados deberán seguir la misma estructura de prenombres, nomenclatura y organización que actualmente utiliza el API.

Se deberá utilizar el nombre `Cierre` para identificar los nuevos controladores y componentes relacionados, respetando los patrones existentes en el proyecto.

No se deberá introducir una arquitectura diferente ni modificar innecesariamente la estructura actual del API.

Importante: Antes de desarrollar, se deberá revisar la organización actual del API para identificar cómo están estructurados los controladores, servicios, repositorios, DTO, modelos y endpoints, y replicar ese mismo patrón en la implementación del nuevo módulo.

## 10. Regla obligatoria: manejo de fecha y hora de Colombia

Se deberá establecer como regla obligatoria para todo el sistema que todas las fechas y horas utilizadas en los cierres operacionales y en los movimientos del negocio deberán trabajar directamente con la fecha y hora local de Colombia (UTC-5).

El sistema deberá utilizar la hora de Colombia como referencia única para registrar, consultar, calcular y comparar las fechas y horas de todos los movimientos.

### 10.1. Reglas de cumplimiento

1. Todas las fechas y horas deberán registrarse utilizando la hora local de Colombia.
2. No se deberán utilizar conversiones automáticas de zonas horarias entre UTC y Colombia, ni conversiones adicionales que puedan modificar la hora original del registro.
3. No se deberán presentar diferencias de horas entre el backend, frontend, base de datos, servicios o reportes.
4. La fecha y hora de los cierres deberán corresponder exactamente al momento en que el administrador ejecuta la operación, utilizando la hora de Colombia.
5. La asignación de movimientos a cada cierre deberá realizarse utilizando las fechas y horas locales de Colombia.
6. Los cálculos de inicio y finalización de los períodos deberán respetar la misma referencia horaria.
7. Los informes, históricos, gráficos y consultas deberán mostrar siempre la fecha y hora de Colombia, sin aplicar conversiones adicionales.
8. Se deberá garantizar que todos los servidores, servicios y componentes involucrados utilicen una configuración horaria coherente con Colombia.

### 10.2. Ejemplo obligatorio

Si el administrador ejecuta un cierre el 06/11/2026 a las 16:00:00, el sistema deberá almacenar y mostrar exactamente esa fecha y hora.

El siguiente período deberá comenzar el 06/11/2026 a las 16:00:01.

No se permitirá que, por conversiones de zona horaria, el sistema almacene, consulte o presente una hora diferente a la que realmente corresponde en Colombia.

Importante: Esta regla aplica a todo el sistema, no solamente al módulo de cierres. Ningún módulo deberá utilizar conversiones horarias que alteren la fecha o la hora local de Colombia.

## 11. Consideraciones técnicas y reglas de funcionamiento

1. Los cierres solamente podrán ser ejecutados por el administrador.
2. Cada cierre deberá requerir confirmación mediante contraseña.
3. Los cierres deberán registrar fecha y hora exacta, incluyendo segundos.
4. No se deberá crear una apertura manual ni un registro de apertura.
5. El inicio de cada nuevo período se calculará automáticamente sumando un segundo al último cierre.
6. Todos los movimientos del sistema deberán quedar asociados a un período operacional.
7. Ningún movimiento podrá pertenecer a dos cierres diferentes.
8. No se deberá detener la operación normal del negocio cuando se ejecute un cierre.
9. Los movimientos nuevos deberán comenzar a asociarse al siguiente período inmediatamente después del cierre.
10. El historial de cierres deberá conservarse para futuras consultas y análisis.
11. Los cierres históricos deberán permitir consultar la información detallada de todos sus movimientos.
12. Los informes actuales deberán continuar funcionando y se complementarán con los nuevos análisis comparativos.
13. Los valores consolidados de cada cierre deberán coincidir con el detalle de sus movimientos.
14. Se deberá garantizar la integridad de la información para evitar que los movimientos históricos sean modificados o trasladados accidentalmente a otros cierres.
15. El sistema deberá contemplar correctamente las operaciones que se estén registrando simultáneamente mientras el administrador ejecuta un cierre.

## 12. Consideraciones adicionales importantes para el desarrollo

Hay tres aspectos técnicos que se deben contemplar para evitar inconsistencias en la implementación:

### A. El cierre debe ser transaccional

Cuando el administrador confirme un cierre, el sistema debe garantizar que todos los movimientos queden correctamente asignados.

Si ocurre un error durante el proceso, no debe quedar un cierre creado parcialmente.

### B. Los cierres deben conservar una fotografía de los resultados

Además de asociar los movimientos, se recomienda almacenar los totales consolidados del cierre. Así se podrá consultar el resultado histórico sin depender exclusivamente de cálculos posteriores sobre datos que eventualmente puedan cambiar.

### C. Control de operaciones simultáneas

Se debe controlar qué sucede si un vendedor registra una venta, un usuario registra un gasto o un recaudador realiza un pago exactamente mientras se está ejecutando el cierre.

La asignación deberá realizarse con una regla de corte consistente, para garantizar que cada movimiento quede en el período correcto.

### D. Identificación única de cada cierre

Se deberá utilizar un identificador único de cierre (`CierreId`) relacionado con los movimientos de cada módulo.

Esto facilitará las consultas, los informes, la trazabilidad y la integridad de los bloques operacionales.

## 13. Resultado esperado

Implementar un sistema de cierres operacionales que permita dividir toda la actividad del negocio en bloques independientes, utilizando fechas y horas exactas como referencia.

De esta manera, el administrador podrá cerrar períodos cuando lo considere necesario, consultar el historial de cierres, revisar todos los movimientos asociados a cada período y analizar el comportamiento financiero del negocio.

Adicionalmente:

- La página de Cierres permitirá organizar visualmente sus bloques, de la misma manera que la vista de Informes.
- La base de datos contará con un esquema independiente llamado `cierre`, centralizando la información general de cada período y relacionando los movimientos mediante `CierreId`.
- Se conservará la tercera forma normal (3FN) en la estructura de la base de datos.
- El API tendrá toda la estructura necesaria para administrar esta funcionalidad, respetando las convenciones, nomenclatura y arquitectura que ya utiliza el proyecto.
- Los informes permitirán comparar las ganancias, los gastos y los resultados generales entre diferentes períodos, facilitando la toma de decisiones y el seguimiento de la evolución del negocio.

Regla fundamental de todo el desarrollo: el sistema deberá garantizar que cada movimiento pertenezca a un único período de cierre, que ningún movimiento se pierda y que todos los registros, cálculos e informes utilicen exclusivamente la fecha y hora local de Colombia, sin conversiones horarias que alteren los datos.