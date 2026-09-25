# Requerimiento funcional — Topes por Lotería

## 1. Objetivo

Permitir que el **Administrador** configure un valor máximo de apuesta acumulada para cada **lotería**.

El tope configurado para una lotería deberá aplicarse **individualmente a cada número jugado dentro de dicha lotería**.

El acumulado se calcula **por día calendario** y suma el valor de las apuestas **directas y combinadas**.

El sistema deberá validar, antes de registrar un juego, cuánto dinero se ha jugado previamente ese día para la combinación:

> **Lotería + Número + Día**

y determinar si el nuevo juego puede realizarse sin superar el tope establecido para esa lotería.

---

# 2. Configuración de Topes

En el sitio web, el Administrador tendrá una sección:

**Configuración → Topes**

En esta sección podrá consultar y administrar el tope de cada lotería.

### Información de la configuración


| Campo   | Descripción                                 |
| ------- | ------------------------------------------- |
| Lotería | Lotería a la que aplica el tope             |
| Tope    | Valor máximo acumulado permitido por número |


**No existirá un campo Número en esta configuración.**

El Administrador configura únicamente:

> **Lotería → Tope**

### Ejemplo


| Lotería  | Tope     |
| -------- | -------- |
| Medellín | $50.000  |
| Bogotá   | $100.000 |
| Cali     | $75.000  |


### 2.1 Tope obligatorio al crear la lotería

Toda lotería **debe tener un tope** desde el momento de su creación.

- Al crear una lotería nueva, el Administrador **debe** indicar el tope.
- No es válido crear una lotería sin tope.
- Para las loterías que **ya existen** en el sistema al momento de implementar esta funcionalidad, se asignará un tope inicial de **$1.000**.

### 2.2 Tope igual a cero

Si el tope de una lotería es **$0**:

- Esa lotería **no podrá utilizarse** para juegos.
- El PDA y el servidor deberán impedir apuestas sobre esa lotería.

### 2.3 CRUD y contraseña

El Administrador podrá realizar **CRUD** sobre los topes (consultar, crear junto con la lotería, modificar y eliminar según las reglas del sistema).

Para **modificar** un tope, el Administrador deberá **confirmar su contraseña**.

Si el Administrador baja el tope por debajo del acumulado ya jugado ese día para algún número, los juegos ya registrados se conservan; los juegos nuevos solo podrán usarse el saldo disponible restante (que puede ser $0).

---

# 3. Aplicación del tope

El tope se aplicará **individualmente a cada número dentro de la lotería**, **por día**.

Por ejemplo, si se configura:

> **Medellín → $50.000**

entonces cada número de Medellín tendrá su propio acumulado independiente **en el día**.


| Lotería  | Número | Día        | Tope    |
| -------- | ------ | ---------- | ------- |
| Medellín | 1234   | 2026-09-25 | $50.000 |
| Medellín | 5678   | 2026-09-25 | $50.000 |
| Medellín | 9012   | 2026-09-25 | $50.000 |
| Medellín | 4321   | 2026-09-25 | $50.000 |


Los valores de diferentes números **no se suman entre sí**.

Por ejemplo:

- Medellín + 1234 → $50.000
- Medellín + 5678 → $50.000

No significa que Medellín haya alcanzado un acumulado de $100.000 para efectos del tope.

Cada número tiene su propio acumulado diario.

### 3.1 Qué suma al acumulado

Al acumulado de **Lotería + Número + Día** se suma el valor jugado en:

- Apuestas **directas** (individuales).
- Apuestas **combinadas**.

Ambas modalidades cuentan contra el mismo tope del número en esa lotería ese día.

### 3.2 Juego sobre varias loterías

Si el vendedor juega el mismo número en **varias loterías** a la vez (por ejemplo Medellín y Bogotá), el sistema deberá **validar el tope de cada lotería por separado**.

El juego solo podrá continuar si **todas** las loterías involucradas cumplen la fórmula del tope para ese número y día.

---

# 4. Validación al realizar un juego

