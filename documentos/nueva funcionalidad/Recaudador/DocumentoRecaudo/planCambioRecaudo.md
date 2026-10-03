# Plan de trabajo: módulo Recaudo en local

Requerimiento de referencia: `Requerimiento_Modulo_Recaudo_actualizado.md`

Rama de trabajo: `RecaudoDllo`

Este plan describe cómo se integra el módulo en el ambiente local. No incluye publicación, scripts sobre la base de producción ni instalación del APK de subida.

## 1. Límite del ambiente

Todo el desarrollo y la prueba ocurren en este equipo.

| Pieza | Destino local |
| --- | --- |
| API | `http://localhost:5295`, perfil Development |
| Sitio administrativo | `http://localhost:5274`, perfil Development, API en `http://localhost:5295/` |
| Base de datos | `Server=localhost;Database=NewRich` en este equipo (`DESKTOP-JLLF9LK`), autenticación de Windows |
| APK | Compilación Debug en el dispositivo USB, solo cuando el corte del PDA esté listo para probarse |

El archivo `Proyectos/Administrador/appsettings.json` sigue apuntando a la API de Azure. No se edita. El sitio local usa `appsettings.Development.json`.

No se ejecuta SQL fuera de `localhost` en `DESKTOP-JLLF9LK`. No se copia el APK a `C:\inetpub`, al blob ni a `documentos\APKTrest`. No se cambia la cadena de conexión de producción ni se publican sitios.

Antes de cada script se comprueba el servidor:

```text
SELECT @@SERVERNAME
```

El resultado esperado es `DESKTOP-JLLF9LK`. Si el nombre no corresponde, el script no se ejecuta.

## 2. Cómo entra el módulo al sistema actual

El recaudo se agrega al lado de lo que ya existe. La venta, el boleto, la tirilla del vendedor, su sincronización, el observador, los grupos, las loterías, los resultados, los premios, los dispositivos y las pantallas administrativas actuales siguen su camino. Al cerrar cada tarea se vuelven a ejecutar las pruebas que ya existían. Si una falla, esa tarea se revierte antes de seguir.

```text
PDA o sitio administrativo
  -> API /api/Recaudo/
  -> caso de uso en NewRich.Application
  -> regla en NewRich.Domain
  -> NewRich.Infrastructure
  -> esquema SQL recaudo y procedimientos recaudo.sp_recaudo_*
```

Las lecturas de ventas salen de las tablas actuales, sin modificarlas. La obligación se guarda en `recaudo`.

El enganche con una venta nueva es el último corte de la integración. Primero el módulo lee las ventas que ya están en la base local y calcula. Cuando ese cálculo esté probado, una venta confirmada en local disparará el aumento de la obligación, después de que la venta ya haya quedado guardada. El procedimiento de la venta no se modifica.

## 3. Orden de los cortes

Cada corte termina con pruebas verdes en local y se puede revisar solo. No se adelanta el PDA ni el enganche de la venta.

### Corte 0. Dejar el local listo

1. Confirmar la rama `RecaudoDllo`.
2. Levantar la API y el administrador en Development y entrar a `http://localhost:5274`.
3. Confirmar que la API responde en `http://localhost:5295` y que `@@SERVERNAME` es el equipo local.
4. Anotar un vendedor, un grupo y ventas del día que ya existan en esa base, para usarlos como datos de prueba.

La sección 19 quedó en la opción A: al vendedor sin grupo, ya asignado, se le cobra con su porcentaje individual. Entra a pendientes y a cobrados, y conserva la señal de que debe ingresar a un grupo.

### Corte 1. Reglas de cálculo, sin base de datos

Se escriben primero las pruebas de `NewRich.Domain` y después el código que las cumple.

Reglas de este corte:

- Porcentaje entero del 1 al 100.
- Obligación del día = porcentaje sobre la suma de `Venta.Total` del día.
- Redondeo hacia arriba al peso. 10 % de $1.005 queda en $101.
- El pendiente es un solo saldo: saldo anterior + obligación de hoy − pagos.
- Rechazo de pago en cero, negativo o mayor que el pendiente.
- Varios pagos el mismo día.
- Un reintento idéntico no crea otro pago.
- Colores: por cobrar en verde, deudado en rojo, al día en azul.
- Un grupo y un vendedor solo pueden estar en un recaudador.
- Los miembros de un grupo asignado heredan esa asignación.

Este corte no abre SQL ni la API.

### Corte 2. Esquema en la base local

Script nuevo, solo para la base local, con el esquema `recaudo` y las tablas de configuración, asignaciones, porcentajes, obligaciones, pagos, tirilla de cobro, historial y auditoría.

En la misma base local se amplía `CK_Usuarios_Rol` para admitir Recaudador junto a Administrador, Vendedor, Observador y Super. Antes del cambio se cuenta los usuarios por rol. Después, esas cuentas siguen iguales.

