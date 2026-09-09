# Pruebas del administrador New Rich

Documento de alcance para las pruebas Playwright del sitio MVC **New Rich Administración**.

- **Aplicación:** `Proyectos/Administrador` (`NewRich.Admin`)
- **URL base:** `http://localhost:5274`
- **API:** `http://localhost:5295` (el MVC no consulta SQL Server; toda operación pasa por la API)
- **Idioma de la interfaz:** español de Colombia (`lang=es-CO`)
- **Ejecución:** desde esta carpeta, `npm test` (Chromium, Firefox, WebKit y un viewport móvil)
- **Credenciales:** usuario administrador existente. No usar datos de demostración del mockup (`admin/admin123`).

Las pruebas se ejecutarán **en este orden**. Cada caso cubre un recorro de usuario. Si un caso depende de un registro creado antes, se indica en **Datos**.

## Precondiciones

1. La API New Rich está en ejecución en `http://localhost:5295`.
2. El administrador MVC está en ejecución en `http://localhost:5274`.
3. Existe un usuario con rol **Administrador**, estado activo y contraseña definitiva (no temporal).
4. El navegador de prueba no tiene sesión previa (o se limpia el almacenamiento al inicio de cada archivo).
5. Los textos de la interfaz no deben contener emojis. Los controles de tabla deben ofrecer 5, 10 y 15 filas (máximo 15) y paginación **Inicio**, **Anterior**, números, **Siguiente** y **Último**.
6. Todo desplegable (`select.searchable`) debe permitir buscar opciones **dentro del mismo control**.

## Convenciones

| Código | Significado |
|---|---|
| PA-xxx | Caso Playwright del administrador, en orden de ejecución |
| OK | El flujo feliz debe completar y mostrar el resultado esperado |
| Error | Debe mostrar mensaje de la API o de validación, sin pantalla en blanco |
| Bloqueo | El usuario no autenticado o sin rol administrador no entra al módulo |

---

## 1. Acceso, sesión y cambio de contraseña

### PA-001 Ingreso visible
- **Ruta:** `/Cuenta/Ingresar`
- **Qué probar:** Título **Iniciar sesión**, marca NEW RICH / ADMINISTRACIÓN, campos Usuario y Contraseña, botón **Ingresar**, `lang=es-CO`, sin emojis.
- **Esperado:** Formulario usable con teclado y con foco visible.

### PA-002 Ingreso con credenciales incorrectas
- **Acción:** Enviar usuario o contraseña inválidos.
- **Esperado:** Mensaje de error de la API (por ejemplo *Usuario o contraseña incorrectos.*) y permanencia en la pantalla de ingreso.

### PA-003 Ingreso de un rol que no es administrador
- **Datos:** Usuario vendedor u observador válido en la API.
- **Esperado:** No entra al escritorio. Mensaje de que solo un administrador puede usar esta aplicación.

### PA-004 Ingreso correcto de administrador
- **Acción:** Ingresar con administrador activo.
- **Esperado:** Redirección a Resumen (`/`). Menú lateral visible. Nombre del usuario en la cabecera. Enlace **Avisos**. Botón **Cerrar sesión**.

### PA-005 Forzar cambio de contraseña temporal
- **Datos:** Usuario administrador con contraseña temporal (`DebeCambiarPassword`).
- **Esperado:** Tras el ingreso, redirección a `/Cuenta/CambiarPassword`. No puede abrir Resumen ni otros módulos hasta cambiar la contraseña.

### PA-006 Cambio de contraseña: validación
- **Ruta:** `/Cuenta/CambiarPassword`
- **Acciones:** Vacío; nueva distinta de la confirmación; nueva igual a la actual; formato débil.
- **Esperado:** Mensajes de validación. No entra al escritorio.

### PA-007 Cambio de contraseña: éxito
- **Acción:** Contraseña actual correcta, nueva fuerte y confirmación igual.
- **Esperado:** Aviso de éxito y acceso a Resumen. La sesión siguiente usa la nueva contraseña.

