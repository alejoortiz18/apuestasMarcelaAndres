# Especificación funcional: Utilidades

**Estado:** Alcance acordado para esta entrega  
**Módulo:** Utilidades  
**Perfiles:** Administrador y Vendedor Web  
**Sitios:** Administrador y sitio del vendedor

El administrador elige un periodo y ve tres cifras del mismo rango: ingresos por ventas, gastos registrados y la resta. El vendedor web deja registrado un gasto a su nombre y consulta solo lo que él mismo guardó. La ganancia la ve únicamente el administrador.

Esta entrega no incluye gráficos, comparación con otro periodo, alertas, exportación, soportes adjuntos ni un flujo de aprobación.

------------------------------------------------------------------------

## 1. Resultado operativo

La ganancia neta operativa es la resta de los ingresos de ventas incluidos y de los gastos activos del periodo. No es el saldo de caja ni la utilidad contable. Devoluciones, premios y otros conceptos que no estén dentro de las tres fuentes de la sección 6 siguen fuera de esta resta.

Las tres cifras del administrador usan la misma regla:

- **Ingresos** = suma de las ventas incluidas de las tres fuentes del periodo, sin contar dos veces el mismo valor.
- **Gastos** = suma de los gastos no anulados del periodo.
- **Ganancia neta operativa** = ingresos − gastos.

Si todavía no se puede calcular el ingreso con las reglas de la sección 6, el resumen no muestra un cero como si el cierre fuera real. Muestra los gastos y deja los ingresos y la ganancia en blanco, con la leyenda de que falta cerrar la fuente.

Los importes se guardan como decimal de moneda, con la misma precisión que ya usan las ventas (`decimal(18,2)`). No se usa punto flotante binario. El periodo se corta con la misma zona horaria con la que la aplicación guarda `FechaVenta` y `FechaRegistro`.

Un gasto anulado se conserva, se distingue en el listado y no entra en Gastos ni en la resta.

------------------------------------------------------------------------

## 2. Navegación

### 2.1 Sitio del vendedor

El menú plano actual pasa a dos grupos. Las rutas, los permisos y el comportamiento de cada pantalla se conservan. No se duplican ni se eliminan opciones.

**Ventas**

- Inicio
- Registrar juego
- Histórico
- Alertas
- Validar ticket
- Resultados
- Atención al cliente
- Soporte técnico
- Entrega de premio

**Utilidades**

- **Indicadores:** informe de los gastos activos que registró el usuario en el periodo: total, cantidad, promedio, mayor gasto, participación por tipo y evolución por día (por mes si el periodo pasa de 31 días). Los anulados se nombran aparte y no entran en las cifras. No incluye ventas ni ganancia.
- **Mis gastos:** listado de los gastos que registró el usuario de la sesión. El botón Registrar gasto abre el formulario en un cuadro sobre esta misma página.

El vendedor no tiene Resumen de ganancias ni Configuración de gastos.

### 2.2 Administrador

Se agrega el grupo **Utilidades** debajo de **Revisada manual**. El resto del menú del administrador, incluido el grupo Ventas que ya existe, permanece donde está.

- **Indicadores:** el mismo informe del vendedor, con los gastos de todos los usuarios: total, cantidad, promedio, mayor gasto, participación por tipo y evolución por día. Los anulados se nombran aparte. No incluye ventas ni ganancia.
- **Resumen:** ingresos, gastos y ganancia neta operativa del periodo. El botón **Registrar gasto** abre el formulario en un cuadro sobre esta misma página.
- **Movimientos:** gastos de todos los usuarios.
- **Configuración de gastos**

------------------------------------------------------------------------

## 3. Permisos

El servidor rechaza lo que el rol no puede hacer. Ocultar el menú no basta. El usuario del gasto sale de la sesión autenticada. El cliente no puede enviar otro usuario.

| Acción | Vendedor Web | Administrador |
| --- | --- | --- |
| Registrar un gasto a su nombre | Sí | Sí |
| Ver el total y el listado de sus propios gastos | Sí | Sí, dentro del listado general |
| Ver gastos de otros usuarios | No | Sí |
| Ver ingresos y ganancia neta operativa | No | Sí |
| Editar valor, fecha o tipo de un gasto | No | Sí, con motivo y rastro |
| Anular un gasto | No | Sí, con motivo y rastro |
| Crear, editar, activar o desactivar tipos de gasto | No | Sí |
| Borrar un tipo que ya tiene gastos | No | No |

------------------------------------------------------------------------

## 4. Mis gastos (vendedor web)

El vendedor consulta únicamente los gastos que él registró. No ve ventas, no ve la ganancia y no ve gastos de otras personas. El informe está en **Indicadores**. El listado está en **Mis gastos**. Cada página tiene su propio periodo.

