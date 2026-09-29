# Requerimiento funcional y técnico: Nuevo módulo de Recaudo

Proyecto: Sistema de apuestas y loterías
Módulo: Recaudo
Aplicación operativa: el mismo APK Android actual, con un menú propio para el recaudador
Administración: vistas nuevas en el sitio web administrativo

Este documento deja cerradas las reglas definidas en la revisión del requerimiento.

## 1. Contexto actual

El sistema de apuestas está en producción. Hoy cuenta con:

- Vendedores que venden loterías desde el PDA.
- Dispositivos PDA asignados a los vendedores.
- Grupos a los que pertenecen los vendedores. Un vendedor pertenece a un solo grupo.
- Administrador y Super, que gestionan la operación.
- Observador, que usa el mismo APK con un menú propio.
- Ventas almacenadas en la base de datos actual. Cada venta tiene vendedor, fecha, total y tipo de apuesta.

El perfil Recaudador recoge el dinero que cada vendedor debe entregar. Ese valor es un porcentaje entero de sus ventas del día, en hora de Colombia. Si paga menos, la diferencia queda como saldo y se suma a lo que deba después.

El módulo consulta vendedores, grupos y ventas existentes. Las ventas no se duplican ni se modifican.

## 2. Objetivos

1. Crear el perfil Recaudador.
2. Permitir que el administrador asigne grupos y vendedores a cada recaudador.
3. Configurar el porcentaje de recaudo por grupo y, para quien no tenga grupo, un porcentaje individual.
4. Calcular la obligación de cada vendedor en el momento en que vende.
5. Registrar pagos completos y parciales desde el PDA, también sin señal.
6. Acumular el saldo pendiente como un solo valor.
7. Consultar el recaudo por vendedor, grupo y recaudador.
8. Consultar históricos de pagos y deudas, conservando quién recibió cada pago.
9. Ver indicadores y gráficos en la web y en el PDA.
10. Incorporar el recaudador al APK actual, con su propio menú.
11. Incorporar vistas administrativas nuevas.
12. Aislar tablas, procedimientos, controladores y vistas del módulo.

## 3. Perfil Recaudador

Se reutiliza la creación actual de usuarios. Al crear un Recaudador no se le asigna grupo: la asignación de grupos y vendedores se hace en `configRecaudo`.

Hoy `CK_Usuarios_Rol` solo admite Administrador, Vendedor, Observador y Super. Para crear el perfil se agrega Recaudador a esa lista. Los cuatro roles actuales siguen permitidos. Esa ampliación no modifica, no borra y no rechaza los usuarios que ya están en producción. No se alteran las demás tablas, relaciones ni procesos existentes.

El recaudador entra al mismo APK y al mismo login. El rol abre su menú, igual que hoy el vendedor abre el suyo y el observador el suyo. Administrador y Super no operan en el PDA. Las pantallas del vendedor y del observador permanecen como están. El vendedor no ve saldos ni pagos de recaudo.

El recaudador puede:

- Consultar sus grupos, sus vendedores y el total vendido del día.
- Ver el valor por cobrar y el saldo pendiente.
- Registrar el dinero recibido y confirmar el cobro.
- Imprimir la tirilla de cobro.
- Consultar historial, métricas y gráficos de lo suyo.
- Trabajar sin señal y sincronizar al recuperar la conexión.

No modifica la configuración administrativa ni consulta información de otros recaudadores.

## 4. Día de recaudo, ventas y obligación

El día de recaudo es el día calendario en hora de Colombia. Se cobran las ventas de ese mismo día. El módulo empieza a calcular el día de la salida a producción, sin deuda anterior.

Entran todas las ventas del vendedor en ese día: la suma de `Venta.Total`, de todos los tipos de apuesta, sin excluir estados del boleto.

El porcentaje es la parte que el vendedor debe entregar. Con 80 %, se cobra el 80 % de lo vendido y el vendedor conserva el 20 %.

La obligación del día nace cuando el vendedor registra una venta. Si después de un cobro vuelve a vender, esa venta aumenta la obligación de ese mismo día. La obligación del día es el porcentaje aplicado al total vendido del día. Si el resultado tiene parte decimal, se sube al peso siguiente. Si ya es un peso entero, se conserva. No se usan centavos.