### PA-008 Rutas protegidas sin sesión
- **Acción:** Visitar `/`, `/Usuarios`, `/Dispositivos`, `/Ventas`, `/Soporte` sin cookie.
- **Esperado:** Redirección a `/Cuenta/Ingresar`.

### PA-009 Cerrar sesión
- **Acción:** **Cerrar sesión** en el pie del menú.
- **Esperado:** Vuelve a ingreso. Un **Atrás** del navegador no reabre el escritorio autenticado (o redirige a ingreso).

---

## 2. Cascarón del sitio (todas las páginas autenticadas)

Estas comprobaciones se reutilizan en cada módulo. Además hay casos dedicados:

### PA-010 Menú lateral completo y activo
- **Qué probar:** Los 13 destinos, en este orden: Resumen, Usuarios, Grupos, PDA, Loterías, Resultados, Ventas, Ventas offline, Casos de premios, KPI, Notificaciones, Soporte, Configuración.
- **Esperado:** Cada enlace abre su módulo. El ítem actual tiene estado `active` (no solo color). El migas muestra `New Rich /` más el nombre de la sección.

### PA-011 Marca y cabecera
- **Qué probar:** Enlace NR al Resumen, texto Administración, **Avisos** (texto, no emoji), nombre e iniciales del usuario.

### PA-012 Barra móvil
- **Viewport:** ancho menor o igual a 650 px.
- **Qué probar:** El menú lateral se oculta. Aparecen Resumen, Usuarios, Ventas y Soporte. El contenido no queda tapado. No hay scroll horizontal.

### PA-013 Menú colapsado en tablet
- **Viewport:** ancho menor o igual a 1050 px y mayor a 650 px.
- **Esperado:** Columna estrecha, marca NR, etiquetas del menú ocultas, contenido reflow.

### PA-014 Sin emojis ni textos en inglés
- **Qué probar:** Recorrido de todas las vistas listadas en este documento.
- **Esperado:** `lang=es-CO`. Sin emojis. Etiquetas en español (Ingresar, Buscar, Limpiar, Guardar, Cancelar, Inicio, Anterior, Siguiente, Último).

---

## 3. Resumen

**Ruta:** `/` o `/Inicio`

### PA-015 Encabezado y acción primaria
- **Qué probar:** Kicker *Control operativo*, título *Resumen de hoy*, botón **Registrar resultado** hacia `/Resultados/Crear`.

### PA-016 Indicadores del día
- **Qué probar:** Ventas del día, boletos emitidos, vendedores activos, alertas pendientes. Valores numéricos coherentes con la API (no datos del mockup).

### PA-017 Últimas ventas
- **Qué probar:** Tabla con código, vendedor, fecha y hora, total, estado. Enlace **Ver módulo de ventas** a `/Ventas`.
- **Vacío:** Texto *No hay registros para mostrar.* si no hay ventas del día.

### PA-018 API caída en Resumen
- **Acción:** Detener la API y recargar `/`.
- **Esperado:** Mensaje de servidor central no disponible o redirección a ingreso si la sesión queda inválida. Sin excepción HTML cruda.

---

## 4. Usuarios

**Ruta índice:** `/Usuarios`

### PA-019 Listado, filtros y paginación
- **Qué probar:** Buscar por nombre, documento o alias. Filtro de rol (Todos / Administrador / Vendedor / Observador) con buscador interno. **Filas por página** 5, 10 y 15. **Buscar** y **Limpiar filtros**.
- **Paginación:** Inicio, Anterior, números, Siguiente, Último. Resumen *Mostrando x-y de z*. Conservar filtros al cambiar de página. No ofrecer más de 15 filas.
- **Tabla:** Columnas identificador, nombre, rol, PDA, estado, acciones. Resaltado de fila en hover y foco.

### PA-020 Crear usuario: validación
- **Ruta:** `/Usuarios/Crear`
- **Acción:** Enviar vacío; rol inválido no aplica (el combo limita).
- **Esperado:** Nombre completo y nombre de usuario obligatorios. Cancelar vuelve al listado.

### PA-021 Crear usuario: éxito
- **Datos:** Nombre, usuario único, alias, documento único, celular, correo, rol Vendedor u Observador, PDA opcional.
- **Esperado:** Vuelve al listado con aviso de creación y contraseña temporal visible en el aviso.