### 4.1 Periodo

El periodo de cada página es independiente:

- Hoy, ayer, últimos 7 días, últimos 30 días, mes actual, mes anterior y rango desde/hasta.

Se muestra el periodo aplicado. La acción es consultar ese rango. Limpiar vuelve al mes actual.

### 4.2 Indicadores

Informe de sus propios gastos activos del periodo:

- **Total de mis gastos:** suma.
- **Cantidad:** número de gastos.
- **Gasto promedio:** total dividido entre la cantidad.
- **Mayor gasto:** el valor más alto, con su tipo y su fecha.
- **Gastos por tipo:** cada tipo con su valor, su participación sobre el total y su cantidad, en barras.
- **Gastos por día:** una barra por día con gasto. Si el periodo supera 31 días, las barras se agrupan por mes.

Los anulados del periodo se mencionan aparte (cantidad y valor) y no entran en ninguna de esas cifras. Si no hay gastos activos, las cifras quedan en cero y las gráficas muestran el estado vacío.

### 4.3 Listado

Columnas: fecha del gasto, tipo, observación, valor y estado. El total corresponde al conjunto filtrado, no solo a la página visible. Los anulados se ven marcados y no entran en el total. El vendedor no tiene acciones de editar ni anular.

------------------------------------------------------------------------

## 5. Resumen (administrador)

El resumen muestra las tres cifras del sistema en el periodo elegido. No se parten por vendedor: el ingreso de esta entrega es un total, no una venta atribuida a cada persona.

### 5.1 Periodo

Las tres cifras usan el mismo periodo:

- Hoy, ayer, últimos 7 días, últimos 30 días, mes actual, mes anterior y rango desde/hasta.

Encima de las cifras se ve el periodo aplicado y el momento de la última lectura. Limpiar vuelve al mes actual.

El filtro de usuario, de tipo o de estado no vive en estas tres cifras. Vive en Movimientos. Así un recorte del listado no deja la ganancia como si el ingreso también se hubiera partido.

### 5.2 Cifras

1. **Ingresos**, con la regla de la sección 6.
2. **Gastos**, suma de los gastos activos de todos los usuarios en el periodo.
3. **Ganancia neta operativa**, ingresos menos gastos.

Junto a la ganancia hay una ayuda visible: es el resultado operativo del periodo. No es saldo de caja ni utilidad contable.

Desde el resumen se puede pasar a Movimientos, ya acotado al mismo periodo, y a Registrar gasto.

------------------------------------------------------------------------

## 6. Ingresos

Los ingresos del administrador suman tres fuentes que ya existen. Se leen. No se copian al esquema `utilidades` ni se modifican sus tablas.

| Fuente | Qué representa | Dónde está el importe |
| --- | --- | --- |
| Ventas del celular | Ventas del PDA | `dbo.Ventas.Total`, con fecha `FechaVenta` |
| Juego manual | Ventas confirmadas en el esquema manual | `manual.Ventas.Total`, con fecha `FechaVenta` |
| Sitio del vendedor | Jugadas de Registrar juego | Suma de `manual.RegistroJuegoFilas.Valor` por registro, con fecha `manual.RegistrosJuego.FechaRegistro` |

`manual.RegistrosJuego` no tiene columna de total. El importe de esa fuente es la suma de sus filas. En `dbo.Ventas` y en `manual.Ventas` se usa el total de la cabecera. No se suma además el detalle (`Boletos`, `Juegos` o filas) cuando ese detalle repite el mismo valor.

### 6.1 Lo que hay que cerrar antes de programar la cifra

Antes de mostrar Ingresos hay que dejar escrito, para cada fuente:

- Qué estados entran y cuáles quedan fuera (anulada, devuelta, pendiente de sincronizar u otro estado que ya exista).
- Qué fecha corta el periodo: `FechaVenta` en las dos primeras fuentes y `FechaRegistro` en la tercera, salvo que el negocio indique otra.
- Que el detalle no se esté sumando junto con la cabecera.
- Una comparación del total de un periodo de prueba contra el reporte que el negocio ya usa, y la diferencia explicada si la hay.

Hasta cerrar esos cuatro puntos, Ingresos y Ganancia permanecen en blanco. Los gastos sí se pueden registrar y consultar.

El ingreso no se muestra por usuario en esta entrega, aunque las tablas tengan `UsuarioId`.

------------------------------------------------------------------------

## 7. Registro de gastos

El formulario es el mismo para el administrador y para el vendedor web.

