# Requerimiento: Confirmación de contraseña para acciones administrativas

## 1. Objetivo

Implementar un mecanismo de seguridad que solicite la **contraseña del administrador que tiene la sesión abierta** antes de ejecutar determinadas acciones administrativas que crean, modifican, eliminan o afectan información del sistema.

La contraseña debe solicitarse **antes de ejecutar la acción**.

---

# 2. Comportamiento general

Para todas las acciones definidas como protegidas:

1. El administrador presiona el botón o ejecuta la acción.
2. El sistema **no ejecuta todavía la acción**.
3. Se muestra una ventana solicitando la contraseña del administrador.
4. El administrador ingresa la contraseña.
5. El sistema valida la contraseña **en el backend**.
6. Si la contraseña es correcta:
  - Se cierra la ventana.
  - Se continúa con la acción solicitada.
7. Si la contraseña es incorrecta:
  - No se ejecuta la acción.
  - No se modifican datos.
  - Se muestra un mensaje indicando que la contraseña es incorrecta.
  - El administrador puede volver a intentarlo.
8. Si el administrador cancela:
  - Se cierra la ventana.
  - No se ejecuta la acción.
  - No se modifica ningún dato.

### Momento de la solicitud

La contraseña se pide al **persistir** (guardar, eliminar, asociar, bloquear, etc.), no al abrir un formulario ni al pulsar un enlace que solo navega.

Ejemplos que **no** piden contraseña: `Registrar resultado` (abre el formulario), `Editar`, `Crear grupo`, `Nuevo registro`, `Agregar lotería`.

Ejemplos que **sí** piden contraseña: `Guardar`, `Guardar resultado`, `Eliminar usuario`.

Cada ejecución pide contraseña otra vez, aunque sea la misma acción repetida.

### Lotes

En PDA se puede seleccionar más de un dispositivo. Un clic (por ejemplo `Bloquear`) pide la contraseña **una vez** y aplica a todo el lote.

### Eliminaciones con aviso de irreversible

Orden:

1. Pedir contraseña.
2. Mostrar el aviso de que la eliminación es irreversible.
3. Si el administrador confirma, ejecutar la eliminación.

La confirmación de contraseña queda asociada a esa eliminación concreta y no autoriza otra acción.

---

# 3. Seguridad

La validación de la contraseña debe realizarse contra el **backend/servidor**.

No se debe confiar únicamente en una validación realizada mediante JavaScript o en el frontend.

El backend debe validar nuevamente la autorización antes de ejecutar la operación.

La contraseña validada es la del usuario administrador autenticado, no una clave maestra distinta.

### Regla

> Una acción protegida nunca debe ejecutarse si la validación de contraseña no fue exitosa.

---

# 4. Acciones protegidas

Revisión por módulo. Se protege toda acción administrativa de **crear, actualizar o eliminar** datos. Abrir pantallas de consulta o de formulario no pide contraseña.

## ITEM: RESUMEN

### Registrar resultado

- Control: enlace `Registrar resultado`
- Requiere contraseña: **No** (solo abre el formulario de Resultados)
- La contraseña se pide al `Guardar resultado`

---

# ITEM: USUARIO

## Crear usuario → Guardar

- Botón: `Guardar`
- Requiere contraseña: **Sí**

## Editar usuario → Guardar

- Botón: `Guardar`
- Requiere contraseña: **Sí**

## Restablecer contraseña

- Acción: `Restablecer contraseña`
- Requiere contraseña: **Sí**

## Desbloquear usuario

- Acción: `Desbloquear usuario`
- Requiere contraseña: **Sí**
- Agregada en revisión: actualiza el bloqueo del usuario.

## Cambiar grupo

- Acción: `Cambiar grupo`
- Requiere contraseña: **Sí**
- Agregada en revisión: actualiza la asignación del vendedor.

## Eliminar usuario

- Acción: `Eliminar usuario`
- Requiere contraseña: **Sí** (antes del aviso irreversible)

