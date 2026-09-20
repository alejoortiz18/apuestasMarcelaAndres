# Códigos Offline – Sincronización, Reposición y Cambio de Límite

## Objetivo

Implementar la lógica de control, sincronización y reposición de códigos offline en el PDA, considerando que:

* El PDA puede trabajar sin conexión.
* Las ventas offline pueden sincronizarse posteriormente.
* Los códigos gastados deben conservarse como información independiente de las ventas.
* El administrador puede modificar el límite máximo de códigos offline.
* El servidor es la fuente de verdad del límite vigente.
* La reposición diaria debe ejecutarse como máximo una vez por día.
* Debe existir sincronización automática y un botón manual para ejecutar el proceso en caso de que la automática falle.

**No modificar el comportamiento existente fuera de esta funcionalidad.**

---

# 1. Información almacenada en el PDA

El PDA debe mantener de forma persistente:

* `codigosOfflineMaximos`: último límite máximo de códigos conocido por el PDA.
* `codigosOfflineGastadosPendientes`: cantidad de códigos offline utilizados y pendientes de reposición.

### Al utilizar un código offline

Cada vez que el vendedor utilice un código offline:

1. Registrar la venta normalmente.
2. Incrementar `codigosOfflineGastadosPendientes` en 1.
3. Mantener el contador aunque la venta sea sincronizada posteriormente.

**Importante:** la sincronización de una venta NO debe poner en cero `codigosOfflineGastadosPendientes`.

El contador solamente se debe poner en `0` después de que el servidor confirme exitosamente la reposición.

---

# 2. Sincronización de ventas

Las ventas offline deben sincronizarse cuando exista conexión con el servidor.

El servidor debe validar cada venta:

* Si la venta ya existe, no registrarla nuevamente.
* Si la venta no existe, registrarla.

La sincronización de ventas es independiente del contador de códigos gastados.

Ejemplo:

```text
Día 1:
Utiliza 5 códigos
Gastados pendientes = 5

Las 5 ventas se sincronizan durante la tarde.

Ventas pendientes = 0
Gastados pendientes = 5
```

El contador de códigos gastados debe continuar siendo `5`.

---

# 3. Primera conexión diaria

En la primera conexión del PDA con el servidor de cada día, el PDA debe enviar:

* `codigosOfflineMaximos`: último límite máximo conocido por el PDA.
* `codigosOfflineGastadosPendientes`: cantidad de códigos utilizados pendientes de reposición.

El PDA puede tener información desactualizada sobre el límite máximo.

Por lo tanto:

> **El PDA no determina el límite vigente. El servidor es la fuente de verdad.**

La primera conexión diaria debe permitir solicitar la reposición aunque no existan ventas pendientes de sincronización.

---

# 4. Servidor como fuente de verdad

El servidor debe consultar la configuración actual:

**Códigos offline máximos por PDA**

Debe comparar:

* Límite máximo conocido por el PDA.
* Límite máximo actualmente configurado en el servidor.
* Códigos offline gastados pendientes.
* Códigos disponibles actualmente, utilizando la lógica existente del sistema.

El valor configurado actualmente en el servidor siempre tiene prioridad sobre el valor enviado por el PDA.

---

# 5. Ejemplo de cambio de límite

### Día 1

Configuración:

```text
Códigos offline máximos por PDA = 20
```

El PDA conoce:

```text
codigosOfflineMaximos = 20
```

El vendedor utiliza 5 códigos:

```text
codigosOfflineGastadosPendientes = 5
```

Posteriormente, el administrador cambia la configuración:

```text
Códigos offline máximos por PDA = 30
```

El PDA todavía no conoce este cambio.

### Día 2

El PDA envía:

```text
codigosOfflineMaximos = 20
codigosOfflineGastadosPendientes = 5
```

El servidor consulta su configuración:

```text
Máximo conocido por PDA = 20
Máximo actual servidor = 30
Gastados pendientes = 5
```

El servidor debe detectar que el límite fue modificado.

El nuevo límite vigente es:

```text
30
```

El valor `20` enviado por el PDA solamente representa el último valor que este conocía.

**No debe utilizarse como límite vigente.**

El servidor debe calcular la cantidad real que falta para que el vendedor/PDA quede correctamente nivelado según el nuevo límite.

---

# 6. Cálculo de reposición

El servidor debe determinar primero el límite máximo vigente consultando su propia configuración.

Después debe determinar la cantidad real de códigos que deben reponerse para que el PDA/vendedor quede correctamente nivelado de acuerdo con el límite actual.

El cálculo debe considerar:

1. Límite vigente configurado en el servidor.
2. Estado actual de códigos del vendedor/PDA.
3. Códigos gastados pendientes reportados por el PDA.
4. Cambios realizados por el administrador.
5. Lógica existente de asignación de códigos.

El cálculo **no debe basarse únicamente en el valor enviado por el PDA**.

### Regla

El PDA informa el último estado que conoce.

El servidor determina el estado real y vigente.

---

# 7. Parametrización del administrador

La funcionalidad de reposición diaria debe quedar parametrizada desde la configuración del administrador.