- **Tipo de gasto:** obligatorio, solo tipos activos.
- **Fecha del gasto:** obligatoria, por defecto hoy. Es un día, no una hora.
- **Valor:** obligatorio, mayor que cero, en pesos.
- **Observación:** obligatoria cuando el tipo lo exige. Si el tipo no la exige, puede ir vacía.
- **Guardar** y **Cancelar**.

El usuario que registra no aparece como campo editable. Al guardar, el gasto queda activo y entra en los totales de quien tenga permiso para verlos. No hay cola de aprobación.

Validar en cliente y en servidor. Rechazar cero, negativos y formatos inválidos. Un segundo clic no crea otro gasto. Si el guardado falla, los datos siguen en el formulario y el mensaje dice qué corregir. La confirmación muestra tipo, valor y fecha.

Cancelar vuelve a Mis gastos en el sitio del vendedor y al Resumen en el administrador.

------------------------------------------------------------------------

## 8. Edición y anulación

Solo el administrador corrige. El vendedor no edita ni anula, tampoco sus propios gastos.

Corregir un gasto activo permite cambiar valor, fecha y tipo. Cada cambio pide un motivo. El rastro guarda el valor anterior, el valor nuevo, el motivo, quién lo hizo y cuándo. La observación no se edita: si quedó mal, se anula el gasto y se registra otro.

Anular también pide motivo. El gasto sigue en el listado, marcado como anulado, y deja de sumar. No se borra la fila.

No hay borrado físico de gastos.

------------------------------------------------------------------------

## 9. Movimientos (administrador)

Listado de los gastos de todos los usuarios, paginado en el servidor cuando el volumen lo exija.

Filtros: el mismo periodo del resumen, usuario, tipo de gasto y estado (activo o anulado). Esos filtros recortan este listado y su total. No cambian las tres cifras del Resumen.

Columnas: fecha del gasto, tipo, observación, valor, usuario que registró, estado y, si hubo cambio, quién lo modificó por última vez.

El total es el de todo el conjunto filtrado, no el de la página visible. Los activos suman. Los anulados se muestran y no suman.

Acciones del administrador sobre un gasto activo: editar valor, fecha y tipo, o anular. Ambas piden motivo y quedan en el rastro.

------------------------------------------------------------------------

## 10. Configuración de gastos

Solo el administrador.

Campos:

- Nombre, obligatorio y único sin distinguir mayúsculas ni espacios sobrantes.
- Descripción opcional.
- Activo o inactivo.
- Si la observación es obligatoria al registrar.
- Fecha y usuario de creación y de la última actualización.

Operaciones: crear, consultar, editar nombre, descripción y la exigencia de observación, activar y desactivar. Cada tipo muestra cuántos gastos lo usan.

Un tipo con gastos no se elimina. Desactivarlo lo quita del formulario de registro nuevo. Los gastos ya guardados siguen mostrando ese nombre.

Semilla inicial, modificable después por el administrador: transporte, alimentación, entrega de premio, servicios, mensajería, materiales, mantenimiento, comisiones y gastos bancarios, y otros. Es un catálogo de gastos anotados a mano. No lee otros módulos.

------------------------------------------------------------------------

## 11. Datos

Las tablas nuevas viven en el esquema `utilidades`. Los nombres se adaptan a las convenciones del proyecto si hace falta, sin salir de ese esquema.

### 11.1 `utilidades.TiposGasto`

| Campo | Regla |
| --- | --- |
| `Id` | Identificador, clave primaria |
| `Nombre` | Obligatorio, único según la regla de la sección 10 |
| `Descripcion` | Opcional |
| `Activo` | Verdadero al crear |
| `RequiereObservacion` | Define si el formulario exige observación |
| `FechaCreacion`, `UsuarioCreacionId` | Obligatorios, desde la sesión |
| `FechaActualizacion`, `UsuarioActualizacionId` | Se llenan al modificar |

### 11.2 `utilidades.Gastos`

| Campo | Regla |
| --- | --- |
| `Id` | Identificador, clave primaria |
| `TipoGastoId` | Tipo existente. Puede quedar inactivo después |
| `FechaGasto` | Día obligatorio |
| `Valor` | `decimal(18,2)`, mayor que cero |
| `Observacion` | Según el tipo |
| `UsuarioRegistroId` | Usuario de la sesión. No se actualiza al editar |
| `Estado` | Activo o anulado |
| `ClaveIdempotencia` | Evita el doble envío del mismo guardado |
| `FechaCreacion` | Auditoría |
| `FechaActualizacion`, `UsuarioActualizacionId` | Se llenan al editar o anular |

### 11.3 `utilidades.AuditoriaCambios`

Un registro por edición o anulación:

- Gasto afectado.
- Acción: edición o anulación.
- Valor, fecha y tipo anteriores y nuevos. En la anulación, el estado pasa de activo a anulado.
- Motivo.
- Usuario y fecha de la acción.