Ejemplo: 10 % de $1.005 queda en $101. 10 % de $1.000 queda en $1.000.

Si el vendedor no vendió y no tiene saldo, la obligación del día es $0 y no aparece en pendientes. Si tiene saldo de días anteriores, aparece aunque hoy no haya vendido.

El porcentaje vigente para las ventas de un día es el que estaba configurado al comenzar ese día. Un cambio de porcentaje rige desde el día siguiente. Las ventas de hoy, incluidas las posteriores al cambio, siguen con el porcentaje de hoy. Las obligaciones ya guardadas no cambian.

Si el vendedor cambia de grupo, las obligaciones ya generadas conservan el porcentaje con el que se crearon. Las ventas posteriores al cambio usan el porcentaje del grupo nuevo.

Lo de hoy y el saldo anterior se muestran por separado. El pendiente es un solo valor: saldo anterior más obligación de hoy, menos los pagos aplicados. No se separa el dinero entre deuda vieja y deuda del día.

Ejemplo:

- Saldo anterior: $50.000.
- Obligación de hoy: $80.000.
- Pendiente: $120.000.
- Paga $30.000.
- Saldo del día siguiente: $90.000.

## 5. Configuración administrativa: `configRecaudo`

El administrador y Super configuran el módulo. Cada recaudador tiene su propia configuración.

Pueden:

- Consultar los usuarios con perfil Recaudador.
- Asignarle uno o varios grupos.
- Configurar el porcentaje de cada grupo.
- Asignarle vendedores que no tienen grupo.
- Configurar el porcentaje individual de esos vendedores.
- Retirar asignaciones.
- Consultar y modificar la configuración.

Se conserva recaudador, grupo, porcentaje, vendedores asignados, estado de la asignación y fechas de creación y modificación. Los cambios no reescriben el historial.

### 5.1. Porcentaje

El porcentaje es un entero de 1 a 100. No admite decimales. El 0 no es un porcentaje guardado: significa que falta configurar.

El porcentaje del grupo se aplica a cada vendedor del grupo, no al total del grupo.

Los grupos que ya existen aparecen con porcentaje 0, alarma gris y el aviso de que deben configurarse. Una ventana de alarmas lista los grupos que hay que corregir. Mientras el porcentaje sea 0, ese grupo no genera obligación.

La fila del grupo muestra, con el mismo tamaño de letra destacado:

- Porcentaje configurado.
- Total por recaudar del rango de fechas elegido en esa pantalla.
- Total recaudado de ese mismo rango.

Esos totales se actualizan al actuar en esa pantalla. Un pago hecho en el PDA no empuja la pantalla del administrador si nadie actúa en ella.

Total por recaudar es la suma de las obligaciones de los vendedores del grupo en el rango. Total recaudado es la suma de los pagos registrados a esos vendedores en el rango.

### 5.2. Asignación exclusiva

1. Un grupo solo puede estar asignado a un recaudador.
2. Un vendedor solo puede estar asignado a un recaudador.
3. Un recaudador puede tener varios grupos.
4. Si un grupo está asignado, todos sus vendedores heredan el porcentaje y la asignación. No pueden quedar asignados por fuera de ese grupo, ni al mismo recaudador ni a otro.
5. Si un vendedor entra después a un grupo ya asignado, pasa al recaudador de ese grupo y se retira su asignación individual.
6. No se puede asignar un grupo si alguno de sus vendedores ya está asignado a otro recaudador. El sistema muestra el conflicto. El administrador quita primero esa asignación en la otra ficha y luego asigna el grupo.
7. Las reglas se validan en la interfaz, en la API y en la base de datos.
8. Un vendedor no aparece duplicado en las listas de recaudo.

### 5.3. Vendedor sin grupo

«Personas asignadas» son vendedores asignados uno a uno. No hay otro tipo de persona.

El administrador puede asignar un vendedor sin grupo a un recaudador y guardarle un porcentaje del 1 al 100.

Ya asignado, se le cobra con ese porcentaje individual. Entra a pendientes y a cobrados, y también aparece en Todos. En las tres listas lleva la señal de que debe ingresar a un grupo. Hay filtros y cada columna se puede ordenar.

Al entrar a un grupo, hereda el porcentaje y el recaudador de ese grupo. La señal desaparece.

### 5.4. Retiro y cambio de recaudador