---

# ITEM: GRUPOS

## Editar grupo / Crear grupo

- Controles de navegación: `Editar`, `Crear grupo`
- Requiere contraseña: **No** (abren el formulario)

## Guardar grupo / Guardar nuevo grupo

- Botón: `Guardar`
- Requiere contraseña: **Sí**

## Agregar vendedor al grupo

Ruta: `Ver grupo → Agregar vendedor`

- Acción: `Agregar vendedor`
- Requiere contraseña: **Sí**

## Quitar vendedor del grupo

Ruta: `Ver grupo → Quitar del grupo`

- Acción: `Quitar del grupo`
- Requiere contraseña: **Sí**

## Eliminar grupo

- Acción: `Eliminar grupo`
- Requiere contraseña: **Sí** (antes del aviso irreversible)

---

# ITEM: PDA

| Acción                    | Requiere contraseña | Notas |
| ------------------------- | ------------------- | ----- |
| Asociar PDA               | Sí                  | Una vez por lote |
| Desasociar PDA            | Sí                  | Una vez por lote |
| Bloquear PDA              | Sí                  | Una vez por lote |
| Desbloquear PDA           | Sí                  | Una vez por lote |
| Eliminar PDA              | Sí                  | Antes del aviso irreversible; una vez por lote |
| Registrar PDA → Continuar | Sí                  | Antes de continuar el registro |
| Registrar PDA → Reintentar | Sí                  | Misma persistencia si falla la instalación |

## Registrar PDA

Flujo:

```text
Registrar PDA
      ↓
Continuar
      ↓
Solicitar contraseña
      ↓
Validar contraseña
      ↓
¿Contraseña correcta?
   ├── No → Mostrar error
   │
   └── Sí → Continuar registro

```

La contraseña debe solicitarse **antes de continuar con el registro del PDA**.

`Continuar` comprueba el USB y luego persiste el dispositivo. La comprobación (`Verificar`) no pide contraseña; `Continuar` y `Reintentar` sí, porque persisten el registro.

---

# ITEM: RESULTADOS

## Registrar resultado

- Enlace: `Registrar resultado`
- Requiere contraseña: **No** (abre el formulario)

## Guardar resultado

- Botón: `Guardar resultado`
- Requiere contraseña: **Sí**

---

# ITEM: LOTERÍAS VENDIDAS

La validación aplica independientemente de la pestaña de lotería seleccionada.

## Nuevo registro

- Enlace: `Nuevo registro`
- Requiere contraseña: **No** (abre el formulario)

## Guardar

- Botón: `Guardar` (crear o editar lotería)
- Requiere contraseña: **Sí**

---

# ITEM: VENTAS

## Autorizar pago

- Botón: `Autorizar pago`
- Requiere contraseña: **Sí**
- Agregada en revisión: actualiza el estado de pago del boleto.

---

# ITEM: VENTAS OFFLINE

## TAB: GENERAR CÓDIGOS

### Guardar

- Botón: `Guardar`
- Requiere contraseña: **Sí**

## TAB: REGISTRO QR VENDIDOS

### Registrar QR vendido

- Botón: `Registrar QR vendido`
- Requiere contraseña: **Sí**

---

# ITEM: CASOS DE PREMIOS

## Validar caso

- Botón: `Validar caso`
- Requiere contraseña: **Sí**
- Agregada en revisión: actualiza el estado del caso.

## Rechazar caso

- Botón: `Rechazar caso`
- Requiere contraseña: **Sí**
- Agregada en revisión: actualiza el estado del caso.

## Asignar observador

- Botón: `Asignar observador`
- Requiere contraseña: **Sí**

## Iniciar caso de premio

- Botón: `Iniciar caso de premio`
- Requiere contraseña: **Sí**
- Agregada en revisión: crea el caso de premio.

## Validar ticket

- Botón: `Validar ticket`
- Requiere contraseña: **No** (solo consulta el boleto)