Este rastro no lo modifica el vendedor ni el administrador desde la pantalla. Sirve para consulta de soporte, no como un módulo de auditoría general.

Índices de apoyo, a validar con el plan de ejecución: fecha del gasto, tipo, usuario y estado. Las migraciones crean el esquema `utilidades` y estas tablas, y no alteran las tablas de ventas.

------------------------------------------------------------------------

## 12. API

Rutas propias, sin cambiar las existentes. La autorización, los errores y el formato de respuesta se reutilizan.

- `GET /api/utilidades/resumen` — administrador. Ingresos, gastos y ganancia del periodo.
- `GET /api/utilidades/mis-gastos` — usuario de la sesión. Total, cantidad y listado propios.
- `GET /api/utilidades/gastos` — administrador. Listado de todos, con filtros y total del conjunto.
- `POST /api/utilidades/gastos` — administrador y vendedor web.
- `PUT /api/utilidades/gastos/{id}` — administrador. Edita valor, fecha y tipo, con motivo.
- `POST /api/utilidades/gastos/{id}/anular` — administrador, con motivo.
- `GET /api/utilidades/tipos-gasto` — tipos activos para el formulario. El administrador también consulta los inactivos en la configuración.
- `POST /api/utilidades/tipos-gasto` — administrador.
- `PUT /api/utilidades/tipos-gasto/{id}` — administrador.
- `PATCH /api/utilidades/tipos-gasto/{id}/estado` — administrador.

Controladores nuevos, con `Utilidades` en el nombre: `ResumenUtilidadesController`, `GastosUtilidadesController` y `TiposGastoUtilidadesController`, o el equivalente que ya use el API. La lógica queda en servicios y repositorios de este módulo. La lectura de ventas es una dependencia explícita de solo consulta. No se agrega esta lógica a los controladores de ventas.

Las tres cifras se calculan en un solo servicio. El resumen, el total de Mis gastos y el total de Movimientos no reimplementan la suma cada uno por su lado.

------------------------------------------------------------------------

## 13. Criterios de aceptación

1. El administrador ve el grupo Utilidades con Resumen, Registrar gasto, Movimientos y Configuración de gastos.
2. El sitio del vendedor agrupa el menú actual bajo Ventas, sin perder pantallas ni permisos, y agrega Utilidades con Mis gastos y Registrar gasto.
3. El vendedor registra un gasto y el sistema lo guarda a su nombre. No hay forma de elegir otro usuario.
4. El gasto guardado queda activo y aparece en Mis gastos de quien lo registró y en Movimientos del administrador.
5. El vendedor ve el total, la cantidad y el listado de sus gastos del periodo. No ve ingresos, ni ganancia, ni gastos de otros, y no puede editar ni anular.
6. El administrador ve, para el mismo periodo, ingresos, gastos de todos los usuarios y ganancia neta operativa.
7. Con un usuario, un tipo o un estado elegido en Movimientos, cambia el listado y su total. Las tres cifras del Resumen siguen siendo las del periodo completo.
8. La ganancia es ingresos menos gastos activos. Un anulado no entra en la resta y sigue visible.
9. Si la regla de ingresos de la sección 6.1 no está cerrada, ingresos y ganancia no se muestran como cero.
10. El administrador edita valor, fecha y tipo con un motivo, y el rastro conserva el anterior, el nuevo, quién y cuándo.
11. El administrador anula con un motivo. El vendedor recibe rechazo si llama a editar o anular en el API.
12. Un tipo usado se desactiva y deja de ofrecerse al registrar. El gasto viejo conserva el nombre. No se borra el tipo.
13. Los ingresos leen `dbo.Ventas`, `manual.Ventas` y las filas de `manual.RegistrosJuego`, sin duplicar cabecera y detalle y sin copiar esas ventas al esquema `utilidades`.
14. Las tablas nuevas están en el esquema `utilidades`. Las migraciones no cambian las tablas de ventas.
15. Los controladores nuevos llevan `Utilidades` en el nombre y sus rutas cuelgan de `/api/utilidades/`.

------------------------------------------------------------------------

## 14. Orden de construcción

1. Esquema `utilidades`, tipos de gasto, registro, Mis gastos y Movimientos, con permisos y anulación.
2. Edición con motivo y rastro.
3. Inventario de la sección 6.1 y, solo después de cerrarlo, la cifra de ingresos y la ganancia en el Resumen.
4. Reagrupar el menú del sitio del vendedor y agregar el grupo Utilidades en los dos sitios.

Los gastos se pueden usar antes de que la cifra de ingresos esté cerrada. La ganancia no se publica antes de ese cierre.