### Parámetro

**Reasignación diaria de códigos offline**

```text
Sincronización 1 vez
```

La regla de una sola reposición diaria debe ser controlada por el servidor.

La validación de que el vendedor/PDA ya recibió una reposición durante el día debe realizarse en el servidor y no depender únicamente del PDA.

### Comportamiento

Si la configuración está habilitada:

* El servidor puede realizar la reposición una vez por día.
* Debe validar si ya se realizó la reposición durante el día.

Si ya se realizó:

```text
No realizar una nueva reposición.
```

Si no se realizó:

```text
Procesar la reposición correspondiente.
```

Si la configuración está deshabilitada:

```text
Sincronizar ventas normalmente.
No realizar reposición de códigos.
```

---

# 8. Actualización del PDA

Después de que el servidor realice exitosamente la reposición, debe devolver al PDA:

* El nuevo límite máximo vigente.
* Confirmación de reposición.
* Estado actualizado de la operación.

Ejemplo:

```text
Servidor:
Máximo vigente = 30
Reposición calculada = X
Reposición exitosa = Sí
```

El PDA recibe:

```text
codigosOfflineMaximos = 30
codigosOfflineGastadosPendientes = 0
```

De esta forma, el PDA queda actualizado con el nuevo límite.

### Si la reposición falla

El PDA NO debe poner el contador en cero.

Ejemplo:

```text
codigosOfflineGastadosPendientes = 5
```

Si la reposición falla:

```text
codigosOfflineGastadosPendientes = 5
```

La información debe permanecer almacenada para permitir un nuevo intento.

---

# 9. Nueva opción "Sincronización" en el PDA

Dentro de:

**Más**

agregar:

**Sincronización**

Esta vista permitirá al vendedor consultar y ejecutar el proceso de sincronización.

La sincronización debe funcionar de dos formas:

### Automática

Cuando el PDA detecte conexión con el servidor y se cumplan las condiciones, debe iniciar automáticamente el proceso.

### Manual

La vista debe disponer de un botón:

**Sincronizar todo**

Este botón permitirá al vendedor iniciar manualmente el proceso cuando:

* La sincronización automática haya fallado.
* Existan ventas pendientes.
* Existan códigos gastados pendientes de reposición.
* El usuario quiera ejecutar nuevamente el proceso.

El botón manual debe utilizar la misma lógica y servicios de la sincronización automática para evitar duplicar código.

---

# 10. Vista de sincronización – Perfil Vendedor

Para el perfil **Vendedor**, mostrar como mínimo:

### Ventas pendientes

Mostrar:

```text
Ventas por sincronizar: 3
```

### Códigos pendientes de reposición

Mostrar:

```text
Códigos pendientes de reposición: 5
```

### Barra de progreso

Mostrar el porcentaje general del proceso:

```text
Sincronizando

[████████████████░░░░] 80%
```

El porcentaje debe actualizarse conforme avance el proceso.

### Botón manual

Mostrar:

```text
[ Sincronizar todo ]
```

El botón debe estar disponible para permitir un intento manual cuando sea necesario.

---

# 11. Sincronización de códigos offline

La reposición de códigos debe procesarse de forma independiente de las ventas pendientes.

**NO utilizar como condición obligatoria que existan ventas pendientes.**

La reposición debe ejecutarse cuando:

1. Existan `codigosOfflineGastadosPendientes > 0`.
2. La reposición diaria esté habilitada.
3. El vendedor/PDA no haya recibido reposición durante el día.
4. Exista conexión con el servidor.

Por lo tanto, puede ocurrir:

```text
Ventas pendientes = 0
Códigos pendientes de reposición = 5
```

En este caso, el proceso debe continuar con la solicitud de reposición.

### Ejemplo de vista

```text
Sincronización

Ventas pendientes: 3

[████████████████████] 100%
✓ Ventas sincronizadas

Códigos pendientes de reposición: 5

[████████████████████] 100%
✓ Códigos reasignados
```

Si el vendedor ya recibió la reposición correspondiente al día:

```text
✓ Reposición diaria ya realizada
```

No se debe generar una nueva reposición.

---

# 12. Flujo automático

```text
VENDEDOR UTILIZA CÓDIGOS OFFLINE
              │
              ▼
Registrar venta localmente
              │
              ▼
Incrementar códigos gastados pendientes
              │
              ▼
Guardar información persistentemente
              │
              ▼
        ¿Existe conexión?
              │
       ┌──────┴──────┐
       │             │
      NO             SÍ
       │             │
       │             ▼
       │       Iniciar sincronización
       │             │
       │             ▼
       │       Sincronizar ventas
       │             │
       │             ▼
       │       Mantener contador
       │       de códigos gastados
       │             │
       └──────┬──────┘
              │
              ▼
      Primera conexión diaria
              │
              ▼
PDA envía último máximo conocido
+ códigos gastados pendientes
              │
              ▼
Servidor consulta configuración actual
              │
              ▼
Determina máximo vigente
              │
              ▼
Calcula cantidad real a reponer
              │
              ▼
¿Reposición diaria habilitada?
       │             │
      NO             SÍ
       │             │
       │             ▼
       │       ¿Ya repuso hoy?
       │          │        │
       │         SÍ       NO
       │          │        │
       │          │        ▼
       │          │   Reponer códigos
       │          │        │
       │          │        ▼
       │          │   Confirmar operación
       │          │        │
       │          │        ▼
       │          │   Actualizar PDA
       │          │        │
       │          │        ▼
       │          │   Contador = 0
       │
       ▼
Finalizar
```