---

# ITEM: CONFIGURACIÓN

### Guardar configuración

- Botón: `Guardar configuración`
- Requiere contraseña: **Sí**

### Guardar días de venta

- Botón: `Guardar días de venta`
- Requiere contraseña: **Sí**

### Agregar lotería

- Enlace: `Agregar lotería`
- Requiere contraseña: **No** (abre el formulario)

### Guardar catálogo de loterías

- Botón: `Guardar` (crear o editar lotería del catálogo)
- Requiere contraseña: **Sí**

### Deshabilitar lotería

- Botón: `Deshabilitar`
- Requiere contraseña: **Sí**

### Habilitar lotería

- Botón: `Habilitar`
- Requiere contraseña: **Sí**
- Agregada en revisión: actualiza el estado de la lotería, igual que deshabilitar.

---

# ITEM: SOPORTE

### Marcar atendida

- Botón: `Marcar atendida`
- Requiere contraseña: **Sí**
- Agregada en revisión: cierra la conversación (actualiza estado).
- Aplica en **Atención al cliente** y en **Soporte técnico** (misma acción).

Enviar mensajes o iniciar un chat no pide contraseña: es la operación cotidiana del módulo, no un cambio de catálogo ni de configuración.

---

# 5. Flujo de una acción protegida

Ejemplo: guardar un usuario.

```text
Administrador
      ↓
Presiona "Guardar"
      ↓
¿Acción protegida?
      ↓
      Sí
      ↓
Mostrar modal de contraseña
      ↓
Administrador ingresa contraseña
      ↓
Enviar contraseña al backend
      ↓
Validar contraseña
      ↓
¿Contraseña correcta?
      │
      ├── NO
      │    ↓
      │  Mostrar mensaje de error
      │    ↓
      │  NO guardar
      │
      └── SÍ
           ↓
        Ejecutar acción
           ↓
        Guardar usuario

```

---

# 6. Regla para acciones consecutivas

La contraseña debe solicitarse para cada **nueva acción protegida**.

Ejemplo:

```text
Guardar usuario
      ↓
Solicitar contraseña
      ↓
Contraseña correcta
      ↓
Guardar usuario

```

Si posteriormente el administrador realiza otra acción protegida:

```text
Eliminar usuario
      ↓
Solicitar contraseña nuevamente
      ↓
Validar contraseña
      ↓
Eliminar usuario

```

La validación anterior no debe autorizar automáticamente una acción diferente.

---

# 7. Acciones que NO requieren contraseña

- Ver, consultar, buscar, filtrar, paginar
- Navegar entre pestañas o módulos
- Abrir formularios (`Editar`, `Crear`, `Nuevo registro`, `Agregar lotería`, `Registrar resultado`)
- Cancelar, volver, cerrar
- Validar ticket (consulta)
- Enviar mensajes de soporte
- Marcar notificaciones como leídas
- Comprobar el USB al registrar un PDA (`Verificar`)
- Descargar PDF, ver tirilla, KPI
- Cambiar la propia contraseña (ya exige la contraseña actual)
- Cerrar sesión

### Excepción

Aunque `Ver` normalmente no requiere contraseña, las acciones que se ejecutan desde el detalle y que modifican información sí requieren contraseña:

- `Ver grupo → Agregar vendedor`
- `Ver grupo → Quitar del grupo`
- `Ver caso → Validar caso`, `Rechazar caso`, `Asignar observador`

---

# 8. Modal de confirmación

## Título

**Confirmar contraseña**

## Mensaje

> Para continuar con esta acción, ingrese la contraseña del administrador.

## Campo

```text
Contraseña

```

El campo debe permitir ingresar la contraseña de forma oculta.

## Botones

```text
Cancelar    Continuar

```

---

# 9. Mensajes

## Contraseña incorrecta

> La contraseña ingresada es incorrecta. No se realizó ninguna modificación.

## Cancelación

> No se realizó ninguna modificación.

