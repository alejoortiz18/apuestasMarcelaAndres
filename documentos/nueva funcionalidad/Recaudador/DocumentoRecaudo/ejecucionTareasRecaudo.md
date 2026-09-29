# Ejecución de tareas: módulo Recaudo

Requerimiento: `Requerimiento_Modulo_Recaudo_actualizado.md`
Plan: `planCambioRecaudo.md`
Rama: `RecaudoDllo`

Las tareas se hacen en este orden. Una termina y se comprueba antes de empezar la siguiente. Todo ocurre en local: API `http://localhost:5295`, administrador `http://localhost:5274` y base `NewRich` de `LAPTOP-CQSL6T3I`.

Cada tarea de código se implementa así:

1. Se escribe la prueba que describe la regla.
2. Se ejecuta y se confirma que falla por la regla, no por un error de compilación ajeno.
3. Se escribe el código mínimo que la pone en verde.
4. Se vuelven a ejecutar las pruebas de esa tarea.
5. Se ejecutan las pruebas que ya existían. Si una falla, la tarea no sigue.

## La funcionalidad actual queda intacta

Esta condición vale para las 23 tareas. El recaudo se agrega al lado. Venta, boleto, tirilla del vendedor, sincronización del vendedor, observador, grupos, loterías, resultados, premios, dispositivos, usuarios ya creados y el resto de pantallas del administrador siguen comportándose como hoy.

En cada tarea se cumple lo siguiente:

- No se modifican tablas, columnas, relaciones ni procedimientos que ya existen. La única ampliación en una estructura existente es `CK_Usuarios_Rol`, y solo para admitir el valor Recaudador junto a los cuatro roles actuales.
- No se cambia el cálculo, el guardado ni la impresión de una venta. Cuando la tarea 22 avise al recaudo, eso ocurre después de que la venta ya quedó guardada. Si el recaudo falla, la venta permanece.
- No se editan las pantallas ni el menú del vendedor y del observador. El recaudador entra por un destino nuevo.
- No se editan los controladores, vistas ni menús administrativos que ya existen. Las pantallas de recaudo son nuevas. En usuarios solo se agrega el rol Recaudador al crear, y el vendedor sigue obligando grupo.
- Las pruebas de `NewRich.UnitTests`, `NewRich.Admin.Tests` y `NewRich.Pda.Tests` que ya pasan siguen en verde al cerrar cada tarea.
- Un recorrido corto de lo actual se repite al cerrar las tareas 8, 12, 17, 20, 21 y 22: login de vendedor, registro de una venta, sincronización del vendedor, login de observador y una pantalla administrativa que ya existía.

Si una de esas comprobaciones cambia de resultado, se revierte el cambio de esa tarea antes de continuar.
5. Se ejecutan las pruebas que ya existían de venta, usuario, grupo, vendedor y observador. Si alguna falla, la tarea no sigue.

## La funcionalidad actual queda intacta

Ninguna tarea cambia el comportamiento que hoy está en uso. El recaudo se agrega al lado. Venta, boleto, usuario, grupo, dispositivo, vendedor, observador, administrador y super siguen respondiendo como antes de este módulo.

Eso se cumple así:

- No se modifican tablas, columnas, relaciones ni procedimientos de ventas, boletos, usuarios, grupos ni dispositivos. La única ampliación en una estructura ya existente es `CK_Usuarios_Rol`, y solo para agregar el valor Recaudador. Administrador, Vendedor, Observador y Super siguen admitidos.
- No se editan las pantallas, menús ni flujos del vendedor y del observador. El recaudador entra por un menú nuevo.
- No se reutiliza un controlador ya existente para las funciones nuevas. Lo nuevo va en controladores, vistas y rutas `Recaudo`.
- Leer ventas para calcular la obligación no escribe en esas tablas.
- El paso que aumenta la obligación ocurre después de que la venta ya quedó guardada. Si el recaudo falla, la venta permanece. El procedimiento de la venta no se toca.
- La sincronización y la impresión del vendedor conservan su camino. El recaudador tiene los suyos.
- `Proyectos/Administrador/appsettings.json` no se edita.