### PA-022 Crear usuario: duplicados
- **Acción:** Repetir nombre de usuario o documento.
- **Esperado:** Mensaje de la API, formulario con los valores ingresados.

### PA-023 Ver detalle
- **Ruta:** `/Usuarios/Detalle/{id}`
- **Qué probar:** Nombre, identificador, rol, estado (incluido Bloqueado), PDA, documento, correo. **Cerrar** y **Restablecer contraseña**.

### PA-024 Editar usuario
- **Ruta:** `/Usuarios/Editar/{id}`
- **Qué probar:** Nombre de usuario de solo lectura. Cambio de nombre, alias, estado Activo/Inactivo, PDA. **Guardar cambios**.
- **Esperado:** Aviso de actualización y datos persistidos en el listado.

### PA-025 Restablecer contraseña desde el listado y desde el detalle
- **Esperado:** Aviso con contraseña temporal. El usuario afectado debe cambiarla al ingresar (en su cliente). El administrador sigue en sesión.

### PA-026 Desbloquear usuario
- **Datos:** Usuario bloqueado por intentos fallidos.
- **Esperado:** El botón **Desbloquear usuario** aparece solo si está bloqueado. Tras confirmar, aviso de desbloqueo. Si no está bloqueado, la API indica que no lo está.

### PA-027 Eliminar usuario: advertencia
- **Ruta:** `/Usuarios/Eliminar/{id}`
- **Qué probar:** Texto de irreversibilidad, nombre del usuario, **Cancelar** y **Sí, eliminar usuario**.

### PA-028 Eliminar usuario: confirmación
- **Esperado:** Si la API lo permite, desaparece del listado. Si hay FK o es el último administrador o es el propio usuario, mensaje de error claro (no página 500 en blanco).

### PA-029 No eliminar la sesión actual
- **Acción:** Intentar eliminar el administrador autenticado.
- **Esperado:** Mensaje *Un usuario no puede eliminar su propia cuenta.*

---

## 5. Grupos

**Ruta:** `/Grupos`

### PA-030 Módulo aún no disponible
- **Qué probar:** Título *Grupos de vendedores*, aviso de que el módulo no está en la API, estado vacío.
- **Esperado:** No hay alta ni edición. El ítem de menú **Grupos** permanece visible y marcado como activo.

---

## 6. PDA (dispositivos)

**Ruta índice:** `/Dispositivos`

### PA-031 Listado, filtros y paginación
- **Qué probar:** Aviso del orden recomendado (registrar PDA, luego usuario). Buscar por código o usuario. Filtro de estado. Filas 5/10/15. Paginación completa. Columnas PDA, usuario, tipo, conexión, estado, acciones.

### PA-032 Registrar PDA: validación
- **Ruta:** `/Dispositivos/Crear`
- **Acción:** Código vacío.
- **Esperado:** Código obligatorio. Tipo Vendedor u Observador con buscador. Cancelar vuelve al listado.

### PA-033 Registrar PDA: éxito
- **Datos:** Código único. Modelo y serie opcionales (si la serie va vacía, el sistema debe no chocar con el único `NULL` de SQL).
- **Esperado:** Aviso de registro creado y fila en el listado.

### PA-034 Registrar PDA: código duplicado
- **Esperado:** Mensaje de la API.

### PA-035 Asociar PDA
- **Acción:** Elegir un usuario en el desplegable con búsqueda y **Asociar**.
- **Esperado:** El PDA muestra el usuario. Un vendedor no opera sin PDA asociado (regla de negocio verificable al consultar el dato).

### PA-036 Desasociar PDA
- **Esperado:** El usuario pasa a *Sin asociar*.

### PA-037 Inactivar y activar PDA
- **Acción:** **Inactivar** / **Activar**.
- **Esperado:** El estado en la tabla cambia. La API confirma.

---

## 7. Loterías

**Ruta índice:** `/Loterias`

### PA-038 Listado, filtros y paginación
- **Qué probar:** Buscar por nombre. Estado Todos/Activo/Inactivo. Filas 5/10/15. Paginación. Enlace **Buscar boletos**.