Cuando el vendedor realice un juego desde el PDA, el sistema deberá identificar:

- Lotería (o loterías).
- Número jugado.
- Valor de la apuesta (directa o combinada).
- Día del juego.

### Momento de la validación en el PDA

La validación de topes se ejecuta cuando el vendedor da clic en **Jugar**, **antes** de mostrar o confirmar el paso **Aceptar y pagar**.

Si el valor supera el disponible, el sistema no debe avanzar a aceptar y pagar hasta que el vendedor ajuste el valor o se rechace el juego.

El PDA deberá consultar al servidor (si hay conexión) para obtener el acumulado existente de esa combinación:

> **Lotería + Número + Día**

Posteriormente se deberá calcular:

**Acumulado actual del número en el día + Valor del nuevo juego**

y comparar el resultado contra el tope configurado para la lotería.

### Fórmula

**Acumulado actual del día + nuevo valor ≤ tope de la lotería**

Si se cumple la condición, el juego puede continuar.

Si no se cumple, el juego deberá ser rechazado o el vendedor deberá ajustar el valor al máximo disponible.

---

# 5. Ejemplo — Juego permitido

Configuración:

> **Medellín → $50.000**

### Juego 1

El vendedor juega:

> Medellín — 1234 — $15.000

El servidor consulta:

> Medellín + 1234 + día de hoy

Acumulado actual:

> $0

Validación:

> $0 + $15.000 = $15.000

Como:

> $15.000 ≤ $50.000

El juego es permitido.

Nuevo acumulado del día:

> **$15.000**

---

### Juego 2

El vendedor juega:

> Medellín — 1234 — $35.000

El servidor consulta nuevamente:

> Medellín + 1234 + día de hoy

Acumulado:

> $15.000

Validación:

> $15.000 + $35.000 = $50.000

Como el valor es exactamente igual al tope, el juego es permitido.

Nuevo acumulado del día:

> **$50.000**

---

# 6. Número que alcanza el tope

Posteriormente el vendedor intenta:

> Medellín — 1234 — $2.000

El servidor consulta:

> Medellín + 1234 + día de hoy

Acumulado:

> $50.000

Validación:

> $50.000 + $2.000 = $52.000

Como:

> $52.000 > $50.000

el juego **no podrá realizarse**.

El sistema deberá informar al vendedor que el número ya alcanzó el tope configurado para esa lotería en el día.

### Mensaje

> ⚠️ El número 1234 para la lotería Medellín ha alcanzado el tope permitido de $50.000. No es posible realizar esta apuesta.

---

# 7. Ejemplo — Valor parcialmente disponible

Configuración:

> **Medellín → $50.000**

### Juego 1

> Medellín — 1234 — $35.000

Acumulado del día:

> $35.000

Disponible:

> **$15.000**

### Juego 2

> Medellín — 1234 — $10.000

Acumulado del día:

> $45.000

Disponible:

> **$5.000**

### Juego 3

El vendedor intenta:

> Medellín — 1234 — $10.000

El servidor calcula:

> $45.000 + $10.000 = $55.000

El valor supera el tope.

Sin embargo, el número todavía tiene disponible ese día:

> **$5.000**

Por lo tanto, el sistema deberá informar al vendedor que no puede jugar $10.000, pero que puede reducir el valor hasta un máximo de $5.000.

### Mensaje

> ⚠️ El valor ingresado supera el tope permitido para el número 1234 en la lotería Medellín.
>
> **Valor ingresado:** $10.000  
> **Valor disponible:** $5.000
>
> Reduzca el valor de la apuesta para poder continuar.

---

# 8. El control aplica automáticamente a todos los números

Una de las características principales de esta funcionalidad es que el Administrador **no tendrá que registrar cada número individualmente**.

Si configura:

> **Medellín → $50.000**

el sistema automáticamente aplicará ese tope a cualquier número que se juegue en Medellín ese día.

Por ejemplo:

- 0000
- 0001
- 1234
- 5678
- 9999
- Cualquier otro número válido