Antes de dar por cerrada una tarea que toque código o SQL ya existente, se comprueba en local:

- Un vendedor sigue exigiendo grupo al crearse y puede vender.
- Un observador sigue entrando a su menú.
- Administrador y Super siguen sin operar en el PDA y siguen entrando al sitio.
- Una venta de prueba conserva su total, su vendedor y su boleto.
- Los conteos de usuarios por rol, tomados antes del cambio, siguen iguales.

La prueba de dominio vive en `Proyectos/API/tests/NewRich.UnitTests`. La de PDA vive en `Proyectos/Android/tests/NewRich.Pda.Tests`. La de SQL vive en `Proyectos/API/tests/NewRich.IntegrationTests` y solo corre si `@@SERVERNAME` es el equipo local.

## 1. Comprobar el ambiente local

Se confirma la rama `RecaudoDllo`. Se levanta la API y el administrador con el perfil Development. Se entra a `http://localhost:5274` y se comprueba que la API responde en `http://localhost:5295`.

En la base local se ejecuta `SELECT @@SERVERNAME`. Si el nombre no es el equipo local, no se sigue. Se anota un vendedor, su grupo y las ventas de un día ya guardadas, para usarlas como dato de prueba.

No se modifica código ni `Proyectos/Administrador/appsettings.json`.

## 2. Porcentaje y redondeo

Se prueba que el porcentaje solo acepta enteros del 1 al 100, que el 0 significa sin configurar y que la obligación del día es el porcentaje aplicado a la suma de ventas. 10 % de $1.005 queda en $101. 10 % de $1.000 queda en $1.000.

Se implementa en `Proyectos/API/src/NewRich.Domain/Services`, en una regla pura, sin base de datos ni HTTP. El día se calcula con la hora de Colombia que ya existe en `ZonaHorariaColombia`.

## 3. Saldo único

Se prueba el caso cerrado: saldo anterior $50.000 más obligación de hoy $80.000 dan $120.000. Un pago de $30.000 deja $90.000. Lo de hoy y el saldo anterior se informan por separado, y el pendiente es un solo número.

Se implementa en la misma regla de dominio. Una venta posterior del mismo día vuelve a calcular la obligación sobre el total vendido del día y sube el pendiente.

## 4. Reglas del pago

Se prueba el rechazo de cero, de negativos y de un valor mayor que el pendiente. Se prueba que el mismo vendedor puede recibir varios pagos el mismo día y que un reintento con la misma clave de idempotencia devuelve el pago ya registrado.

Se implementa en dominio. La clave de idempotencia viaja desde el PDA y desde la API. En este paso todavía no se guarda en SQL.

## 5. Estado y color

Se prueba la tabla cerrada:

- Debe solo lo de hoy y no ha pagado: por cobrar, verde, lista pendientes.
- Arrastra saldo y no ha pagado: deudado, rojo, lista pendientes.
- Pagó una parte y sigue debiendo: deudado, rojo, lista cobrados.
- Saldo en cero: al día, azul, lista cobrados.
- Sin ventas y sin saldo: no aparece en pendientes.

Se implementa en dominio. El primer pago mayor que cero del día cambia la lista a cobrados.

## 6. Asignación exclusiva

Se prueba que un grupo y un vendedor solo pueden pertenecer a un recaudador, que un recaudador puede tener varios grupos y que los vendedores de un grupo asignado heredan porcentaje y recaudador. También se prueba el bloqueo cuando un miembro ya está asignado a otro recaudador, el paso automático al entrar después a un grupo asignado, y el cobro del vendedor sin grupo con su porcentaje individual.

