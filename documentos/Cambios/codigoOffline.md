# Códigos Offline – Reposición y Cambio de Límite

## Objetivo

Implementar la lógica de control, sincronización y reposición de códigos offline en el PDA, considerando que el administrador puede modificar el límite máximo de códigos en cualquier momento.

**No modificar el comportamiento existente fuera de esta funcionalidad.**

---

## 1. Información que debe almacenar el PDA

El PDA debe mantener de forma persistente:

- `codigosOfflineMaximos`: último límite máximo de códigos conocido por el PDA.
- `codigosOfflineGastadosPendientes`: cantidad de códigos offline utilizados y pendientes de reposición.

### Al utilizar un código offline

Cada vez que el vendedor utilice un código offline:

1. Registrar la venta normalmente.
2. Incrementar `codigosOfflineGastadosPendientes` en 1.
3. Mantener el contador aunque la venta sea sincronizada posteriormente.

**Importante:** la sincronización de una venta NO debe poner en cero `codigosOfflineGastadosPendientes`.

El contador solamente se debe poner en `0` después de que el servidor confirme exitosamente la reposición de los códigos.

---

## 2. Primera conexión diaria

En la primera conexión del PDA con el servidor de cada día, el PDA debe enviar:

- `codigosOfflineMaximos`: último límite máximo conocido por el PDA.
- `codigosOfflineGastadosPendientes`: cantidad de códigos utilizados pendientes de reposición.

El PDA puede tener información desactualizada sobre el límite máximo.

Por lo tanto, **el PDA no determina el límite vigente**.

El servidor es la fuente de verdad.

---

## 3. Servidor como fuente de verdad

El servidor debe consultar la configuración actual:

**Códigos offline máximos por PDA**

El servidor debe comparar:

- Límite máximo conocido por el PDA.
- Límite máximo actualmente configurado en el servidor.
- Códigos offline gastados pendientes de reposición.
- Códigos disponibles actualmente, utilizando la lógica existente del sistema.

El valor configurado actualmente en el servidor siempre tiene prioridad sobre el valor enviado por el PDA.

---

## 4. Ejemplo de cambio de límite

### Día 1

Configuración del servidor:

```text
Códigos offline máximos por PDA = 20

```

El PDA conoce:

```text
codigosOfflineMaximos = 20

```

El vendedor utiliza 5 códigos offline:

```text
codigosOfflineGastadosPendientes = 5

```

El PDA queda con:

```text
codigosOfflineMaximos = 20
codigosOfflineGastadosPendientes = 5

```

Posteriormente, el administrador modifica la configuración:

```text
Códigos offline máximos por PDA = 30

```

El PDA todavía no conoce este cambio.

---

## 5. Conexión del día 2

El PDA envía al servidor:

```text
codigosOfflineMaximos = 20
codigosOfflineGastadosPendientes = 5

```

El servidor consulta su configuración actual y encuentra:

```text
Máximo conocido por PDA = 20
Máximo configurado actualmente en servidor = 30
Códigos gastados pendientes = 5

```

El servidor debe detectar que el límite fue modificado.

El nuevo límite vigente es:

```text
30

```

El valor `20` enviado por el PDA solamente representa el último valor que este conocía.

**No debe utilizarse como límite vigente.**

---

## 6. Cálculo de reposición

El servidor debe determinar primero el límite máximo vigente consultando su propia configuración.

Después debe determinar la cantidad real de códigos que deben reponerse para que el PDA/vendedor quede correctamente nivelado de acuerdo con el nuevo límite.

El cálculo debe considerar:

1. El límite vigente configurado en el servidor.
2. El estado actual de códigos del vendedor/PDA.
3. Los códigos gastados pendientes reportados por el PDA.
4. La lógica existente de asignación de códigos.

El cálculo **no debe basarse únicamente en el valor** `codigosOfflineMaximos` **enviado por el PDA**.

### Regla

El PDA informa el último estado que conoce.

El servidor determina el estado real y vigente.

---

## 7. Actualización del PDA

Después de que el servidor realice exitosamente la reposición, debe devolver al PDA el límite máximo vigente.

Ejemplo:

```text
Servidor:
Máximo vigente = 30
Reposición calculada = X

```

Respuesta al PDA:

```text
codigosOfflineMaximos = 30
codigosOfflineGastadosPendientes = 0

```