Si el vendedor sale del grupo y no tiene otra asignación, deja de aparecerle al recaudador en ese momento. El administrador sigue viendo la deuda. Ningún recaudador puede cobrarla hasta una nueva asignación.

Cuando un recaudador nuevo recibe al vendedor, cobra el saldo acumulado. El historial sigue mostrando quién recibió cada pago anterior.

## 6. APK del recaudador

Es el mismo APK. El recaudador tiene pantallas, navegación y menú propios, contra las API `/api/Recaudo/`. No hay sitio web para el recaudador.

### 6.1. Pantalla principal

Al ingresar ve:

- Grupos asignados.
- Vendedores asignados, con nombre completo y alias. No se muestra el celular.
- Pendientes, cobrados y todos.
- Indicadores del día.
- Filtros y orden por cada columna.

El periodo por defecto es el día de hoy.

Hay tres listas. Dentro de cada una, los vendedores van agrupados por grupo. Los que no tienen grupo van en su propia sección, con la señal de que deben ingresar a un grupo, y se les cobra con su porcentaje individual.

- Pendientes: todavía no tienen un pago mayor que cero en el día.
- Cobrados: ya tienen al menos un pago mayor que cero en el día.
- Todos: todos los asignados, en cualquier estado.

### 6.2. Columnas

| Campo | Descripción |
| --- | --- |
| Vendedor | Nombre completo y alias |
| Total vendido | Ventas del día |
| Valor a cobrar | Obligación del día, según el porcentaje |
| Total pendiente | Saldo anterior más obligación de hoy, menos pagos |
| Valor recibido | Valor digitado por el recaudador |
| Estado | Por cobrar, deudado o al día |
| Acción | Registrar el cobro |

### 6.3. Colores

El amarillo no se usa. Los estados son:

| Situación | Estado | Color | Lista |
| --- | --- | --- | --- |
| Debe solo lo de hoy y todavía no ha pagado | Por cobrar | Verde | Pendientes |
| Arrastra saldo de otros días y todavía no ha pagado | Deudado | Rojo | Pendientes |
| Ya pagó una parte y sigue debiendo | Deudado | Rojo | Cobrados |
| El saldo quedó en cero | Al día | Azul | Cobrados |

Al día siguiente vuelve a pendientes si tiene obligación nueva o saldo. Si no vendió y el saldo está en cero, no aparece en pendientes.

### 6.4. Registro del pago

El valor se digita en la fila. Acepta pesos colombianos, muestra separadores de miles y rechaza texto, negativos, cero y un valor mayor que el pendiente. El separador es solo visual: a la API viaja el número entero.

Puede haber varios pagos del mismo vendedor en el mismo día. El primero mayor que cero lo saca de pendientes y lo pasa a cobrados. Otro pago del mismo día se registra desde cobrados o desde Todos.

El modal muestra:

- Nombre del vendedor.
- Total pendiente: saldo anterior más obligación de hoy.
- Valor que se recibirá: lo digitado.
- Saldo que quedará: total pendiente menos lo digitado.

Aceptar registra el pago. Cancelar cierra el modal sin registrar nada. Al terminar se actualizan listas, saldo e indicadores, y se imprime la tirilla de cobro.

Un reintento idéntico, por doble toque o por una falla de red, no crea otro pago. La respuesta es la del pago ya registrado.

### 6.5. Sin señal

Con conexión, el PDA descarga lo pendiente de cobro del recaudador. Sin señal, registra los pagos en la base local y completa el proceso. Al volver la conexión, el sincronizador del menú sube los pagos y baja los cambios, con la misma dinámica del sincronizador actual del vendedor. La sincronización de ventas del vendedor no cambia.

Si durante el tiempo sin señal el vendedor registró ventas nuevas, al sincronizar esas ventas aumentan la obligación de ese día y el saldo pendiente.

### 6.6. Tirilla de cobro

Al confirmar el pago se imprime la tirilla con la impresora del PDA y el documento queda registrado como Tirilla de cobro, dentro del módulo Recaudo.

Por ahora la tirilla lleva:

- Recaudador.
- Vendedor.
- Fecha y hora.
- Valor recibido.
- Saldo que queda.
- Consecutivo.

El texto completo se definirá después.

## 7. Panel administrativo