Se implementa en dominio. El vendedor sin grupo queda marcado para mostrar la señal de que debe ingresar a un grupo. Al entrar a un grupo, hereda el porcentaje del grupo y la señal se quita.

## 7. Esquema `recaudo` en la base local

Se escribe un script SQL nuevo en el repositorio. Antes de ejecutarlo se vuelve a leer `@@SERVERNAME`.

El script crea el esquema `recaudo` y las tablas de configuración, asignación de grupos, asignación de vendedores sin grupo, porcentajes, obligaciones diarias, pagos, tirilla de cobro, historial de configuración y auditoría. Las claves únicas impiden dos recaudadores para el mismo grupo o el mismo vendedor. La obligación de un vendedor y un día es una sola fila: una venta nueva la aumenta, no crea otra.

No se alteran tablas de ventas, boletos, usuarios, grupos ni dispositivos, ni sus procedimientos. Al terminar, una venta de prueba conserva su total, su vendedor y su boleto.

## 8. Rol Recaudador en la base local y en el código

En la base local se cuentan los usuarios por rol. Se amplía `CK_Usuarios_Rol` para admitir Recaudador junto a Administrador, Vendedor, Observador y Super. Se vuelven a contar: las cantidades anteriores quedan iguales.

En código se agrega `Recaudador` a `RolUsuario`. La creación de usuario, que hoy obliga grupo solo al vendedor, acepta al recaudador sin grupo. La prueba cubre ese caso y cubre que el vendedor sigue exigiendo grupo.

La pantalla actual de usuarios muestra el rol nuevo. No se crea otra pantalla de usuarios. Al terminar, se crea un vendedor con grupo y un administrador como hoy, y se ejecutan las pruebas de usuarios que ya existían. Esas pruebas siguen en verde. Las pruebas ya existentes de creación de vendedor, administrador, observador y super siguen en verde. Un vendedor sin grupo sigue rechazado.

## 9. Procedimientos de configuración y consulta

Se crean, en el mismo script local, los procedimientos `recaudo.sp_recaudo_configurar_recaudador`, `recaudo.sp_recaudo_asignar_grupo`, `recaudo.sp_recaudo_asignar_vendedor`, `recaudo.sp_recaudo_configurar_porcentaje`, `recaudo.sp_recaudo_consultar_ventas`, `recaudo.sp_recaudo_consultar_grupos` y `recaudo.sp_recaudo_consultar_vendedores`.

`consultar_ventas` lee `Venta.Total` del vendedor y del día, de todos los tipos de apuesta, y no escribe en esas tablas. `generar_obligaciones` se crea en la tarea 11.

La prueba de integración ejecuta cada procedimiento contra la base local y comprueba el nombre del servidor antes de correr.

## 10. Casos de uso de configuración

Se agregan los contratos en `NewRich.Application` y la implementación en `NewRich.Infrastructure`. La infraestructura llama los procedimientos con `ExecuteSql` o `FromSql`, con parámetros tipados. Los controladores no abren SQL.

Los casos de uso aplican las reglas de la tarea 6 y persisten el resultado. Un porcentaje nuevo queda guardado para el día siguiente. Las obligaciones ya generadas conservan el porcentaje anterior.

## 11. Obligación leída desde las ventas locales

Se crea `recaudo.sp_recaudo_generar_obligaciones`. Calcula la obligación con la regla de la tarea 2 a partir de las ventas ya guardadas. Si el grupo está en 0, no genera obligación. Si el vendedor no tiene grupo, usa su porcentaje individual.

La API expone:

- `GET /api/Recaudo/Grupos` en `GruposRecaudo`.
- `GET /api/Recaudo/Vendedores` en `VendedoresRecaudo`.
- `GET /api/Recaudo/Obligaciones` en `ObligacionesRecaudo`.

La prueba de aceptación usa el vendedor anotado en la tarea 1: el total por recaudar coincide con el porcentaje redondeado hacia arriba.