### PA-039 Nueva lotería: validación y éxito
- **Ruta:** `/Loterias/Crear`
- **Qué probar:** Nombre obligatorio. Guardar. Aparece en el catálogo como Activo.

### PA-040 Editar lotería
- **Ruta:** `/Loterias/Editar/{id}`
- **Qué probar:** Cambiar nombre y estado. Guardar. Cancelar.

### PA-041 Buscar boletos vendidos
- **Ruta:** `/Loterias/Boletos`
- **Filtros:** Código, número jugado, vendedor, fecha, estado, lotería (con buscador), filas por página.
- **Qué probar:** Leyenda de estados (Jugado, Ganador, Pagado, Vencido, No ganador). Tabla con paginación. **Ver ticket** abre tirilla en Ventas. **Limpiar** restablece filtros.
- **Vacío:** Mensaje de sin registros.

---

## 8. Resultados

**Ruta índice:** `/Resultados`

### PA-042 Listado y filtros
- **Qué probar:** Lotería con buscador, fecha de juego, filas 5/10/15, paginación, **Limpiar**. Columnas fecha, lotería, número ganador.

### PA-043 Registrar número ganador: validación
- **Ruta:** `/Resultados/Crear` (también desde Resumen).
- **Qué probar:** Aviso de unicidad fecha + lotería. Lotería obligatoria (solo activas). Número obligatorio. Cancelar.

### PA-044 Registrar número ganador: éxito
- **Datos:** Lotería activa, fecha, número de 4 dígitos.
- **Esperado:** Aviso *El número ganador fue registrado correctamente.* Aparece en el listado.

### PA-045 Registrar número ganador: duplicado
- **Acción:** Misma lotería y fecha.
- **Esperado:** Error de la API. No se duplica la fila.

---

## 9. Ventas y boletos

**Ruta índice:** `/Ventas`

### PA-046 Listado, leyenda, filtros y paginación
- **Qué probar:** Leyenda de estados. Buscar código o vendedor. Estado (Jugado, Ganador, Pagado, Vencido). Filas 5/10/15. Paginación. Columnas código, vendedor, fecha, total, estado, acciones.

### PA-047 Ver ticket / tirilla
- **Ruta:** `/Ventas/Tirilla/{id}`
- **Qué probar:** Recibo con código, fecha, vendedor, líneas de juego, total, leyenda. **Cerrar** vuelve a ventas.
- **Error:** Boleto inexistente muestra aviso y vuelve al listado.

### PA-048 Autorizar pago
- **Condición:** El botón solo en estado **Ganador**.
- **Esperado:** Tras confirmar, el estado pasa a pagado/cobrado o se muestra el error de la API (vigencia, ya pagado, no ganador).

---

## 10. Ventas offline

**Ruta:** `/Offline`

### PA-049 Módulo aún no disponible
- **Qué probar:** Título *Ventas offline*, aviso de API no implementada, estado vacío. Menú activo.

---

## 11. Casos de premios

**Ruta:** `/Premios`

### PA-050 Módulo aún no disponible
- **Qué probar:** Título *Casos de premios ganadores*, aviso de API no implementada, estado vacío. Menú activo.

---

## 12. KPI

**Ruta:** `/Kpi`

### PA-051 Filtros
- **Qué probar:** Fecha inicial, fecha final, vendedor con buscador interno, **Actualizar**. Conservar valores al recargar el resultado.

### PA-052 Indicadores
- **Qué probar:** Ingresos y ventas (totales, cantidad, boletos, números ganadores). Personal (vendedores, observadores, activos, inactivos). Loterías utilizadas si la API las envía.
- **Esperado:** Cifras de la API, no del prototipo estático.

### PA-053 Sin datos / error de API
- **Esperado:** Estado vacío o aviso. No se rompe el diseño.

---

## 13. Notificaciones

**Ruta:** `/Notificaciones`

### PA-054 Acceso desde Avisos
- **Acción:** Clic en **Avisos** en la cabecera.
- **Esperado:** Llega a esta página. El menú Notificaciones queda activo.