Las vistas son nuevas. Los identificadores de menú llevan el sufijo `Recaudo`:

| Elemento | Identificador |
| --- | --- |
| Configuración | `configRecaudo` |
| Panel | `panelRecaudo` |
| Historial | `historialRecaudo` |
| Métricas | `metricasRecaudo` |
| Detalle | `detalleRecaudo` |

Los filtros de grupo y de vendedor viven dentro de `panelRecaudo` e `historialRecaudo`. El administrador no registra, no anula y no corrige pagos. El pago nace en el PDA.

### 7.1. Listado de recaudadores

| Campo | Descripción |
| --- | --- |
| Recaudador | Nombre |
| Grupos asignados | Grupos a cargo |
| Personas asignadas | Vendedores asignados sin grupo |
| Total por recaudar | Obligaciones del periodo |
| Total recaudado | Dinero recibido |
| Saldo pendiente | Diferencia |
| Porcentaje recaudado | Avance |
| Acción | Ver detalle |

El periodo por defecto es hoy.

### 7.2. Detalle del recaudador

Muestra la información del recaudador, sus grupos y vendedores, total por recaudar, total recaudado, saldo pendiente, porcentaje de recaudo, historial y gráficos. Cada grupo va en una sección, con sus vendedores y valores.

### 7.3. Cobrados y pendientes

Hay dos tablas de vendedores:

Cobrados:

- Grupo.
- Vendedor.
- Recaudador responsable.
- Valor que debía pagar.
- Valor recibido.
- Saldo pendiente.
- Fecha y hora del pago.
- Estado.

Pendientes:

- Grupo.
- Vendedor.
- Recaudador responsable.
- Valor que debe pagar.
- Deuda anterior.
- Total pendiente.
- Estado.

Se actualizan cuando el administrador consulta o actúa en la pantalla.

### 7.4. Consulta por grupo

Dentro del panel y del historial se puede consultar nombre del grupo, recaudador, porcentaje, vendedores, total vendido, total por recaudar, total recaudado, saldo pendiente e historial.

### 7.5. Consulta por vendedor

Dentro del panel y del historial se puede consultar nombre, grupo actual, recaudador asignado, recaudador que registró cada pago, ventas diarias, porcentaje aplicado, valor generado, valor pagado, deuda acumulada e historial completo.

## 8. Historial

El administrador y Super consultan todo. El recaudador consulta el historial de sus asignados.

| Campo | Descripción |
| --- | --- |
| Fecha | Día de la obligación |
| Ventas | Total vendido |
| Porcentaje | Porcentaje aplicado |
| Valor generado | Obligación del día |
| Deuda anterior | Saldo anterior |
| Total por pagar | Obligación más saldo anterior |
| Valor pagado | Total recibido ese día |
| Saldo pendiente | Deuda resultante |
| Recaudador | Usuario que recibió el dinero |
| Estado | Por cobrar, deudado o al día |

El historial se conserva si el vendedor o el grupo dejan de estar asignados. Cada pago guarda el recaudador que lo recibió.

El administrador filtra el historial de recaudos por recaudador, grupo, vendedor, fecha inicial, fecha final y estado. Cada movimiento muestra fecha y hora, recaudador, vendedor, grupo, valor recibido, obligación afectada y saldo resultante.

## 9. Gráficos y métricas

El administrador los ve en la web, sobre toda la operación. El recaudador los ve en el PDA, sobre lo suyo.

En el gráfico del vendedor, arriba se muestra la deuda acumulada en rojo, dos puntos mayor que el texto normal. Por día muestra valor pagado y valor pendiente.

El gráfico del recaudador por grupo muestra, por día, lo que debía recaudar el grupo, lo recaudado y lo pendiente.

Las métricas generales son:

- Total vendido.
- Total por recaudar.
- Total recaudado.
- Total pendiente.
- Porcentaje de recaudo.
- Vendedores al día.
- Vendedores en deuda.
- Grupos con obligaciones pendientes.
- Recaudo diario por recaudador.
- Recaudo acumulado por grupo.

Los indicadores salen de los mismos registros de obligaciones y pagos.

## 10. Seguridad

Administrador y Super pueden crear la configuración, asignar grupos y vendedores, configurar porcentajes y consultar cualquier recaudador, vendedor, grupo, historial, métrica, cobrado y pendiente.