## 12. Pantalla `configRecaudo`

Se crea un controlador y vistas nuevas en `Proyectos/Administrador`. El controlador llama la API local. No consulta SQL.

La pantalla lista recaudadores, asigna grupos y vendedores sin grupo, guarda el porcentaje y muestra la fila con porcentaje, total por recaudar y total recaudado del rango de fechas elegido. Los tres valores usan el mismo tamaño de letra. Los grupos en 0 se ven en gris y una ventana lista los que faltan por configurar. El spinner cubre cada operación.

Se prueba en `http://localhost:5274`: crear el recaudador, asignar el grupo, elegir el rango y ver los totales. En el mismo sitio se abre una pantalla que ya existía, por ejemplo grupos o ventas, y se comprueba que sigue respondiendo igual. En el mismo sitio se abre un flujo que ya existía, por ejemplo grupos o ventas, y se comprueba que sigue operando.

## 13. Registro del pago

Se crea `recaudo.sp_recaudo_registrar_pago`. En una transacción guarda el pago, actualiza el saldo único, escribe la auditoría y rechaza el duplicado por la clave de idempotencia. No hay procedimiento de borrar ni de corregir un pago.

La API expone `POST /api/Recaudo/Pagos` en `PagosRecaudo`. El recaudador solo puede pagar un vendedor que tenga asignado. Administrador y Super no registran pagos por esta ruta.

La prueba de integración reproduce $50.000 + $80.000 − $30.000 = $90.000, un segundo pago el mismo día y un reintento con la misma clave.

## 14. Tirilla de cobro

Al confirmar el pago, el mismo caso de uso guarda la tirilla en `recaudo` con recaudador, vendedor, fecha y hora, valor recibido, saldo que queda y consecutivo. El texto largo de la tirilla queda para después. Esta tarea solo deja el registro y el dato que después imprimirá el PDA.

## 15. Historial

Se crea `recaudo.sp_recaudo_consultar_historial` y `GET /api/Recaudo/Historial/Vendedor/{id}` en `HistorialRecaudo`. Cada fila conserva el recaudador que recibió el pago, aunque la asignación cambie después.

El recaudador solo recibe el historial de sus asignados. Administrador y Super consultan con filtros de recaudador, grupo, vendedor, fecha inicial, fecha final y estado.

## 16. Panel, detalle y métricas del administrador

Se crean las vistas `panelRecaudo`, `detalleRecaudo`, `historialRecaudo` y `metricasRecaudo`. Los filtros de grupo y de vendedor viven dentro del panel y del historial.

El panel lista recaudadores con grupos, personas asignadas, totales y avance. El detalle separa cada grupo. Hay dos tablas de vendedores: cobrados y pendientes. Las métricas y los gráficos salen de las mismas obligaciones y pagos. El periodo por defecto es hoy. Los indicadores se refrescan al actuar en la pantalla.

Se prueban en `http://localhost:5274` con el pago registrado en la tarea 13.

## 17. Menú del recaudador en el APK

En `NavegacionPorRol` se agrega el destino del rol Recaudador, al lado de vendedor y observador. Administrador y Super siguen sin operar en el PDA.

Se crea el shell y la navegación propios en `Proyectos/Android/src/NewRich.Maui`. Las páginas del vendedor y del observador no se modifican. La prueba de `NewRich.Pda.Tests` comprueba que un recaudador entra a su shell, que un vendedor sigue entrando al suyo y que un observador sigue entrando al suyo. Las pruebas del vendedor y del observador que ya existían se ejecutan otra vez y siguen en verde.

## 18. Listas y cobro en el PDA

La pantalla principal llama `GET /api/Recaudo/Obligaciones` de la API local. Muestra pendientes, cobrados y todos, agrupados por grupo. Los vendedores sin grupo van en su sección, con la señal, y se pueden cobrar.