Cada uno tendrá su propio acumulado diario y su propio límite de $50.000.

---

# 9. Responsabilidad del servidor

La validación definitiva deberá realizarse en el **servidor**.

El PDA podrá consultar el servidor antes de permitir el juego, pero la validación definitiva deberá ejecutarse nuevamente en el momento de registrar la apuesta.

Esto es necesario porque pueden existir múltiples PDA realizando juegos simultáneamente.

### Ejemplo

Tope:

> Medellín → $50.000

Acumulado de 1234 en el día:

> $40.000

Dos PDA intentan simultáneamente:

- PDA 1 → $10.000
- PDA 2 → $10.000

Si ambos PDA validaran únicamente de forma local, ambos podrían considerar que tienen $10.000 disponibles.

Por esta razón, el servidor deberá controlar la operación de manera que el acumulado **nunca pueda superar los $50.000** en operaciones en línea.

La validación y registro deberán manejarse de forma **atómica/transaccional**.

---

# 10. Funcionamiento del PDA

El PDA deberá descargar y mantener localmente la configuración de los topes establecida por el Administrador (incluido el flag **Permitir juegos offline**, que es una configuración **nueva** del sistema).

Esta información será utilizada para realizar las validaciones correspondientes cuando el dispositivo esté conectado o, si el Administrador permite juegos offline, cuando el dispositivo no tenga conexión.

Para determinar el límite correspondiente a un juego, el PDA deberá identificar:

> **Lotería + Número + Día**

y utilizar el tope configurado para esa lotería.

### Ejemplo de información disponible en el PDA

```
Lotería: Medellín
Número: 1234
Día: 2026-09-25
Tope: $50.000
Acumulado del día: $45.000
Disponible: $5.000
```

Cuando exista conexión, el PDA deberá consultar al servidor para obtener el acumulado actualizado del día **al dar clic en Jugar**, antes de **Aceptar y pagar**.

---

# 11. Funcionamiento Offline

En la configuración del sistema, el Administrador tendrá una opción **nueva**:

> **Permitir juegos offline**

Esta configuración determinará si los vendedores pueden realizar juegos cuando el PDA no tenga conexión con el servidor.

## 11.1 Juegos offline habilitados

Si el Administrador tiene habilitada la opción **Permitir juegos offline**:

- El vendedor podrá ingresar y utilizar el sistema normalmente aunque el PDA no tenga conexión.
- El vendedor podrá realizar juegos.
- El PDA deberá utilizar la información disponible localmente para realizar las validaciones.
- Se deberán respetar los **topes configurados por el Administrador y descargados previamente desde el servidor**.
- Los juegos realizados offline deberán conservarse localmente para posteriormente sincronizarse con el servidor cuando se restablezca la conexión.

> **Importante:** mientras el PDA esté offline, la validación de los topes se realizará utilizando la información disponible localmente en el dispositivo.

## 11.2 Juegos offline deshabilitados

Si el Administrador tiene deshabilitada la opción **Permitir juegos offline**:

- El vendedor podrá ingresar al sistema normalmente.
- El vendedor podrá consultar las funcionalidades que no requieran conexión.
- **No podrá realizar juegos mientras el PDA permanezca offline.**
- El sistema deberá impedir la creación y confirmación de nuevos juegos.
- Una vez se restablezca la conexión con el servidor, el vendedor podrá volver a realizar juegos.

### Mensaje al vendedor

> ⚠️ No es posible realizar juegos sin conexión.
>
> El Administrador ha deshabilitado temporalmente los juegos offline. Verifique la conexión con el servidor para continuar.

## 11.3 Resumen


| Configuración             | PDA online            | PDA offline                                                  |
| ------------------------- | --------------------- | ------------------------------------------------------------ |
| **Offline habilitado**    | Puede realizar juegos | Puede realizar juegos respetando los topes locales           |
| **Offline deshabilitado** | Puede realizar juegos | Puede ingresar al sistema, pero **no puede realizar juegos** |