El recaudador solo consulta y cobra lo suyo. La API valida en el servidor que el recaudador autenticado tenga asignado el vendedor o el grupo solicitado.

## 11. Integración con las ventas

Antes de implementar el cálculo se revisan las tablas de ventas, su relación con vendedores y grupos, la fecha, los estados y la forma de obtener el total vendido por vendedor y día.

La regla funcional ya cerrada es: el total vendido del día es la suma de `Venta.Total` de ese vendedor, de todos los tipos de apuesta. Las ventas no se anulan ni se modifican desde este módulo. No se duplican ni se altera su tabla.

## 12. Base de datos

Las tablas nuevas van en el esquema `recaudo`.

Está prohibido modificar tablas existentes, eliminar columnas, cambiar relaciones, alterar procedimientos actuales, cambiar la lógica de ventas o modificar vistas, controladores y pantallas del vendedor y del observador.

La única estructura existente que se amplía es `CK_Usuarios_Rol`, para admitir Recaudador junto a Administrador, Vendedor, Observador y Super.

Tablas nuevas para:

- Configuración de recaudadores.
- Asignación de grupos.
- Asignación individual de vendedores sin grupo.
- Porcentajes.
- Obligaciones diarias.
- Pagos.
- Aplicación del pago sobre el saldo único.
- Tirilla de cobro.
- Historial de configuración.
- Auditoría.

La exclusividad de grupo y de vendedor queda reforzada en la base de datos.

Cada obligación guarda las ventas usadas, el porcentaje aplicado, el valor generado y la fecha. Un cambio posterior de configuración no las altera.

## 13. Procedimientos almacenados

Los procedimientos actuales no se modifican. Los nuevos son procedimientos de SQL Server en el esquema `recaudo`, con el prefijo `sp_recaudo_`.

Ejemplo: `recaudo.sp_recaudo_registrar_pago`.

| Procedimiento | Responsabilidad |
| --- | --- |
| `recaudo.sp_recaudo_configurar_recaudador` | Configurar recaudador |
| `recaudo.sp_recaudo_asignar_grupo` | Asignar grupo |
| `recaudo.sp_recaudo_asignar_vendedor` | Asignar vendedor sin grupo |
| `recaudo.sp_recaudo_configurar_porcentaje` | Configurar porcentaje |
| `recaudo.sp_recaudo_consultar_ventas` | Consultar ventas del día |
| `recaudo.sp_recaudo_generar_obligaciones` | Generar o aumentar la obligación al registrar la venta |
| `recaudo.sp_recaudo_registrar_pago` | Registrar pago |
| `recaudo.sp_recaudo_consultar_vendedores` | Consultar asignados |
| `recaudo.sp_recaudo_consultar_grupos` | Consultar grupos |
| `recaudo.sp_recaudo_consultar_historial` | Consultar históricos |
| `recaudo.sp_recaudo_metricas_recaudador` | Métricas del recaudador |
| `recaudo.sp_recaudo_metricas_grupo` | Métricas por grupo |

La API llama estos procedimientos. El registro del pago y la actualización del saldo van en una transacción.

## 14. API

Los controladores nuevos incluyen `Recaudo` en el nombre. Las rutas usan `/api/Recaudo/`. Los controladores existentes no se modifican. `BoletosRecaudo` no existe y no forma parte de este módulo.

| Método | Controlador | Ruta |
| --- | --- | --- |
| GET | `GruposRecaudo` | `/api/Recaudo/Grupos` |
| GET | `VendedoresRecaudo` | `/api/Recaudo/Vendedores` |
| GET | `ObligacionesRecaudo` | `/api/Recaudo/Obligaciones` |
| POST | `PagosRecaudo` | `/api/Recaudo/Pagos` |
| GET | `HistorialRecaudo` | `/api/Recaudo/Historial/Vendedor/{id}` |
| GET | `MetricasRecaudo` | `/api/Recaudo/Metricas` |

La API administrativa permite consultar recaudadores, configurar asignaciones, asignar y retirar grupos, asignar y retirar vendedores sin grupo, configurar porcentajes, consultar históricos, métricas, cobrados y pendientes.

La API del recaudador solo entrega y recibe lo asignado a ese recaudador. También atiende la descarga para el PDA y la sincronización de los pagos hechos sin señal.