Cada fila muestra nombre completo, alias, total vendido, valor a cobrar, total pendiente, valor recibido y estado. Hay filtros y orden por columna. El valor se digita en la fila. El modal muestra total pendiente, valor digitado y saldo que quedará, y solo confirma o cancela. Los colores salen de la regla de la tarea 5. Al aceptar se llama `POST /api/Recaudo/Pagos`.

Se prueba con la compilación Debug contra `http://localhost:5295`, en el teléfono conectado. No se copia el APK a `inetpub`, al blob ni a `documentos\APKTrest`.

## 19. Gráficos del recaudador

En el PDA se muestran la deuda acumulada del vendedor y el recaudo diario del grupo, usando `GET /api/Recaudo/Metricas`. El recaudador solo ve lo suyo. Los gráficos del administrador ya quedaron en la tarea 16.

## 20. Base local del PDA y sincronización

Con conexión, el PDA descarga obligaciones y saldos a su base local. Sin conexión, el pago se guarda ahí con la clave de idempotencia y la tirilla. El sincronizador del menú del recaudador sube los pagos pendientes y baja las obligaciones nuevas. Si durante la desconexión hubo ventas, al sincronizar sube el pendiente de ese día.

La sincronización de ventas del vendedor conserva su camino y su menú. La prueba nueva cubre un pago de recaudo hecho sin red y un solo registro después de sincronizar. La prueba de sincronización de ventas que ya existía se ejecuta otra vez y sigue en verde.

## 21. Impresión de la tirilla

Al confirmar el pago, el PDA imprime con la impresora que ya usa el vendedor. El contenido de la tirilla de cobro sale del registro de la tarea 14: recaudador, vendedor, fecha y hora, valor recibido, saldo que queda y consecutivo. El formato largo se sustituye cuando se defina, sin cambiar el registro. El armado y la impresión de la tirilla de venta no se modifican. La prueba de impresión de venta que ya existía se ejecuta otra vez y sigue en verde.

## 22. La obligación nace con la venta

Este paso se hace cuando las tareas 1 a 21 están probadas.

Después de que la venta local ya quedó guardada, la API llama `recaudo.sp_recaudo_generar_obligaciones` para ese vendedor y ese día. El procedimiento y las tablas de la venta no se modifican. Si la llamada de recaudo falla, la venta permanece guardada y el error queda registrado para reintento.

No se genera obligación si el vendedor no tiene recaudador o si su grupo está sin porcentaje. Si no tiene grupo, pero está asignado y tiene porcentaje individual, la obligación usa ese porcentaje.

La prueba crea una venta en local y comprueba que el pendiente del recaudador sube sin duplicar la venta. Otra prueba repite una venta de un vendedor sin recaudador y comprueba que la venta queda guardada, con el mismo total, y que no aparece obligación. Las pruebas de confirmación de venta que ya existían se ejecutan otra vez y siguen en verde.

## 23. Recorrido completo en local

Se ejecuta una sola vez, en este orden, sobre la base local:

1. Crear el recaudador sin grupo.
2. Asignarle un grupo con porcentaje y un vendedor sin grupo con porcentaje individual.
3. Registrar una venta del vendedor del grupo y ver crecer la obligación.
4. En el PDA, cobrar una parte, verlo en cobrados y en rojo, e imprimir la tirilla.
5. Cobrar el resto el mismo día y verlo al día, en azul.
6. Apagar la red, registrar otro pago y sincronizar al reconectar, sin duplicarlo.
7. En el administrador, ver el panel, las dos tablas, el historial y el gráfico con el mismo saldo.
8. Repetir, sin cambios de resultado, un login de vendedor, una venta, la sincronización del vendedor, un login de observador y una pantalla administrativa que ya existía.

Si un paso de recaudo no coincide con el requerimiento, se corrige esa tarea y se repite el recorrido desde el punto fallido. Si el paso 8 cambia el comportamiento actual, se revierte la tarea que lo provocó.