## 11.4 Sincronización

Cuando el PDA vuelva a tener conexión después de haber realizado juegos offline, deberá iniciar el proceso de sincronización con el servidor.

Los juegos realizados offline **ya fueron jugados y entregados al cliente**. Por esa razón el servidor **debe sincronizarlos y registrarlos**, aun si al momento de la sincronización el acumulado en línea haría que se superara el tope.

Durante la sincronización el servidor deberá:

1. Validar que el juego offline es legítimo (código offline, integridad, no duplicado, etc.).
2. **Registrarlo** porque ya fue vendido.
3. Actualizar el acumulado del día de **Lotería + Número** con el valor sincronizado.

> El tope no debe usarse en la sincronización para rechazar un juego offline ya vendido. El control estricto de no superar el tope aplica a los juegos **en línea** y a la validación local offline **antes** de aceptar la venta en el PDA.

---

# 12. Reglas de negocio

### Regla 1 — Configuración

El Administrador configura:

> **Lotería + Tope**

No configura números individuales. El tope es obligatorio al crear la lotería. Las loterías existentes reciben tope inicial de **$1.000**.

### Regla 2 — Identificación del acumulado

El acumulado se determina mediante:

> **Lotería + Número + Día**

### Regla 3 — Tope independiente por número

Cada número tiene su propio acumulado diario dentro de una lotería.

### Regla 4 — Diferentes números no se acumulan

Por ejemplo:

> Medellín + 1234 → $40.000  
> Medellín + 5678 → $40.000

No se consideran $80.000 para un mismo tope.

Cada número se compara individualmente contra el tope configurado para Medellín.

### Regla 5 — Exactamente el tope

Si:

> **Acumulado del día + nuevo juego = Tope**

el juego **sí está permitido**.

### Regla 6 — Superar el tope

Si:

> **Acumulado del día + nuevo juego > Tope**

el juego **en línea** (o offline local antes de vender) **no está permitido por ese valor**.

### Regla 7 — Saldo parcial

Si todavía existe dinero disponible en el día, el sistema deberá informar al vendedor cuál es el **valor máximo que puede jugar**.

### Regla 8 — Sin disponibilidad

Si:

> **Acumulado del día = Tope**

el número queda sin disponibilidad para nuevos juegos ese día.

### Regla 9 — Directo y combinado

El acumulado del día suma valores de apuestas **directas** y **combinadas**.

### Regla 10 — Varias loterías

Si un juego incluye varias loterías, cada una se valida **por separado** contra su propio tope.

### Regla 11 — Tope en cero

Si el tope de la lotería es **$0**, esa lotería **no puede utilizarse** para juegos.

### Regla 12 — Momento de validación en el PDA

Al dar clic en **Jugar**, el PDA valida topes **antes** de **Aceptar y pagar**.

### Regla 13 — Juegos offline

La posibilidad de realizar juegos sin conexión estará determinada exclusivamente por la configuración **nueva**:

> **Permitir juegos offline**

### Regla 14 — Offline habilitado

Si los juegos offline están habilitados, el PDA podrá realizar juegos sin conexión utilizando la información local disponible y respetando los topes descargados desde el servidor.

### Regla 15 — Offline deshabilitado

Si los juegos offline están deshabilitados, el vendedor podrá ingresar al sistema, pero **no podrá realizar juegos mientras el PDA esté sin conexión**.

### Regla 16 — Validación definitiva en línea

Cuando exista conexión, el servidor deberá realizar la validación definitiva antes de registrar el juego para garantizar que el tope del día no sea superado, incluso cuando existan múltiples PDA operando simultáneamente.

### Regla 17 — Sincronización de juegos ya vendidos

Los juegos realizados offline deberán sincronizarse con el servidor cuando el PDA recupere la conexión.

Como **ya fueron jugados**, el servidor debe **aceptarlos y registrarlos**; no debe rechazarlos por tope en el momento de la sincronización. Sí debe actualizar el acumulado diario correspondiente.