### PA-055 Bandeja, filtros y paginación
- **Qué probar:** Buscar por tipo o mensaje. Estado Todos / Pendiente / Leída. Filas 5/10/15. Paginación. Columnas tipo, mensaje, fecha, estado. Contador de pendientes.

### PA-056 Marcar como leídas
- **Acción:** **Marcar como leídas**.
- **Esperado:** Aviso de la API. Pendientes en 0 (o el valor que devuelva la API). Filtro *Pendiente* queda vacío si corresponde.

---

## 14. Soporte (chat)

**Ruta:** `/Soporte`

### PA-057 Lista de conversaciones
- **Qué probar:** Título *Soporte*, panel *Conversaciones*, lista a la izquierda, ventana a la derecha. Estado vacío si no hay chats.

### PA-058 Iniciar conversación
- **Qué probar:** Destinatario con buscador (vendedores y observadores). Mensaje obligatorio. **Iniciar conversación**.
- **Esperado:** Aviso de conversación iniciada. El hilo queda seleccionado con el primer mensaje.

### PA-059 Abrir conversación existente
- **Acción:** Clic en un ítem de la lista.
- **Esperado:** Mensajes propios a la derecha, ajenos a la izquierda. Fecha/hora visible.

### PA-060 Enviar mensaje
- **Qué probar:** Campo *Escribe un mensaje* y **Enviar**. Vacío no envía o muestra validación.
- **Esperado:** El texto aparece en el hilo.

### PA-061 Marcar atendida / cerrar
- **Acción:** **Marcar atendida**.
- **Esperado:** Aviso de conversación cerrada. El estado deja de ser abierta.

### PA-062 Chat en viewport estrecho
- **Viewport:** móvil.
- **Esperado:** Lista y ventana apiladas, área de escritura usable, sin scroll horizontal.

---

## 15. Configuración

**Ruta:** `/Configuracion`

### PA-063 Listar parámetros
- **Qué probar:** Cada clave de la API (por ejemplo `HoraCierre`, `VigenciaPremiosDias`, `AlertaRepeticionNumero`, `AlertaValorMinimo`, `CodigosOfflineCapacidad`, `SincronizacionModo`) con su valor y botón **Guardar configuración**.

### PA-064 Guardar un parámetro
- **Acción:** Cambiar un valor y guardar.
- **Esperado:** Aviso de actualización. Al recargar, el valor persiste. Luego restaurar el valor original para no alterar el ambiente.

### PA-065 Valor vacío
- **Esperado:** Mensaje de valor obligatorio o rechazo de la API.

---

## 16. Paginación y desplegables (transversal)

Aplicar en **todas** las tablas: Usuarios, PDA, Loterías, Boletos, Resultados, Ventas, Notificaciones.

### PA-066 Tamaño de página 5, 10 y 15
- **Esperado:** Nunca hay opción mayor a 15. Al cambiar el tamaño, la tabla muestra como máximo esa cantidad. El total *de z* se mantiene.

### PA-067 Navegación Inicio / Anterior / Siguiente / Último
- **Datos:** Suficientes filas para varias páginas (crear datos de prueba o usar página 5).
- **Esperado:** En la primera página Inicio y Anterior están deshabilitados. En la última, Siguiente y Último deshabilitados. Los números identifican la página actual.

### PA-068 Buscador dentro de cada desplegable
- **Qué probar:** Rol, estado, lotería, vendedor, destinatario de chat, tipo de PDA, y cualquier `select.searchable`.
- **Esperado:** Al enfocar, se puede escribir. Filtra opciones. Si no hay coincidencias: *No se encontraron resultados*. Escape cierra. Enter toma la primera coincidencia. En móvil el menú cabe en el viewport.

### PA-069 Fila activa en tablas
- **Esperado:** Hover y `:focus-within` resaltan la fila. El resaltado no es la única pista de datos (el contenido sigue siendo legible).

---

## 17. Estados de error, vacío y sesión expirada

### PA-070 Estado vacío en cada listado
- **Acción:** Filtros que no coincidan.
- **Esperado:** *No hay registros para mostrar.* Paginación coherente (0 de 0).

