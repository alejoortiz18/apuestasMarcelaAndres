# Pitch: Ver el resultado operativo del periodo

**Método:** [Shape Up](https://basecamp.com/shapeup), Ryan Singer / Basecamp  
**Estado:** Opción para la mesa de apuestas. Todavía no está apostada.  
**Origen:** Refinamiento de `especificacion_utilidades_kpi.md`  
**Perfiles:** Administrador y Vendedor Web

Este texto es un pitch: problema, apetito, solución, agujeros de conejo y exclusiones. Fija el resultado y las reglas del juego. Deja el diseño visual, las tareas y la implementación al equipo que reciba la apuesta.

La especificación de origen pide, en un solo paquete, un panel gerencial, el registro de gastos, un catálogo, la consolidación de todas las ventas, gráficos, alertas, exportación, auditoría general y el control de Por cubrir y Cubierto, además de reordenar el menú del vendedor. En Shape Up eso es una bolsa de asuntos (*grab-bag*): no tiene un solo caso de uso, no cabe en un ciclo y esconde incógnitas que pueden multiplicar el tiempo. Aquí se estrecha a una apuesta con un pago concreto.

---

## 1. Problema

El administrador quiere saber, de un periodo ya cerrado, cuánto entró por ventas, cuánto se gastó en la operación y qué quedó. Hoy las ventas viven en los reportes que el sistema ya tiene, repartidas entre el esquema `dbo` y el esquema manual. Los gastos del día —transporte, alimentación, entrega de un premio, mensajería— no tienen un lugar en el sistema: quedan en un cuaderno, un chat o la memoria de quien los hizo. El resultado del periodo se arma a mano y cambia según quién sume.

El vendedor web que pagó un desplazamiento o una entrega no puede dejar ese gasto registrado a su nombre. Cuando el administrador revisa el mes, el ingreso que ve en ventas no tiene al lado los gastos que ya ocurrieron. La cifra que llama “ganancia” no se puede rastrear ni repetir.

**Línea de base.** Sin esta solución, responder “¿cómo cerró el mes?” exige abrir los reportes de ventas, pedir los gastos por fuera y restar en una hoja. Con la solución, la misma persona elige el mes en Utilidades y ve tres cifras del mismo periodo: ingresos, gastos y resultado operativo, y puede abrir el gasto que las explica.

Ese resultado es operativo. Es la resta de los ingresos de ventas que el sistema ya reconoce y de los gastos registrados en este módulo. No es el saldo de caja ni la utilidad contable: devoluciones, premios, descuentos y comisiones siguen rigiéndose por sus módulos actuales y no entran en esta resta.

---

## 2. Apetito

**Seis semanas. Un equipo. Un proyecto.**

En Shape Up el apetito es el tiempo que el problema merece, y el diseño se ajusta a ese tiempo. Seis semanas alcanzan para dejar usable el registro de gastos, el catálogo mínimo y un resumen con las tres cifras. No alcanzan para rehacer la contabilidad de las ventas ni para administrar Por cubrir y Cubierto.

| | |
| --- | --- |
| Tamaño | Lote grande: el equipo dedica el ciclo entero a esta apuesta |
| Equipo | Una persona de diseño y una o dos de programación, con prueba integrada hacia el final del ciclo |
| Tiempo fijo | Seis semanas, sin interrupciones de otros pedidos |
| Alcance variable | Lo que no quepa se recorta. El recorte baja alcance, no la calidad de lo que sí se entrega |
| Cortacircuitos | Si no queda usable al terminar las seis semanas, no se extiende solo. Se vuelve a dar forma y se presenta de nuevo |
| Pago de la apuesta | Un administrador y un vendedor web pueden registrar gastos y leer el resultado operativo de un periodo dentro del sistema |

Dos semanas solo para “anotar gastos”, sin el ingreso al lado, no pagan el problema: seguiría faltando la resta. Más de seis semanas para “el módulo financiero completo” convierte la apuesta en un proyecto abierto. Por eso el ingreso de esta versión se lee de una fuente que ya existe, y el resto de la especificación original queda fuera.

La apuesta incluye tiempo sin interrupciones. Un pedido nuevo que aparezca durante el ciclo espera al siguiente, salvo una crisis real (datos que se pierden o una operación de venta que deja de funcionar).

---

## 3. Solución

La solución está en bruto a propósito: lugares, acciones y conexiones, sin wireframes. El diseño visual, los textos de interfaz y el acomodo en pantalla los resuelve quien diseñe, dentro de estas piezas.

### Elementos

1. Un ítem de menú **Utilidades** para Administrador y Vendedor Web. El menú de Ventas que ya existe se queda donde está.
2. Tres cifras del mismo periodo: **Ingresos**, **Gastos** y **Ganancia neta operativa** (ingresos − gastos activos).
3. Un filtro de periodo que mueve las tres cifras juntas.
4. Un formulario para registrar un gasto, con el usuario tomado de la sesión.
5. Un listado de gastos que explica la cifra de gastos y permite anular.
6. Un catálogo de tipos de gasto que solo administra el Administrador.
7. Los datos nuevos viven en el esquema `utilidades`. Las ventas se leen; no se copian ni se modifican.

### Lugares

```text
MENÚ  (Administrador y Vendedor Web)
  Utilidades
    Resumen
    Registrar gasto
    Movimientos
    Configuración de gastos     solo Administrador


RESUMEN
  Periodo
    hoy, ayer, últimos 7 días, últimos 30 días,
    mes actual, mes anterior, rango desde/hasta
  Ingresos del periodo
  Gastos del periodo
  Ganancia neta operativa
  Texto de ayuda: resultado operativo del periodo.
    No es saldo de caja ni utilidad contable.
  Periodo aplicado y momento de la última lectura
  Ver movimientos          →  MOVIMIENTOS
  Registrar un gasto       →  REGISTRAR GASTO


REGISTRAR GASTO
  Tipo de gasto            lista de tipos activos
  Fecha                    por defecto, hoy
  Valor                    pesos, mayor que cero
  Observación              obligatoria si el tipo lo exige
  Guardar                  →  confirmación (tipo, valor, fecha)
                           →  RESUMEN, ya con el gasto en la cifra
  Cancelar                 →  RESUMEN


MOVIMIENTOS
  Mismos límites de periodo que el resumen
  Usuario, tipo y estado (activo / anulado)
  Filas: fecha, tipo, observación, valor, quién registró, estado
  Total del conjunto filtrado, no solo de la página visible
  Anular                   solo Administrador
                           pide motivo
                           la fila sigue visible, marcada anulada,
                           y deja de sumar en Gastos y en el resultado


CONFIGURACIÓN DE GASTOS     solo Administrador
  Nombre, descripción, activo o inactivo,
  si la observación es obligatoria
  Crear, editar nombre y descripción, activar, desactivar
  Cuántos gastos usan ese tipo
  Desactivar: no aparece en registros nuevos;
              los gastos viejos siguen mostrando ese nombre
```

### Cómo se juega el caso

1. El vendedor web abre **Registrar gasto**, elige Transporte, la fecha de ayer y el valor, escribe la observación y guarda. El sistema guarda el gasto a su nombre. No hay un campo para elegir otro usuario.
2. El administrador abre **Resumen**, elige el mes anterior y ve ingresos, gastos y la resta. El transporte de ayer está dentro de gastos si cae en ese mes.
3. Desde el resumen pasa a **Movimientos**, filtra por ese vendedor y ve la fila. Si el gasto estaba mal, lo anula con un motivo y pide que se registre otro. El anulado sigue en la lista y no entra en la resta.
4. Si un tipo deja de usarse, el administrador lo desactiva. El histórico no pierde el nombre.

### Definición de las tres cifras

Una sola regla, usada por el resumen, el listado y cualquier total en pantalla.

- **Ingresos** = el total de ventas válidas del periodo que ya produce la fuente nombrada en la sección 4. Se lee. No se vuelve a calcular con otra regla.
- **Gastos** = suma de los gastos no anulados del periodo.
- **Ganancia neta operativa** = ingresos − gastos.
- Si los ingresos todavía no tienen fuente nombrada, el resumen no inventa un cero que parezca un cierre real: muestra los gastos y deja los ingresos en blanco con la leyenda de que falta la fuente. Esa situación es la puerta de la apuesta (sección 4), no un modo de operación.
- Los importes se guardan como decimal de moneda. La fecha del periodo usa la misma zona horaria con la que la aplicación ya agrupa las ventas.
- Un gasto anulado se conserva, se distingue en la lista y no entra en Gastos ni en la resta.

### Permisos

El servidor rechaza lo que el rol no puede hacer. Ocultar el menú no basta.

| Acción | Vendedor Web | Administrador |
| --- | --- | --- |
| Ver el resumen global del periodo | Sí | Sí |
| Registrar un gasto a su nombre | Sí | Sí |
| Ver los gastos de todos | Sí | Sí |
| Anular un gasto, con motivo | No | Sí |
| Crear, editar, activar o desactivar tipos | No | Sí |
| Borrar un tipo que ya tiene gastos | No | No |

No hay edición del valor, la fecha ni el tipo. Corregir es anular y registrar de nuevo. Así queda un rastro sin abrir un editor de historia.

### Aislamiento

El módulo lee las ventas donde ya están y escribe solo lo suyo.

- Las tablas nuevas (`Gastos`, `TiposGasto` y el rastro de anulaciones) se crean en el esquema `utilidades`.
- No se crean tablas de este módulo en `dbo` ni en los esquemas de negocio ya existentes.
- No se mueven, renombran, alteran ni copian las tablas de ventas.
- Los controladores nuevos llevan `Utilidades` en el nombre (`GastosUtilidadesController`, `ResumenUtilidadesController`, `TiposGastoUtilidadesController` o el equivalente que ya use el API) y cuelgan de una ruta propia `/api/utilidades/`.
- La autorización, el manejo de errores y el estilo de respuesta se reutilizan. La lógica de este módulo no se mete en los controladores de Ventas.

### Tipos iniciales

El catálogo nace con tipos que el administrador puede cambiar después: transporte, alimentación, entrega de premio, servicios, mensajería, materiales, mantenimiento, comisiones y gastos bancarios, y otros. Son semilla, no una lista cerrada. “Comisiones y gastos bancarios” aquí es un tipo de gasto anotado a mano. No lee los registros de Por cubrir ni de Cubierto.

### Qué puede recortarse (~)

Si el ciclo aprieta, estas piezas se caen sin romper el pago de la apuesta. Van marcadas como en Shape Up, para distinguir lo imprescindible de lo deseable.

- ~ Porcentaje de margen.
- ~ Cantidad de gastos, gasto promedio y cantidad de usuarios que registraron.
- ~ Filtro por usuario o por tipo dentro del resumen. El listado sí los tiene.
- ~ Gráfico simple de las tres cifras a lo largo del periodo.
- ~ Comparación con el periodo anterior.
- ~ Exportar el listado.

El equipo las anota en el alcance donde aparezcan y las corta cuando haga falta llegar a tiempo. No se diseñan primero.

---

## 4. Agujeros de conejo

Estos puntos, si se dejan abiertos, se comen el ciclo. Quedan decididos aquí.

### La fuente de ingresos se nombra antes de apostar

Consolidar “todas las tablas de ventas” de `dbo` y del esquema manual, con sus cabeceras, detalles, anulaciones y posibles duplicados, es un iceberg: mucha incógnita debajo de una cifra en pantalla. No cabe en esta apuesta y no se descubre durante las seis semanas.

**Parche.** Antes de la mesa de apuestas, alguien escribe abajo el nombre del reporte, consulta o servicio que hoy ya se considera el total de ventas del periodo. Utilidades lee ese total para el mismo rango de fechas y lo muestra como Ingresos. No recorre tablas por su cuenta, no suma cabecera y detalle, y no inventa columnas.

Si esa fuente ya resta anulaciones o devoluciones, el resumen hereda esa regla. Si no separa `dbo` y el esquema manual, el resumen tampoco lo separa: muestra un solo ingreso.

**Esta apuesta no está lista para la mesa mientras el recuadro siguiente siga vacío.**

> **Fuente de ingresos acordada**  
> Nombre del reporte, consulta o servicio:  
> Qué fecha usa (venta, creación o liquidación):  
> Qué estados deja por fuera:  
> Quién confirmó que ese total es el que el negocio ya usa:

### No se atribuye la venta a un usuario

El resultado es del sistema en el periodo, no de cada vendedor. Los gastos sí muestran quién los registró. El ingreso no se reparte por persona salvo que la fuente nombrada ya traiga ese corte, y aun así esta apuesta no lo muestra. Repartir ventas por usuario es otra apuesta.

### Descuentos, premios y comisiones no entran en la resta

Por cubrir y Cubierto significan, según el caso, algo por pagar, algo ya pagado o un dato que ya está dentro de la venta. Sumarlos aquí duplicaría importes. Esta apuesta no los modifica ni los clasifica. Siguen en sus pantallas actuales.

### El gasto queda activo al guardar

No hay cola de aprobación. Un gasto guardado entra en la cifra. Sacarlo es anularlo.

### El vendedor no corrige gastos

Puede registrar y consultar. Anular, cualquier gasto y de cualquier persona, es del Administrador, con motivo. Así se evita una política a medias (“a veces el vendedor edita los suyos”) que el equipo tendría que inventar a mitad del ciclo.

### La anulación deja rastro mínimo

Al anular se guardan el motivo, quién anuló y cuándo, y el gasto deja de sumar. No se construye una plataforma de auditoría para todo el sistema. El histórico de un tipo de gasto es el propio registro: el nombre se conserva aunque el tipo se desactive.

### El menú de Ventas no se reorganiza

Mover todas las opciones actuales del vendedor bajo un grupo Ventas parece un cambio de rótulos y puede romper rutas y permisos. Esta apuesta solo agrega Utilidades. El menú existente sigue igual.

### El rendimiento se resuelve con la consulta del periodo

El resumen agrega en la base el periodo pedido. No se crea una tabla de totales precalculados en esta apuesta. Si una consulta concreta no responde a tiempo, el equipo ajusta esa consulta. Un almacén de resumen aparte sería otro proyecto, porque exige su propia conciliación.

---

## 5. No vamos a hacer

Queda fuera de esta apuesta, para que el problema siga siendo tratable:

- Administrar, crear, editar o cambiar de estado los registros de **Por cubrir** y **Cubierto**.
- Volver a definir qué es una venta válida, una anulación, una devolución o un premio.
- Copiar las ventas al esquema `utilidades`.
- Mostrar utilidad por usuario, por origen (`dbo` frente al esquema manual) o por tipo de venta.
- Alertas de gasto atípico, concentración o periodo negativo.
- Adjuntos o soportes del gasto.
- Flujo de aprobación o estados distintos de activo y anulado.
- Borrado físico de gastos o de tipos ya usados.
- Exportación a Excel o CSV, salvo que se rescate como ~ y sobre tiempo.
- Gráficos, comparaciones y KPI secundarios, salvo los marcados con ~ si caben.
- Reagrupar el menú actual del Vendedor Web bajo Ventas.
- Presentar la ganancia neta operativa como cierre contable o como saldo de caja.
- Cambiar el comportamiento de las pantallas y tablas de ventas que ya existen.

Esas ideas pueden volver más adelante, cada una con su propio pitch, si siguen importando. No forman una lista de trabajo de este ciclo.

---

## 6. Cómo se ve terminado

Al final de las seis semanas, comparado con la línea de base:

- El Administrador y el Vendedor Web entran a Utilidades desde el sistema, no desde un prototipo aparte.
- Un gasto registrado por el vendedor aparece en Movimientos a su nombre y dentro de Gastos del periodo que le corresponde.
- El resumen del mes muestra ingresos (de la fuente nombrada), gastos activos y la resta, con el texto de que es resultado operativo.
- Anular un gasto lo deja visible y lo saca de la resta, con motivo y autor.
- El Administrador crea un tipo, lo usa en un gasto y luego lo desactiva: el gasto histórico conserva el nombre y el tipo ya no sale al registrar.
- Un vendedor que llame al API no anula gastos ni cambia tipos.
- Las tablas de ventas siguen iguales. Lo nuevo está en el esquema `utilidades`.

Eso es el pago. El pulido visual que no cambie este recorrido puede esperar.

El equipo descubre los alcances al construir, no recibe una lista de fases. Un corte natural, para tener una rebanada integrada pronto, es este orden de aprendizaje:

1. Registrar un gasto y verlo en el total de gastos y en el listado.
2. Leer el ingreso de la fuente ya nombrada para el mismo periodo.
3. Juntar las tres cifras en el resumen.
4. Tipos de gasto y anulación, que cuelgan de lo anterior.

Esos nombres sirven para hablar del avance. No son tareas asignadas por persona ni un plan cerrado. Si un alcance se hincha, se parte. Lo que no se pueda terminar en pocos días no es un alcance: es otro proyecto metido adentro.

---

## 7. Preguntas para la mesa de apuestas

Shape Up pide decidir con estas preguntas, no acumular el pitch en una bolsa de pendientes.

1. **¿El problema importa ahora?** ¿Cerrar el mes con ingresos y gastos en un solo lugar vale más que otras opciones de este ciclo?
2. **¿El apetito es el correcto?** ¿Seis semanas de un equipo alcanzan, sabiendo que Por cubrir, los gráficos y la consolidación nueva de ventas quedan fuera?
3. **¿La solución atrae?** ¿Las tres cifras, el registro y la anulación resuelven la historia de la sección 1?
4. **¿Es el momento?** ¿La fuente de ingresos del recuadro de la sección 4 ya tiene nombre? Si el recuadro sigue vacío, la apuesta se devuelve a shaping.
5. **¿Están las personas?** ¿Hay diseño y programación libres las seis semanas, sin compartirlos con otro lote grande?

Si la mesa apuesta, el equipo recibe este pitch en el arranque y es responsable del resultado. Hecho significa usable en el sistema al cabo de las seis semanas. Si no llega, el cortacircuitos manda: se detiene, se vuelve a dar forma y solo regresa si alguien lo trae de nuevo a la mesa.