---

# 13. Flujo manual – "Sincronizar todo"

El botón **Sincronizar todo** debe ejecutar el mismo proceso de sincronización automática.

```text
Usuario pulsa "Sincronizar todo"
              │
              ▼
Verificar conexión
              │
              ▼
Sincronizar ventas pendientes
              │
              ▼
Validar ventas en servidor
              │
              ▼
Consultar códigos gastados pendientes
              │
              ▼
Consultar/validar reposición diaria
              │
              ▼
Servidor consulta límite vigente
              │
              ▼
Calcular reposición
              │
              ▼
Reponer códigos si corresponde
              │
              ▼
Actualizar límite del PDA
              │
              ▼
Confirmar reposición
              │
              ▼
Poner contador de gastados = 0
              │
              ▼
Mostrar resultado final
```

Si la reposición falla, el contador de códigos gastados debe permanecer pendiente.

---

# 14. Independencia de las ventas

La lógica debe separar claramente:

### Sincronización de ventas

Se realiza cuando:

```text
Existen ventas offline pendientes
+
Existe conexión
```

### Reposición de códigos offline

Se realiza cuando:

```text
Existen códigos gastados pendientes
+
La reposición diaria está habilitada
+
No se ha realizado la reposición del día
+
Existe conexión
```

**No asumir que las ventas pendientes y los códigos pendientes de reposición tienen el mismo estado.**

---

# 15. UX/UI

La nueva vista de **Sincronización** debe utilizar buenas prácticas de UX/UI.

Debe mostrar claramente:

* Estado actual de sincronización.
* Ventas pendientes.
* Códigos gastados pendientes de reposición.
* Porcentaje de progreso.
* Estado de la reposición.
* Resultado de la última sincronización.
* Botón `Sincronizar todo`.

Debe minimizar la intervención del vendedor y permitir que el proceso automático funcione sin acciones adicionales.

### Estados principales

| Estado                     | Descripción                                |
| -------------------------- | ------------------------------------------ |
| Pendiente                  | Existen datos pendientes de sincronización |
| Sincronizando              | El PDA está enviando información           |
| Validando                  | El servidor está validando las ventas      |
| Reponiendo códigos         | El servidor está procesando la reposición  |
| Actualizando configuración | El PDA está actualizando el límite vigente |
| Completado                 | Proceso finalizado correctamente           |
| Error                      | Se presentó un problema durante el proceso |
| Reposición ya realizada    | La reposición diaria ya fue ejecutada      |

---

# 16. Regla principal

La lógica debe separar completamente dos conceptos:

### Ventas

Las ventas pueden sincronizarse en cualquier momento en que exista conexión.

### Códigos gastados

Los códigos gastados deben mantenerse como información pendiente de reposición hasta que el servidor confirme exitosamente la reposición.

Por lo tanto:

> **Sincronizar una venta NO significa que el código utilizado ya fue repuesto.**

---

# 17. Regla fundamental del sistema

### PDA

El PDA reporta:

```text
Último máximo conocido
+
Códigos gastados pendientes
```

### Servidor

El servidor determina:

```text
Máximo vigente configurado actualmente
+
Estado real de códigos
+
Códigos gastados pendientes
=
Cantidad real que debe reponer
```

Por lo tanto:

> **El servidor siempre es la fuente de verdad sobre el límite máximo y la cantidad que debe reponerse.**

El PDA solamente informa el último estado que conoce y los códigos que tiene pendientes de reposición.

---

# 18. Consideraciones de implementación

Antes de realizar cambios:

1. Revisar la arquitectura actual del PDA y del servidor.
2. Identificar dónde se almacenan actualmente los códigos offline.
3. Identificar dónde se registran las ventas offline.
4. Identificar el mecanismo actual de sincronización.
5. Identificar la configuración existente de `Códigos offline máximos por PDA`.
6. Identificar cómo se controla actualmente la asignación de códigos.
7. Identificar cómo se puede registrar que un vendedor/PDA ya realizó la reposición del día.
8. Reutilizar servicios, entidades, repositorios y patrones existentes.
9. No duplicar lógica existente.
10. Mantener `codigosOfflineGastadosPendientes` de forma persistente ante cierres, reinicios y pérdida de conexión.
11. No reiniciar el contador hasta recibir confirmación exitosa del servidor.
12. El botón `Sincronizar todo` debe reutilizar el mismo proceso de sincronización automática.
13. No modificar funcionalidades que no estén relacionadas con esta mejora.
14. Validar todas las reglas de reposición en el servidor.
15. El servidor debe tener siempre prioridad sobre cualquier configuración desactualizada del PDA.
