## Requerimiento: Horario disponible por lotería

### 1. Contexto actual

Actualmente existe en la configuración del administrador una sección llamada **Catálogo de loterías**, donde se administran las loterías disponibles en el sistema.

También existe una configuración de actividad del PDA con los siguientes horarios:

- **Hora de apertura del PDA**
- **Hora de cierre o desconexión del PDA**

Actualmente, cuando se cumple la **Hora de cierre o desconexión del PDA**, el sistema cierra la jornada y no permite al usuario ingresar al PDA. Este funcionamiento debe mantenerse sin cambios.

### 2. Nuevo requerimiento

Cada lotería configurada en el **Catálogo de loterías** debe tener obligatoriamente un **horario disponible**, compuesto por:

- **Hora de inicio**
- **Hora de fin**

Este horario determina durante qué periodo de la jornada la lotería estará disponible para realizar juegos desde el PDA.

### 3. Validación del horario de las loterías

El horario configurado para cada lotería debe estar siempre dentro del horario de funcionamiento del PDA.

Por lo tanto:

- La **hora de inicio de la lotería** no puede ser anterior a la **Hora de apertura del PDA**.
- La **hora de inicio de la lotería** no puede ser posterior a la **Hora de cierre del PDA**.
- La **hora de fin de la lotería** no puede ser anterior a la **Hora de apertura del PDA**.
- La **hora de fin de la lotería** no puede ser posterior a la **Hora de cierre del PDA**.
- La **hora de inicio** debe ser menor que la **hora de fin**.

#### Ejemplo

Si el PDA tiene configurado:

- **Hora de apertura:** 08:00
- **Hora de cierre:** 18:00

Una lotería podría tener:

- Inicio: 09:00
- Fin: 11:00

Pero no sería válido configurar:

- Inicio: 07:00 → ❌ Antes de la apertura del PDA.
- Fin: 19:00 → ❌ Después del cierre del PDA.
- Inicio: 14:00 / Fin: 10:00 → ❌ La hora de inicio es posterior a la hora de fin.

### 4. Configuración obligatoria

Cada vez que se cree una nueva lotería, el sistema debe exigir obligatoriamente:

- **Hora de inicio**
- **Hora de fin**

No debe ser posible guardar una lotería sin estos dos valores.

En caso de editar una lotería existente, también debe validarse que su horario cumpla las reglas establecidas.

### 5. Comportamiento en el PDA

El PDA debe mostrar únicamente las loterías cuyo horario disponible se encuentre vigente en el momento actual.

La validación debe realizarse tomando como referencia la hora actual del sistema.

#### Ejemplo

Horario del PDA:

- **Apertura:** 08:00
- **Cierre:** 18:00

Loterías configuradas:


| Lotería  | Hora inicio | Hora fin |
| -------- | ----------- | -------- |
| Medellín | 09:00       | 11:00    |
| Bogotá   | 09:00       | 14:00    |


**A las 10:00:**

- Medellín → ✅ Disponible
- Bogotá → ✅ Disponible

El PDA muestra:

> Medellín  
> Bogotá

**A las 11:05:**

- Medellín → ❌ No disponible, ya terminó su horario.
- Bogotá → ✅ Disponible.

El PDA muestra:

> Bogotá

**A las 17:00:**

- Medellín → ❌ No disponible.
- Bogotá → ❌ No disponible.

El PDA no muestra ninguna lotería.

### 6. Relación con el horario del PDA

El horario de las loterías **no modifica ni reemplaza** el horario general del PDA.

El funcionamiento debe ser:

**Horario del PDA = ventana general de operación**

Dentro de esa ventana, cada lotería tendrá su propia ventana de disponibilidad.

Por ejemplo:

```

```

```
PDA
08:00 ├──────────────────────────────────┤ 18:00
      │                                  │
      │   Medellín                       │
      │   09:00 ├──────────┤ 11:00       │
      │                                  │
      │   Bogotá                        │
      │   09:00 ├────────────────┤ 14:00 │
      │                                  │
```

Una vez alcanzada la **Hora de cierre del PDA**, se mantiene el comportamiento actual: **se cierra la jornada y el usuario no puede ingresar al sistema**.

### 7. Cambio en el Catálogo de loterías

En la tabla **Catálogo de loterías**, se debe mostrar el horario habilitado de cada lotería.

Ejemplo:


| Lotería  | Horario habilitado | Estado |
| -------- | ------------------ | ------ |
| Medellín | 09:00 - 11:00      | —      |
| Bogotá   | 09:00 - 14:00      | —      |


El horario debe quedar visible directamente al frente de cada lotería para que el administrador pueda identificar fácilmente el periodo en el que estará disponible.

### 8. Regla general de disponibilidad

Una lotería estará disponible en el PDA cuando se cumplan simultáneamente estas condiciones:

```

```

```
Hora actual >= Hora inicio de la lotería
Y
Hora actual <= Hora fin de la lotería
Y
La jornada del PDA se encuentra activa
```

Cuando la hora actual esté fuera del horario configurado de la lotería, esta **no debe mostrarse en el módulo de ventas del PDA**.

**Importante:** el cierre general del PDA continúa funcionando exactamente como lo hace actualmente; este requerimiento únicamente agrega un horario individual de disponibilidad para cada lotería.