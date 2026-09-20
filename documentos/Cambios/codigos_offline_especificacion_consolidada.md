# Códigos Offline – Especificación Funcional Consolidada

## Objetivo

Implementar la lógica de uso offline, sincronización, reposición y actualización del límite de códigos offline en el PDA.

La solución debe considerar que:

- El PDA puede trabajar sin conexión.
- Las ventas realizadas offline pueden sincronizarse posteriormente.
- Los códigos offline gastados deben conservarse como información independiente de las ventas.
- El administrador puede modificar el parámetro **Códigos offline máximos por PDA** en cualquier momento.
- El PDA puede tener un límite desactualizado.
- El servidor es la fuente de verdad del límite vigente y del proceso de reposición.
- La reposición diaria debe ejecutarse como máximo una vez por día por vendedor/PDA.
- Debe existir sincronización automática y sincronización manual mediante el botón **Sincronizar todo**.
- La sincronización de ventas y la reposición de códigos son procesos relacionados, pero independientes.
- No se debe modificar el comportamiento existente fuera de esta funcionalidad.

---

# 1. Conceptos y datos principales

## 1.1 Datos persistentes en el PDA

El PDA debe mantener de forma persistente:

- `codigosOfflineMaximos`: último límite máximo conocido por el PDA.
- `codigosOfflineGastadosPendientes`: cantidad de códigos offline utilizados y pendientes de reposición.

`codigosOfflineGastadosPendientes` debe conservarse ante:

- Cierre de la aplicación.
- Reinicio del PDA.
- Pérdida de conexión.
- Sincronización de las ventas.

## 1.2 Regla del contador de códigos gastados

Cada vez que el vendedor utiliza un código offline:

1. Registrar la venta normalmente.
2. Incrementar `codigosOfflineGastadosPendientes` en 1.
3. Persistir inmediatamente el nuevo valor.

La sincronización de una venta **NO** debe disminuir ni poner en cero este contador.

El contador solamente puede ponerse en `0` después de que el servidor confirme exitosamente la reposición correspondiente.

Si la reposición falla, el contador debe permanecer pendiente.

---

# 2. Sincronización de ventas

Cuando exista conexión con el servidor, el PDA debe poder sincronizar las ventas offline.

El servidor debe validar cada venta:

- Si la venta ya existe, no registrarla nuevamente.
- Si la venta no existe, registrarla.

La sincronización de ventas debe ser independiente del contador `codigosOfflineGastadosPendientes`.

### Ejemplo

Día 1:

```text
Códigos utilizados: 5
codigosOfflineGastadosPendientes = 5
```

Posteriormente, el PDA recupera conexión y sincroniza las 5 ventas:

```text
Ventas pendientes = 0
codigosOfflineGastadosPendientes = 5
```

El contador continúa en `5` porque esos códigos todavía están pendientes de reposición.

---

# 3. Primera conexión diaria

En la primera conexión del PDA con el servidor de cada día, debe ejecutarse el proceso de sincronización/reposición diaria.

El PDA debe enviar al servidor:

```text
codigosOfflineMaximos = último límite conocido por el PDA
codigosOfflineGastadosPendientes = códigos gastados pendientes de reposición
```

El PDA puede tener un valor desactualizado de `codigosOfflineMaximos`.

Por lo tanto:

> El PDA informa el último estado que conoce, pero NO determina el límite vigente.

El servidor debe consultar su propia configuración y determinar el límite actual.

### Importante

La reposición debe poder ejecutarse aunque:

```text
Ventas pendientes = 0
```

siempre que:

```text
codigosOfflineGastadosPendientes > 0
```

y se cumplan las demás condiciones de reposición.

---

# 4. Servidor como fuente de verdad

El servidor debe consultar la configuración actual:

**Códigos offline máximos por PDA**

El servidor debe considerar:

- Último `codigosOfflineMaximos` informado por el PDA.
- `Códigos offline máximos por PDA` configurado actualmente en el servidor.
- `codigosOfflineGastadosPendientes`.
- Estado actual de códigos del vendedor/PDA, utilizando la lógica existente.
- Historial/estado de reposición diaria.
- Configuración de si la reposición diaria está habilitada.

El valor configurado actualmente en el servidor tiene prioridad sobre el valor informado por el PDA.

---

# 5. Cambio del límite por el administrador

El administrador puede modificar:

**Códigos offline máximos por PDA**

El PDA no necesariamente conocerá el cambio hasta su siguiente comunicación con el servidor.

### Ejemplo

Día 1:

```text
Máximo configurado en servidor = 20
Máximo conocido por PDA = 20
Códigos utilizados = 5
codigosOfflineGastadosPendientes = 5
```

Posteriormente, el administrador cambia:

```text
Códigos offline máximos por PDA = 30
```

El PDA todavía conserva:

```text
codigosOfflineMaximos = 20
codigosOfflineGastadosPendientes = 5
```

### Día 2

El PDA envía:

```text
codigosOfflineMaximos = 20
codigosOfflineGastadosPendientes = 5
```

El servidor consulta su configuración y detecta:

```text
Máximo conocido por PDA = 20
Máximo vigente en servidor = 30
Códigos gastados pendientes = 5
```

El servidor debe utilizar `30` como máximo vigente.

El valor `20` solamente representa el último valor conocido por el PDA y no debe utilizarse como configuración actual.

---

# 6. Cálculo de reposición

El servidor debe determinar primero el límite máximo vigente según su configuración actual.

Después debe determinar la cantidad real de códigos que deben reponerse para que el vendedor/PDA quede correctamente nivelado según ese límite.

El cálculo debe considerar:

1. Máximo vigente configurado en el servidor.
2. Estado actual de códigos del vendedor/PDA.
3. Códigos gastados pendientes reportados por el PDA.
4. Cambios realizados por el administrador.
5. Lógica existente de asignación de códigos.

El cálculo **NO debe basarse únicamente en el valor enviado por el PDA**.

### Regla fundamental del cálculo

```text
PDA:
último máximo conocido
+
códigos gastados pendientes

Servidor:
máximo vigente
+
estado real de códigos
+
códigos gastados pendientes
=
cantidad real que debe reponer
```

El servidor debe evitar duplicar códigos y debe garantizar que, después de una reposición exitosa, el estado final corresponda al límite vigente configurado.

---

# 7. Confirmación de reposición

La reposición solamente se considera exitosa cuando el servidor la confirma.

Después de una reposición exitosa, el servidor debe devolver al PDA como mínimo:

```text
codigosOfflineMaximos = máximo vigente
codigosOfflineGastadosPendientes = 0
reposicionExitosa = true
```

El PDA debe actualizar su límite local con el valor vigente informado por el servidor.

### Si la reposición falla

El PDA debe conservar:

```text
codigosOfflineGastadosPendientes
```

sin modificarlo.

Ejemplo:

```text
Antes:
codigosOfflineGastadosPendientes = 5

Reposición:
FALLÓ

Después:
codigosOfflineGastadosPendientes = 5
```

El PDA debe poder intentar nuevamente el proceso posteriormente.

---

# 8. Parametrización del administrador

La funcionalidad de reposición diaria debe quedar parametrizada desde la configuración del administrador.

## Parámetro

**Reasignación diaria de códigos offline**

Valor/configuración:

```text
Sincronización 1 vez
```

La regla de una sola reposición diaria debe ser controlada por el servidor.

La validación de que el vendedor/PDA ya recibió una reposición durante el día debe realizarse en el servidor y no depender únicamente del PDA.

### Reposición habilitada

Si la configuración permite la reposición diaria:

- El servidor puede realizar una reposición como máximo una vez por día.
- Debe validar si el vendedor/PDA ya recibió la reposición correspondiente al día.

Si no ha recibido reposición:

```text
Procesar reposición.
```

Si ya recibió reposición:

```text
No realizar una nueva reposición.
```

### Reposición deshabilitada

Si la funcionalidad está deshabilitada:

```text
Sincronizar ventas normalmente.
No realizar reposición de códigos.
```

---

# 9. Nueva opción "Sincronización" en el PDA

Dentro de:

**Más**

agregar una nueva opción:

**Sincronización**

La vista permitirá consultar el estado y ejecutar el proceso de sincronización.

Debe existir:

### Sincronización automática

Cuando el PDA detecte conexión y se cumplan las condiciones, debe iniciar automáticamente el proceso correspondiente.

### Sincronización manual

La vista debe incluir el botón:

**Sincronizar todo**

Este botón permite ejecutar manualmente el proceso cuando:

- La sincronización automática haya fallado.
- Existan ventas pendientes.
- Existan códigos gastados pendientes de reposición.
- El usuario quiera reintentar la sincronización.
- Sea necesario recuperar un proceso que no se completó correctamente.

El botón **Sincronizar todo** debe reutilizar la misma lógica y servicios utilizados por la sincronización automática. No duplicar lógica.

---

# 10. Vista de sincronización – Perfil Vendedor

Para el perfil **Vendedor**, mostrar como mínimo:

## Ventas pendientes

```text
Ventas por sincronizar: 3
```

## Códigos pendientes de reposición

```text
Códigos pendientes de reposición: 5
```

## Progreso

Mostrar una barra de progreso con porcentaje:

```text
Sincronizando

[████████████████░░░░] 80%
```

El porcentaje debe actualizarse conforme avance el proceso.

## Acción manual

Mostrar:

```text
[ Sincronizar todo ]
```

El botón debe permitir reintentar todo el proceso cuando sea necesario.

---

# 11. Proceso de sincronización de códigos

La reposición de códigos es independiente de las ventas pendientes.

**No debe ser obligatorio que existan ventas pendientes para solicitar la reposición.**

La reposición debe procesarse cuando:

1. `codigosOfflineGastadosPendientes > 0`.
2. La reposición diaria esté habilitada.
3. El vendedor/PDA no haya recibido reposición durante el día.
4. Exista conexión con el servidor.

### Caso válido

```text
Ventas pendientes = 0
Códigos pendientes de reposición = 5
```

El servidor debe poder procesar la reposición.

### Caso de reposición ya realizada

Si el vendedor/PDA ya recibió la reposición del día:

```text
Ventas pendientes = 0
Códigos pendientes = 5
Reposición diaria = ya realizada
```

No debe generarse una segunda reposición ese mismo día.

---

# 12. Flujo automático

```text
VENDEDOR UTILIZA CÓDIGO OFFLINE
              │
              ▼
Registrar venta localmente
              │
              ▼
Incrementar codigosOfflineGastadosPendientes
              │
              ▼
Persistir contador
              │
              ▼
        ¿Existe conexión?
              │
       ┌──────┴──────┐
       │             │
      NO             SÍ
       │             │
       │             ▼
       │      Sincronizar ventas
       │             │
       │             ▼
       │      Mantener contador
       │      de códigos gastados
       │
       ▼
Primera conexión diaria
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
Consulta estado real de códigos
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

El botón **Sincronizar todo** debe ejecutar el mismo proceso de negocio utilizado por la sincronización automática.

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
Servidor valida ventas
              │
              ▼
Consultar códigos gastados pendientes
              │
              ▼
Validar reposición diaria
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
Confirmar operación
              │
              ▼
Si reposición exitosa:
contador de gastados = 0
              │
              ▼
Mostrar resultado final
```

Si la reposición falla, el contador debe permanecer pendiente.

---

# 14. Independencia entre ventas y códigos

La solución debe separar completamente estos dos procesos.

## Sincronización de ventas

Se realiza cuando:

```text
Existen ventas offline pendientes
+
Existe conexión
```

## Reposición de códigos

Se realiza cuando:

```text
Existen códigos gastados pendientes
+
Reposición diaria habilitada
+
No se ha realizado reposición ese día
+
Existe conexión
```

No asumir que ambos estados tienen que coincidir.

### Ejemplos válidos

```text
Ventas pendientes = 5
Códigos pendientes = 5
```

```text
Ventas pendientes = 0
Códigos pendientes = 5
```

```text
Ventas pendientes = 5
Códigos pendientes = 0
```

Cada proceso debe manejarse según su propio estado.

---

# 15. Estados de la vista

La vista de sincronización debe contemplar como mínimo:

| Estado | Descripción |
|---|---|
| Pendiente | Existen datos pendientes |
| Sincronizando | El PDA está enviando información |
| Validando | El servidor está validando las ventas |
| Reponiendo códigos | El servidor está procesando la reposición |
| Actualizando configuración | El PDA está actualizando el límite vigente |
| Completado | El proceso terminó correctamente |
| Error | Se presentó un problema |
| Reposición ya realizada | La reposición diaria ya fue ejecutada |
| Sin conexión | No existe conexión disponible |

---

# 16. UX/UI

La nueva vista **Sincronización** debe utilizar buenas prácticas de UX/UI.

Debe mostrar de forma clara:

- Estado actual.
- Ventas pendientes.
- Códigos gastados pendientes de reposición.
- Porcentaje de progreso.
- Estado de sincronización.
- Estado de reposición.
- Resultado de la última operación.
- Botón **Sincronizar todo**.

La interfaz debe minimizar la intervención del vendedor.

La sincronización automática debe ejecutarse sin requerir acciones adicionales.

El botón manual debe estar disponible como mecanismo de recuperación/reintento.

---

# 17. Reglas de integridad

1. No duplicar ventas existentes.
2. No duplicar reposiciones del mismo día.
3. No reiniciar `codigosOfflineGastadosPendientes` al sincronizar ventas.
4. No reiniciar el contador si la reposición falla.
5. Reiniciar el contador únicamente después de confirmación exitosa del servidor.
6. El servidor debe validar la reposición diaria.
7. El servidor debe ser la fuente de verdad del límite máximo.
8. El PDA puede tener un límite desactualizado.
9. El límite configurado en servidor tiene prioridad.
10. La reposición no depende de que existan ventas pendientes.
11. El botón manual y el proceso automático deben utilizar la misma lógica de negocio.
12. Los datos pendientes deben persistir ante cierre, reinicio o pérdida de conexión.

---

# 18. Reglas de implementación y cambios

Antes de modificar código:

1. Revisar la arquitectura actual del PDA y del servidor.
2. Identificar dónde se almacenan los códigos offline.
3. Identificar dónde se almacenan las ventas offline.
4. Identificar el mecanismo actual de sincronización.
5. Identificar la configuración existente de `Códigos offline máximos por PDA`.
6. Identificar cómo se controla actualmente la asignación de códigos.
7. Identificar cómo se registra la reposición diaria.
8. Identificar los servicios, entidades, repositorios y componentes existentes relacionados con esta funcionalidad.

### Reglas obligatorias

- Reutilizar servicios, entidades, repositorios, componentes y patrones existentes cuando sea posible.
- No duplicar lógica existente.
- No crear mecanismos paralelos si ya existe uno equivalente.
- No modificar funcionalidades que no estén relacionadas con esta mejora.
- Mantener compatibilidad con el comportamiento actual.
- No eliminar información existente sin una razón funcional explícita.
- Mantener persistente `codigosOfflineGastadosPendientes`.
- Todas las validaciones críticas de reposición deben realizarse en el servidor.
- El PDA no debe asumir que su configuración local es la configuración vigente.
- Si existe información insuficiente para realizar un cambio seguro, revisar primero la implementación existente antes de modificarla.

---

# 19. Regla fundamental del sistema

> **El PDA reporta; el servidor decide.**

El PDA informa:

```text
Último máximo conocido
+
Códigos gastados pendientes
```

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

Después de una reposición exitosa:

```text
Servidor → PDA

Nuevo máximo vigente
+
Confirmación de reposición
+
Contador de gastados = 0
```
# 20. Regla reducción del liminte maximo
### Reducción del límite máximo

Si el administrador reduce el parámetro `Códigos offline máximos por PDA`, el servidor debe utilizar siempre el nuevo límite vigente.

La cantidad de códigos gastados pendientes NO implica automáticamente que esa misma cantidad deba reponerse.

El servidor debe calcular la cantidad real necesaria considerando el nuevo límite y la cantidad actual de códigos disponibles.

Si el PDA ya tiene una cantidad de códigos disponibles igual o superior al nuevo límite, la reposición debe ser `0`.

Ejemplo:

Día 1:
- Máximo = 50
- Códigos gastados = 5
- Códigos disponibles = 45

Día 2:
- Nuevo máximo configurado = 40

Resultado:
- Máximo vigente = 40
- Códigos disponibles = 45
- Reposición = 0

No se deben generar códigos adicionales.

La reducción del límite no debe provocar automáticamente una reposición negativa ni generar códigos por encima del nuevo máximo.

El sistema no debe eliminar ni invalidar automáticamente los códigos que ya estén disponibles, salvo que esa funcionalidad sea definida explícitamente como una regla adicional.

La responsabilidad queda claramente separada:

- **PDA:** registrar uso offline, conservar el contador, sincronizar ventas y reportar su último estado conocido.
- **Servidor:** validar ventas, consultar la configuración vigente, determinar la reposición, controlar la regla diaria y confirmar la operación.