Los procedimientos nuevos se crean como `recaudo.sp_recaudo_*`. Los procedimientos existentes no se alteran.

El script queda versionado en el repositorio. No se ejecuta en otro servidor.

### Corte 3. Primer flujo usable

Este es el inicio de la integración visible.

1. Crear un usuario Recaudador en el administrador local, sin grupo.
2. En `configRecaudo`, asignarle un grupo y un porcentaje.
3. Consultar las ventas locales de ese día y mostrar la obligación de cada vendedor.
4. Mostrar porcentaje, total por recaudar y total recaudado del rango elegido.
5. Mostrar la alarma gris de los grupos que siguen en 0.

Capas de este corte:

- Dominio: las reglas del corte 1.
- Aplicación: casos de uso de configurar y consultar.
- Infraestructura: llamadas a `recaudo.sp_recaudo_asignar_grupo`, `recaudo.sp_recaudo_configurar_porcentaje`, `recaudo.sp_recaudo_consultar_ventas` y `recaudo.sp_recaudo_generar_obligaciones`.
- API: `GET /api/Recaudo/Grupos`, `GET /api/Recaudo/Vendedores`, `GET /api/Recaudo/Obligaciones`.
- Administrador: vista `configRecaudo`, consumiendo la API local.

La prueba de aceptación de este corte: con ventas reales de la base local, el total por recaudar coincide con el porcentaje redondeado hacia arriba.

### Corte 4. Pagos

1. `POST /api/Recaudo/Pagos` registra el pago y actualiza el saldo en una transacción.
2. El primer pago del día mueve al vendedor a cobrados.
3. Un segundo pago del mismo día se aplica al mismo saldo.
4. El historial guarda el recaudador que recibió el dinero.
5. El pago queda inalterable.
6. La tirilla de cobro se guarda con recaudador, vendedor, fecha y hora, valor recibido, saldo que queda y consecutivo. El texto completo de la tirilla se define más adelante.

Prueba local: el ejemplo de $50.000 anteriores + $80.000 de hoy, con un pago de $30.000, deja $100.000.

### Corte 5. Consultas del administrador

Sobre la misma API local:

- `panelRecaudo` con el listado de recaudadores.
- Dos tablas de vendedores: cobrados y pendientes.
- `detalleRecaudo`.
- `historialRecaudo`, con filtros de recaudador, grupo, vendedor, fechas y estado.
- `metricasRecaudo` y los gráficos.

Administrador y Super ven toda la operación. El recaudador no entra a estas pantallas.

### Corte 6. Menú del recaudador en el APK

Se agrega el destino de navegación del rol Recaudador en el APK actual. Las pantallas del vendedor y del observador no cambian.

Incluye las tres listas, filtros, orden por columna, nombre completo y alias, el modal de confirmación, los colores y los gráficos de lo asignado a ese recaudador.

La prueba en el teléfono usa la compilación Debug contra la API local. No sustituye el APK publicado.

### Corte 7. PDA sin señal y sincronización

1. Con conexión, el PDA descarga obligaciones y saldos del recaudador.
2. Sin conexión, el pago se guarda en la base local del teléfono.
3. El sincronizador del menú sube esos pagos al volver la señal.
4. Si durante ese tiempo hubo ventas nuevas, al sincronizar sube la obligación de ese día.

La sincronización de ventas del vendedor sigue su camino actual.

### Corte 8. La obligación nace con la venta

Cuando los cortes anteriores estén probados en local, una venta confirmada en la API local aumentará la obligación del vendedor para ese día.

El aumento ocurre después de que la venta ya está guardada. Si el vendedor no tiene recaudador, o su grupo está sin porcentaje, la venta se conserva y no se genera obligación. Si no tiene grupo, pero está asignado y tiene porcentaje individual, la obligación se calcula con ese porcentaje. El procedimiento y las tablas de ventas no se modifican.

## 4. Qué no entra en estos cortes

- Texto definitivo de la tirilla.
- Cualquier cambio en el menú del vendedor para mostrarle saldos de recaudo.
- Publicación del sitio, de la API o del APK.

## 5. Verificación de cada corte

1. Pruebas de dominio y de aplicación en verde.
2. Si el corte toca SQL, la prueba de integración corre contra la base local y comprueba el nombre del servidor.
3. Si el corte toca el administrador, se prueba en `http://localhost:5274` el flujo completo: entrar, ejecutar la acción y ver el dato reflejado.
4. Si el corte toca el PDA, se instala la compilación Debug en el teléfono conectado y se comprueba contra la API local.

Un corte no se da por terminado con la pantalla abierta y sin ejercer la acción.

## 6. Primer paso al empezar a programar

El trabajo arranca por el corte 0 y el corte 1: comprobar el servidor local y dejar probadas las reglas de saldo, porcentaje, redondeo y colores. Con eso, el corte 3 puede mostrar obligaciones reales de la base local sin tocar todavía el flujo de la venta.