## Error de validación

> No fue posible validar la contraseña. Intente nuevamente.

---

# 10. Reglas de seguridad

### Regla 1

La acción no debe ejecutarse antes de validar correctamente la contraseña.

### Regla 2

La contraseña debe validarse en el backend.

### Regla 3

No se debe almacenar la contraseña en el frontend.

### Regla 4

No se debe considerar válida una contraseña simplemente porque el usuario tenga abierta la sesión administrativa.

### Regla 5

Una validación exitosa debe estar asociada a la acción específica que se está ejecutando.

### Regla 6

Si la contraseña es incorrecta, la operación debe cancelarse completamente.

### Regla 7

Si el administrador cancela el modal, la operación debe cancelarse completamente.

### Regla 8

Las operaciones de consulta que no modifican información no requieren contraseña, salvo las excepciones indicadas en este documento.

---

# 11. Lista consolidada de acciones protegidas


| Módulo            | Acción                                      | Requiere contraseña |
| ----------------- | ------------------------------------------- | ------------------- |
| Usuario           | Crear usuario → Guardar                     | Sí                  |
| Usuario           | Editar usuario → Guardar                    | Sí                  |
| Usuario           | Restablecer contraseña                      | Sí                  |
| Usuario           | Desbloquear usuario                         | Sí                  |
| Usuario           | Cambiar grupo                               | Sí                  |
| Usuario           | Eliminar usuario                            | Sí                  |
| Grupos            | Guardar grupo / Guardar nuevo grupo         | Sí                  |
| Grupos            | Agregar vendedor                            | Sí                  |
| Grupos            | Quitar vendedor                             | Sí                  |
| Grupos            | Eliminar grupo                              | Sí                  |
| PDA               | Asociar                                     | Sí                  |
| PDA               | Desasociar                                  | Sí                  |
| PDA               | Bloquear                                    | Sí                  |
| PDA               | Desbloquear                                 | Sí                  |
| PDA               | Eliminar                                    | Sí                  |
| PDA               | Registrar PDA → Continuar                   | Sí                  |
| PDA               | Registrar PDA → Reintentar                  | Sí                  |
| Resultados        | Guardar resultado                           | Sí                  |
| Loterías vendidas | Guardar (crear o editar)                    | Sí                  |
| Ventas            | Autorizar pago                              | Sí                  |
| Ventas Offline    | Generar códigos → Guardar                   | Sí                  |
| Ventas Offline    | Registrar QR vendido                        | Sí                  |
| Premios           | Validar caso                                | Sí                  |
| Premios           | Rechazar caso                               | Sí                  |
| Premios           | Asignar observador                          | Sí                  |
| Premios           | Iniciar caso de premio                      | Sí                  |
| Configuración     | Guardar configuración                       | Sí                  |
| Configuración     | Guardar días de venta                       | Sí                  |
| Configuración     | Catálogo → Guardar                          | Sí                  |
| Configuración     | Catálogo → Deshabilitar                     | Sí                  |
| Configuración     | Catálogo → Habilitar                        | Sí                  |
| Soporte           | Marcar atendida (Atención al cliente)       | Sí                  |
| Soporte técnico   | Marcar atendida                             | Sí                  |


---

# 12. Consideración de implementación

Se recomienda implementar la validación mediante un **mecanismo centralizado de acciones protegidas**, evitando desarrollar una lógica diferente para cada botón.

Conceptualmente:

```text
Acción administrativa
        ↓
¿Requiere contraseña?
        ↓
       Sí
        ↓
Solicitar contraseña
        ↓
Validar en backend
        ↓
¿Autorizado?
   ├── No → Cancelar acción
   │
   └── Sí → Ejecutar acción

```

De esta manera, todas las acciones utilizan el mismo mecanismo de seguridad y se evita duplicar código.

Si posteriormente se requiere proteger una nueva acción, únicamente debe agregarse al conjunto de **acciones administrativas protegidas**.