### PA-071 Sesión expirada a mitad de uso
- **Acción:** Invalidar el token (logout en otra pestaña o token viejo) y disparar un listado.
- **Esperado:** Vuelta a ingreso, opcionalmente con aviso de sesión expirada.

### PA-072 API detenida en un módulo interno
- **Acción:** Con sesión abierta, detener la API y abrir Usuarios o Ventas.
- **Esperado:** Aviso de conexión con el servidor central. No se pierde el cascarón si es posible, o redirección controlada.

### PA-073 Tokens CSRF
- **Qué probar:** Formularios POST (ingreso, crear, guardar, eliminar, chat, marcar leídas, cerrar sesión) incluyen token antifalsificación. Un POST sin token no debe completar la operación.

---

## 18. Recorrido de regresión de punta a punta (orden final)

Este caso se corre al final, con un administrador ya autenticado, y visita **todas** las pantallas en el orden del menú.

### PA-074 Recorrido completo autenticado
1. Resumen  
2. Usuarios (índice, crear, cancelar, detalle de un registro, editar cancelar, eliminar cancelar)  
3. Grupos  
4. PDA (índice y crear cancelar)  
5. Loterías (índice, crear cancelar, buscar boletos)  
6. Resultados (índice y registrar cancelar)  
7. Ventas (índice; tirilla si hay boleto)  
8. Ventas offline  
9. Casos de premios  
10. KPI  
11. Notificaciones  
12. Soporte  
13. Configuración  
14. Cerrar sesión  

**Esperado:** Ninguna ruta 500. Ningún texto en inglés. Ningún emoji. Menú y migas correctos en cada paso. En móvil, las cuatro acciones de la barra inferior funcionan.

### PA-075 Recorrido móvil del menú inferior
- **Viewport:** Pixel 7 o ancho 390.
- **Acciones:** Resumen → Usuarios → Ventas → Soporte.
- **Esperado:** Cada destino carga. El ítem activo se distingue por texto, no solo por color.

---

## Mapa de rutas cubiertas

| Orden | Módulo | Rutas |
|---|---|---|
| 1 | Cuenta | `/Cuenta/Ingresar`, `/Cuenta/CambiarPassword`, `/Cuenta/Salir` |
| 2 | Resumen | `/`, `/Inicio` |
| 3 | Usuarios | `/Usuarios`, `/Usuarios/Crear`, `/Usuarios/Editar/{id}`, `/Usuarios/Detalle/{id}`, `/Usuarios/Eliminar/{id}`, POST restablecer / desbloquear / confirmar eliminar |
| 4 | Grupos | `/Grupos` |
| 5 | PDA | `/Dispositivos`, `/Dispositivos/Crear`, POST asociar / desasociar / cambiar estado |
| 6 | Loterías | `/Loterias`, `/Loterias/Crear`, `/Loterias/Editar/{id}`, `/Loterias/Boletos` |
| 7 | Resultados | `/Resultados`, `/Resultados/Crear` |
| 8 | Ventas | `/Ventas`, `/Ventas/Tirilla/{id}`, POST pagar |
| 9 | Offline | `/Offline` |
| 10 | Premios | `/Premios` |
| 11 | KPI | `/Kpi` |
| 12 | Notificaciones | `/Notificaciones`, POST marcar leídas |
| 13 | Soporte | `/Soporte`, POST iniciar / enviar / cerrar |
| 14 | Configuración | `/Configuracion`, POST guardar |

No quedan pantallas Razor del administrador fuera de esta lista (`Error` genérico se cubre de forma implícita si una ruta falla).

## Cómo ejecutar (cuando los specs estén implementados)

```bash
cd D:\Desarrollo\chances\SWApuestas\playwright
npm test
```

Variables opcionales:

- `ADMIN_BASE_URL` (por defecto `http://localhost:5274`)
- `ADMIN_USER` y `ADMIN_PASSWORD` para los casos autenticados

El archivo de humo actual solo cubre **PA-001**. El resto de este documento es el plan de implementación de las pruebas, en el mismo orden.