De esta forma, el PDA queda actualizado con el nuevo límite y preparado para continuar registrando códigos offline.

---

## 8. Independencia de la sincronización de ventas

La reposición de códigos offline debe ser completamente independiente de las ventas pendientes de sincronización.

Puede ocurrir el siguiente escenario:

### Día 1

El vendedor:

```text
Utiliza 5 códigos offline

```

El PDA registra:

```text
codigosOfflineGastadosPendientes = 5

```

Posteriormente, durante el mismo día, el PDA recupera conexión y sincroniza las 5 ventas.

Las ventas quedan correctamente registradas en el servidor.

Sin embargo:

```text
codigosOfflineGastadosPendientes = 5

```

debe permanecer sin cambios.

### Día 2

El PDA se conecta nuevamente.

Aunque:

```text
Ventas pendientes = 0

```

el PDA debe enviar:

```text
codigosOfflineGastadosPendientes = 5

```

El servidor debe procesar la reposición correspondiente.

**La reposición nunca debe depender de que existan ventas pendientes de sincronización.**

---

## 9. Reposición máxima una vez por día

La reposición de códigos debe ejecutarse como máximo una vez por día por vendedor/PDA.

La validación debe realizarse en el servidor.

Debe existir una configuración administrativa para controlar esta funcionalidad:

```text
Reasignación diaria de códigos offline
    - Habilitada
    - Deshabilitada

```

### Si está habilitada

El servidor debe verificar si el vendedor/PDA ya recibió una reposición durante el día.

- Si no recibió reposición: procesarla.
- Si ya recibió reposición: no realizar una nueva reposición.

### Si está deshabilitada

Sincronizar las ventas normalmente, pero no realizar reposición de códigos.

---

## 10. Confirmación de reposición

El PDA solamente debe poner:

```text
codigosOfflineGastadosPendientes = 0

```

cuando el servidor confirme exitosamente la reposición.

### Si la reposición falla

El contador debe conservar su valor.

Ejemplo:

```text
codigosOfflineGastadosPendientes = 5

```

Si el servidor no confirma correctamente la reposición:

```text
codigosOfflineGastadosPendientes = 5

```

El PDA debe conservar esta información para poder reintentar posteriormente.

---

## 11. Flujo general

```text
Vendedor utiliza código offline
            │
            ▼
Incrementar códigos gastados pendientes
            │
            ▼
Guardar contador en PDA
            │
            ▼
¿Existe conexión?
       │             │
      NO            SÍ
       │             │
       │             ▼
       │      Sincronizar ventas
       │             │
       │             ▼
       │      Mantener contador
       │      de códigos gastados
       │
       ▼
Primera conexión del siguiente día
            │
            ▼
PDA envía:
- Último máximo conocido
- Códigos gastados pendientes
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
¿Reposición diaria permitida?
       │             │
      NO            SÍ
       │             │
       │             ▼
       │      ¿Ya repuso hoy?
       │          │       │
       │         SÍ      NO
       │          │       │
       │          │       ▼
       │          │   Reponer códigos
       │          │       │
       │          │       ▼
       │          │   Confirmar
       │          │       │
       │          │       ▼
       │          │   PDA pone contador = 0
       │
       ▼
Finalizar

```

---

## 12. Regla fundamental

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

> **El servidor siempre es la fuente de verdad sobre el límite máximo de códigos offline y sobre la cantidad que debe reponerse.**

El PDA únicamente informa el último estado que conoce y los códigos que tiene pendientes de reposición.

---

## 13. Consideraciones de implementación

Antes de realizar cambios:

1. Revisar la arquitectura actual del PDA y del servidor.
2. Identificar dónde se almacenan actualmente los códigos offline.
3. Identificar dónde se registran las ventas offline.
4. Identificar el mecanismo actual de sincronización.
5. Identificar la configuración existente de `Códigos offline máximos por PDA`.
6. Reutilizar servicios, entidades, repositorios y patrones existentes.
7. No duplicar lógica existente.
8. No modificar funcionalidades que no estén relacionadas con esta mejora.
9. Mantener la información de códigos gastados de forma persistente para evitar perderla ante cierres, reinicios o falta de conexión.
10. Garantizar que el contador solamente se reinicie después de una confirmación exitosa del servidor.