## 15. Reglas de integridad

Todas las consultas, búsquedas, pagos, sincronizaciones, cargas e indicadores muestran un spinner mientras la operación está en curso. El spinner desaparece al terminar, también si hay error, y bloquea un segundo envío durante ese tiempo. Los errores muestran un mensaje claro.

Los valores en pesos se muestran con separador de miles: $1.000, $25.000, $150.000, $1.000.000. El valor guardado y enviado es un entero.

El sistema impide generar dos veces la obligación del mismo vendedor y día: una venta nueva aumenta la obligación de ese día. Impide pagos duplicados por reintento, asignaciones dobles y vendedores repetidos en las listas.

La auditoría guarda usuario, fecha y hora, tipo de operación, valor, obligación afectada e identificador del movimiento.

Los pagos confirmados no se eliminan ni se corrigen.

## 16. Permisos

| Acción | Administrador y Super | Recaudador |
| --- | --- | --- |
| Configurar asignaciones y porcentajes | Sí | No |
| Consultar toda la operación | Sí | No |
| Cobrar y consultar lo asignado | No opera en el PDA | Sí |
| Ver gráficos de lo suyo | Sí, y además el consolidado | Sí |
| Registrar o corregir un pago en la web | No | No |

## 17. Criterios de aceptación

- Se crea un recaudador sin grupo.
- Agregar el rol Recaudador no altera los usuarios ya existentes.
- El administrador asigna grupos y vendedores sin grupo.
- Al vendedor sin grupo, ya asignado, se le cobra con su porcentaje individual y sigue visible la señal de que debe ingresar a un grupo.
- Un grupo y un vendedor no quedan en dos recaudadores.
- Los vendedores de un grupo asignado heredan esa asignación.
- El porcentaje es un entero de 1 a 100.
- Un grupo sin porcentaje se muestra en 0, con alarma gris, y aparece en la ventana de alarmas.
- La fila del grupo muestra porcentaje, total por recaudar y total recaudado del rango elegido, con el mismo tamaño de letra.
- La obligación nace con la venta y aumenta si hay más ventas ese día.
- El cálculo usa todas las ventas del día y redondea hacia arriba al peso.
- El pendiente es un solo saldo.
- No hay deuda anterior a la salida a producción.
- El recaudador opera en el mismo APK, con menú propio.
- Existen pendientes, cobrados y todos, con filtros y orden por columna.
- Por cobrar es verde, deudado es rojo y al día es azul.
- El primer pago del día pasa al vendedor a cobrados.
- Puede pagar varias veces el mismo día, desde cobrados o desde Todos.
- El modal pide confirmación y no acepta cero ni un valor mayor que el pendiente.
- Un reintento idéntico no duplica el pago.
- Sin señal, el pago queda en el PDA y se sincroniza al conectar.
- La tirilla de cobro se imprime y se registra con los campos definidos.
- El vendedor no ve el recaudo en su menú.
- El administrador consulta dos tablas, cobrados y pendientes, y no registra pagos.
- Los gráficos están en la web y en el PDA.
- Las tablas nuevas están en `recaudo`.
- Los procedimientos nuevos son `recaudo.sp_recaudo_...`.
- Las API nuevas usan `/api/Recaudo/` y controladores con `Recaudo` en el nombre.
- Los pagos confirmados no se eliminan ni se modifican.
- No se modifican tablas, procedimientos, controladores ni vistas del vendedor y del observador, fuera de la ampliación del check de roles.

## 18. Principio de implementación

El módulo Recaudo es una extensión del sistema actual. Reutiliza la autenticación, la identificación de vendedores, los grupos, las ventas, el ingreso del APK, el sincronizador y la impresora.

La operación del recaudador ocurre en su menú del APK. La configuración y la consulta general ocurren en vistas nuevas del sitio administrativo.

Las tablas, los procedimientos, las API, las vistas, los permisos y la tirilla de cobro son propios del módulo.

## 19. Vendedor sin grupo ya asignado

Quedó la opción A.

Un vendedor sin grupo, con porcentaje individual y asignado a un recaudador, se cobra con ese porcentaje. Entra a pendientes y a cobrados, y también aparece en Todos. La señal de que debe ingresar a un grupo se mantiene hasta que tenga grupo.
