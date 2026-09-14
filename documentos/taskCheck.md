# taskCheck — checklist de `requerimientoApuestas.md`

Marca **Hecho** con `[x]` cuando el ítem ya esté creado. El orden sigue el documento fuente.

Leyenda: `[ ]` pendiente · `[x]` creado.

---

## Encabezado del documento

| Hecho | Ítem |
| --- | --- |
| [ ] | Tecnología objetivo: API .NET, SQL Server, Android vendedor/observador, ASP.NET MVC sobre IIS para administración |

---

## 1. Problema

| Hecho | Ítem |
| --- | --- |
| [ ] | Registrar ventas desde PDA, emitir boleto verificable y consultar/validar resultado sin papel, cálculos manuales ni dispositivos no autorizados |
| [ ] | Venta disponible si falla impresora o el PDA pierde conexión |
| [ ] | Administración y observación con información central y trazable |
| [ ] | Confirmación y seguridad del boleto dependen del servidor (no ventas offline no autorizadas) |
| [ ] | Impedir boletos falsos, duplicados o cobrados más de una vez |

---

## 2. Resultado esperado

| Hecho | Ítem |
| --- | --- |
| [ ] | Vendedor autorizado registra venta con uno o varios juegos y loterías |
| [ ] | Imprimir o recuperar tirilla |
| [ ] | Sistema identifica y valida el boleto de forma segura |
| [ ] | Administración opera usuarios, PDA, loterías, resultados y reportes |
| [ ] | Observación consulta y valida sin modificar la operación comercial |

---

## 3. Principios de alcance

| Hecho | Ítem |
| --- | --- |
| [ ] | Tiempo fijo, alcance variable por apuesta |
| [ ] | SQL Server y API como fuente definitiva; PDA réplica operativa temporal offline |
| [ ] | Validez de boleto, QR, vigencia y cobro se decide en el servidor |
| [ ] | El sistema no calcula ni liquida premios; determina ganador / no ganador / vencido / pagado |
| [ ] | El valor informado manualmente por el observador sí se registra durante la entrega |

---

## 4. Apuestas de producto

| Hecho | Apuesta | Resultado que se entrega |
| --- | --- | --- |
| [ ] | A1. Venta y boleto seguro | Venta desde PDA autorizado, tirilla/PDF, código y QR validables |
| [ ] | A2. Resultados y validación | Registro de ganadores y validación de boleto por QR |
| [ ] | A3. Administración y supervisión | Gestión de usuarios/PDA/loterías, consultas, KPI y notificaciones |
| [ ] | A4. Conectividad obligatoria y soporte | Pausa y reanudación segura de borradores, más consulta de soporte |
| [ ] | A5. Operación offline empresarial con códigos preasignados | Admin asigna códigos QR offline, vendedor usa offline, admin registra por escaneo |
| [ ] | A6. Proceso de validación y entrega de premios ganadores | Vendedor prevalida, admin valida y asigna observador, observador registra con datos y 3 fotos |

---

# A1. Venta y boleto seguro

### Actores y acceso

| Hecho | Ítem |
| --- | --- |
| [ ] | Login vendedor: usuario, contraseña y PDA previamente asociado |
| [ ] | Activo/Inactivo controla si puede operar |
| [ ] | `estadoValidado` true al crear o restablecer; false al establecer contraseña definitiva |
| [ ] | `estadoBloqueado` por 3 intentos fallidos; solo admin desbloquea |
| [ ] | Si `estadoValidado` es true, obliga a definir contraseña definitiva antes de operar |
| [ ] | API valida usuario existente/activo, contraseña, PDA registrado/activo/asociado, sesión y horario |
| [ ] | Fuera de horario: no sesiones ni apuestas nuevas; mensaje `LOS JUEGOS ESTÁN CERRADOS` |
| [ ] | Si el cierre llega durante una venta iniciada, se permite terminarla; luego se cierra sesión |

### Recorrido de venta

| Hecho | Ítem |
| --- | --- |
| [ ] | Flujo LOGIN → HOME → JUEGO NUEVO → tipo COMBINADA o INDIVIDUAL → JUGAR → registrar → código/QR → imprimir o PDF |
| [ ] | Boleto COMBINADA o INDIVIDUAL, sin mezclar ambos |
| [ ] | COMBINADA: número, valor y múltiples loterías; total = valor × cantidad de loterías |
| [ ] | INDIVIDUAL: línea con número, valor y una lotería; total = suma de valores |
| [ ] | Máximos configurables por administrador; INDIVIDUAL no supera 6 líneas |
| [ ] | Botón Agregar se desactiva al alcanzar el máximo |
| [ ] | Editar o eliminar juegos/líneas antes de confirmar |
| [ ] | JUGAR registra venta pagada al portador; no se recopila datos del comprador |
| [ ] | Sin conexión: conservar borrador en memoria y avisar que debe reconectarse |

### Datos mínimos de la venta

| Hecho | Ítem |
| --- | --- |
| [ ] | Venta guarda: IDs venta/boleto, vendedor y alias, fecha/hora, `TipoApuesta` (COMBINADO/INDIVIDUAL) |
| [ ] | Juegos/líneas, números, valores, loterías, total, estado del boleto, creación y sincronización si aplica |
| [ ] | Estados: Por jugar, Jugado, Ganador, No ganador, Vencido, Pagado/cobrado |

### Código público y QR

| Hecho | Ítem |
| --- | --- |
| [ ] | Identificador interno GUID/UUID no expuesto como validación al usuario |
| [ ] | Código público exactamente 7 dígitos (`0000000`–`9999999`), ceros a la izquierda, inmutable, índice único |
| [ ] | Colisión: la API genera otro código y revalida antes de confirmar |
| [ ] | Formato impreso `RECIBO DE VENTA AOL-1234567` (`AOL-` prefijo visual) |
| [ ] | Clave de validación aleatoria y única por boleto al confirmar |
| [ ] | QR: payload cifrado/autenticado (id interno, código público, clave, versión, id de clave) |
| [ ] | Cifrado autenticado (p. ej. AES-GCM): versión, id clave, nonce, cifrado, etiqueta |
| [ ] | Clave maestra solo en servidor; nunca en QR, PDA ni tirilla |
| [ ] | BD almacena hash de la clave de validación, no texto plano |

### Tirilla y contingencia

| Hecho | Ítem |
| --- | --- |
| [ ] | Tirilla: código, fecha/hora, juegos, número, valor, loterías, totales, estado, QR, `Paga al portador` |
| [ ] | Impresión automática en impresora térmica integrada tras confirmar |
| [ ] | Falla de impresión: venta válida, boleto local, visualizable, PDF para compartir |
| [ ] | La falla no revierte ni duplica la venta |

### Hecho cuando (A1)

| Hecho | Ítem |
| --- | --- |
| [ ] | Vendedor autorizado crea boleto de varios juegos y loterías con total correcto |
| [ ] | La misma venta no puede confirmarse dos veces |
| [ ] | GUID/UUID, código público único de 7 dígitos y QR cifrado/autenticado |
| [ ] | Servidor descifra QR y compara hash de clave de validación |
| [ ] | Tirilla formato `AOL-1234567` |
| [ ] | Venta disponible y PDF si falla impresión |

---

# A2. Resultados y validación de boletos

| Hecho | Ítem |
| --- | --- |
| [ ] | Admin registra números ganadores por fecha y lotería |
| [ ] | Varios ganadores en una fecha, solo uno por `FechaJuego + LoteriaId` (restricción en BD) |
| [ ] | El mismo número puede ganar en loterías diferentes |
| [ ] | Validación QR: cámara; API descifra/verifica payload y comprueba boleto, código y hash |
| [ ] | Validación recibo: `AOL-` + 7 dígitos o solo 7 dígitos; busca por `CodigoPublico` |
| [ ] | Comprueba número, lotería, fecha, vigencia y si fue cobrado |
| [ ] | Solo Administrador autoriza cobro y cambia a `PAGADO`; Observador solo consulta |
| [ ] | Mensaje `NO SE ENCONTRÓ INFORMACIÓN` (QR inválido / recibo o boleto inexistente) |
| [ ] | Mensaje `GANADOR` (ganador pendiente) |
| [ ] | Mensaje `PAGADO` (ganador ya cobrado) |
| [ ] | Mensaje `NO GANADOR` |
| [ ] | Mensaje `BOLETO VENCIDO` |
| [ ] | Vigencia inicial 30 días calendario, configurable por administrador |

### Hecho cuando (A2)

| Hecho | Ítem |
| --- | --- |
| [ ] | Admin no puede registrar dos resultados para la misma fecha y lotería |
| [ ] | Observador valida QR o recibo y ve estado correcto sin modificar el boleto |
| [ ] | Boleto ganador cobrado deja de mostrarse como pendiente |
| [ ] | QR alterado o boleto inexistente nunca se valida como auténtico |

---

# A3. Administración y supervisión

### Perfiles (matriz)

| Hecho | Función |
| --- | --- |
| [ ] | Crear apuesta / imprimir / PDF: solo Vendedor |
| [ ] | Consultar ventas e históricos: vendedor propias; observador y admin sí |
| [ ] | Validar QR: Observador y Administrador |
| [ ] | Validar boleto por recibo: Observador y Administrador |
| [ ] | Administrar usuarios, PDA y loterías: Admin sí; Observador solo lectura |
| [ ] | Registrar ganadores, horarios y vigencia: solo Administrador |
| [ ] | Autorizar cobro y cambiar a `PAGADO`: solo Administrador |
| [ ] | Restablecer contraseña: solo Administrador |
| [ ] | Crear y administrar grupos: solo Administrador |
| [ ] | Cambiar grupo de vendedor: solo Administrador |
| [ ] | KPI con filtro por grupos: Admin sí; Observador solo lectura |

### Administración

| Hecho | Ítem |
| --- | --- |
| [ ] | Crear vendedores desde solicitudes externas (nombre, usuario, alias, documento, contacto, rol, estado, PDA, info técnica) |
| [ ] | Obligatorio asignar un grupo a cada vendedor al crearlo |
| [ ] | Crear grupos con nombre libremente definible |
| [ ] | Al crear: `estadoValidado` true y contraseña temporal solo en pantalla |
| [ ] | Restablecer contraseña (vendedor ACTIVO): temporal en pantalla, `estadoValidado` true, cierra sesiones |
| [ ] | Desbloquear (ACTIVO bloqueado): `estadoBloqueado` false, `estadoValidado` true, intentos = 0, cierra sesiones |
| [ ] | Cambiar grupo: selecciona nuevo grupo, mueve vendedor con histórico y datos |
| [ ] | Asociar PDA antes de entregarlo; activar/desactivar usuarios y dispositivos |
| [ ] | Administrar loterías, hora de cierre, vigencia, configuraciones y notificaciones |
| [ ] | Consultar por número, vendedor, boleto o lotería; tirilla equivalente |
| [ ] | KPI: usuarios activos/inactivos, ventas por período/vendedor/día, números jugados/ganadores |
| [ ] | Filtro dinámico por grupo (general o grupo específico) actualiza indicadores en tiempo real |
| [ ] | Admin y observador descargan informe PDF con filtros aplicados |
| [ ] | Observador consulta el mismo módulo KPI en solo lectura |
| [ ] | Eliminar usuario en cascada (accesos, PDA, boletos, ventas, juegos); irreversible con confirmación |
| [ ] | No se conserva auditoría histórica de la eliminación |

### Supervisión

| Hecho | Ítem |
| --- | --- |
| [ ] | Observador lectura: vendedores, ventas, números, PDA, configuraciones, históricos, filtros, KPI |
| [ ] | Validación QR y recibo de venta |
| [ ] | No crear/modificar/eliminar/configurar/cobrar información operativa |
| [ ] | Excepción: chat con Administrador (cualquiera inicia) y registro de entrega si el caso está asignado |
| [ ] | Observador no atiende chats de vendedores |

### Hecho cuando (A3)

| Hecho | Ítem |
| --- | --- |
| [ ] | Solo admin altera usuarios, PDA, loterías, resultados, vigencia, horario, grupos y cambio de grupo |
| [ ] | Crear o restablecer deja `estadoValidado` true; contraseña definitiva → false |
| [ ] | Desbloquear: `estadoBloqueado` false, `estadoValidado` true, intentos 0 |
| [ ] | Restablecer y desbloquear solo si vendedor ACTIVO |
| [ ] | Usuario bloqueado por 3 intentos solo lo desbloquea admin |
| [ ] | Grupo se puede crear vacío; vendedor nuevo con grupo obligatorio |
| [ ] | Cambio de grupo mueve histórico completo |
| [ ] | KPI dinámicos por filtro de grupo; observador solo lectura |
| [ ] | Observador no crea apuestas ni modifica configuraciones |
| [ ] | Consultas con filtros definidos sin exponer datos de compradores |

---

# A4. Conectividad obligatoria y consulta de soporte

### Pérdida y recuperación de conexión

| Hecho | Ítem |
| --- | --- |
| [ ] | Sin conexión no se inician ni confirman ventas |
| [ ] | Si se pierde conexión al armar venta: conservar en memoria juegos, números, valores y loterías |
| [ ] | Aviso: debe conectarse al servidor para vender y continuar operando |
| [ ] | Sin conexión: no venta, boleto, código, clave, QR, impresión ni PDF |
| [ ] | Al reconectar: revalidar dispositivo, sesión y horario |
| [ ] | Confirmación idempotente: el servidor registra la operación una sola vez |

### Chat de soporte

| Hecho | Ítem |
| --- | --- |
| [ ] | Vendedor inicia chat de soporte con administrador y adjunta imágenes (galería, almacenamiento o cámara) |
| [ ] | Sin asignación por disponibilidad; cualquier vendedor se conecta con el administrador |
| [ ] | Administrador atiende, responde y descarga imágenes |
| [ ] | Observador no participa en chat de soporte del vendedor ni inicia chats con vendedores |
| [ ] | Observador no modifica operativa, no autoriza pagos, no crea configuraciones ni cierra conversaciones |
| [ ] | Administrador y observador pueden iniciar y responder chat entre sí |
| [ ] | Adjuntos originales del dispositivo no se eliminan al borrar contenido del sistema |

### Hecho cuando (A4)

| Hecho | Ítem |
| --- | --- |
| [ ] | Sin conexión informa que debe conectarse para vender y continuar |
| [ ] | Borrador solo en memoria hasta reconectar |
| [ ] | Confirmación posterior no crea ventas duplicadas |
| [ ] | Vendedor se conecta directo con admin, sin asignación |
| [ ] | Admin y observador chatean entre sí; observador no atiende vendedores |

---

# Modelo mínimo de información

| Hecho | Área | Entidades |
| --- | --- | --- |
| [ ] | Identidad | Usuarios (Activo/Inactivo, `estadoValidado`, `estadoBloqueado`), Roles, Permisos, UsuariosRoles, Sesiones, IntentosFallidos |
| [ ] | Grupos | Grupos (nombre, descripción), UsuariosGrupos |
| [ ] | Dispositivos | Dispositivos, DispositivosUsuarios |
| [ ] | Juego | Loterias, Ventas, Boletos, Juegos, JuegoLoteria, EstadosBoleto |
| [ ] | Seguridad de boleto | CodigoPublico (único), ClavesValidacionBoleto (hash), NumerosGanadores |
| [ ] | Operación | Configuraciones, Notificaciones, Sincronizaciones |
| [ ] | Soporte | Conversaciones, Mensajes, AdjuntosChat |
| [ ] | Restricciones | Índice único `CodigoPublico`; único `FechaJuego + LoteriaId`; venta transaccional |

---

# Reglas transversales de seguridad y calidad

| Hecho | Ítem |
| --- | --- |
| [ ] | HTTPS/TLS; contraseñas con hash seguro; autenticación, autorización y sesión |
| [ ] | Hora de operación del servidor, no solo reloj del PDA |
| [ ] | Ventas transaccionales y concurrencia de múltiples vendedores |
| [ ] | API permite escalar vendedores y dispositivos |
| [ ] | No se almacenan datos del comprador |
| [ ] | Código público es identificador legible; QR cifrado + servidor son la prueba de autenticidad |

---

# Cierre de cada apuesta

| Hecho | Ítem |
| --- | --- |
| [ ] | Apuesta terminada cuando está desplegada, recorridos mínimos pasan aceptación y no hay riesgos críticos abiertos |

---

# A5. Operación offline empresarial con códigos preasignados

### Módulo Ventas Offline

| Hecho | Ítem |
| --- | --- |
| [ ] | Módulo web **"Ventas Offline"** |
| [ ] | Generar y asignar códigos: usuarios activos, PDA, cantidad, destino; payload cifrado; estado **"Generado"**; consecutivo único inmutable |
| [ ] | Consultar códigos: listado con filtros usuario, PDA, fecha, estado y rango; ver consecutivo, fechas y estado |
| [ ] | Registrar por pistola/escáner el QR fotografiado; validar payload; botón **"Registrar"** |
| [ ] | Al registrar: venta oficial, estado **"Registrado"**, fecha/admin, descuenta saldo vendedor/PDA |
| [ ] | Si ya estaba registrado: **"QR ya registrado"** sin duplicar |

### Flujo de sincronización y uso en PDA

| Hecho | Ítem |
| --- | --- |
| [ ] | Al autenticar, consulta códigos asignados en estado **"Generado"** |
| [ ] | Descarga manual por defecto: aviso **"Tiene códigos offline listos para descargar"** |
| [ ] | Botones **"Descargar"**, **"Más tarde"** y **"Buscar y descargar códigos offline"** |
| [ ] | Si admin asigna con vendedor conectado, muestra la misma ventana de inmediato |
| [ ] | Vendedor puede activar sincronización automática en el PDA |
| [ ] | Base local Lite 3.000–5.000 códigos; estado **"Descargado"** y fecha de descarga |
| [ ] | Home muestra cantidad de códigos offline disponibles y se actualiza |
| [ ] | Códigos persisten al cerrar app, sesión o apagar PDA |
| [ ] | Solo se eliminan códigos ya utilizados cuya evidencia QR se envió al chat de admin |
| [ ] | Rechaza descargas que superen capacidad; notifica vendedor y administrador |
| [ ] | Sin conexión en venta: **"Conexión no disponible. ¿Desea continuar en modo offline?"** |
| [ ] | Consume código local, asigna payload a la jugada, genera QR sin reencifrar |
| [ ] | Mensaje foto: **"Por favor, tomar foto de este código vendido. GRACIAS."** y botón **"Listo"** obligatorio |
| [ ] | Guarda venta local, estado **"Utilizado"**, envía al chat admin: **"CÓDIGO VENDIDO OFFLINE — Favor registrar"** (permanente) |

### Línea de tiempo de un código

| Hecho | Estado / trazabilidad |
| --- | --- |
| [ ] | **Generado** — creado por administrador |
| [ ] | **Descargado** — sincronizado al PDA |
| [ ] | **Utilizado** — venta offline + evidencia al admin |
| [ ] | **Registrado** — validado y asociado a boleto/venta oficial |
| [ ] | Trazabilidad: quién generó, cuándo, usuario/PDA, descarga, venta, quién registró |

### Retorno a modo online

| Hecho | Ítem |
| --- | --- |
| [ ] | Al recuperar conexión: revalidar sesión, dispositivo y horario; pasa a online |
| [ ] | Ventas nuevas usan flujo normal (API genera código/QR); no consume offline |
| [ ] | Si reconexión falla, vuelve a offline si hay códigos disponibles |

### Modelo mínimo A5

| Hecho | Ítem |
| --- | --- |
| [ ] | Tabla `CodigosPreventaOffline` (consecutivo, usuario, dispositivo, payload, estados, fechas, admin, VentaId) |
| [ ] | Base Lite del PDA como réplica; SQL Server/API siguen siendo autoridad |

---

# A6. Proceso de validación y entrega de premios ganadores

### Flujo de validación de ticket ganador (vendedor)

| Hecho | Ítem |
| --- | --- |
| [ ] | Menú PDA **"Validar ticket ganador"** |
| [ ] | Código recibo (`AOL-` + 7 dígitos) o escaneo QR; muestra tirilla y estado |
| [ ] | Si listo para cobrar: foto del QR y **"Reportar caso"** |
| [ ] | Al reportar: caso **"Reportado"**, guarda imagen, notifica administrador |
| [ ] | Si no está listo: muestra motivo y no registra caso |

### Etapa administrativa

| Hecho | Ítem |
| --- | --- |
| [ ] | Web **"Casos de premios ganadores"** con casos **"Reportado"** y fotografía |
| [ ] | Validación manual de la foto vs ticket; válido → **"Validado"** y habilita asignación |
| [ ] | Rechazo → **"Rechazado"** permanente |
| [ ] | Desde **"Validado"**: seleccionar observador activo → **"Asignado"** y notificación al PDA |

### Etapa de registro por observador

| Hecho | Ítem |
| --- | --- |
| [ ] | PDA **"Registrar ganador"**: casos **"Asignado"** o **"En proceso"** |
| [ ] | Nombre completo del ganador (obligatorio) |
| [ ] | Apellido del ganador (obligatorio) |
| [ ] | Número de contacto / celular (obligatorio) |
| [ ] | Lugar donde ganó (texto abierto, obligatorio) |
| [ ] | Nombre del vendedor (autocompleta, obligatorio) |
| [ ] | Valor total ganado (ingreso manual; el sistema no calcula) |
| [ ] | Persona que entrega (autofill observador autenticado, no editable) |
| [ ] | Estado **"En proceso"** al comenzar el registro |
| [ ] | Foto 1: ticket ganador con QR visible |
| [ ] | Foto 2: ganador sosteniendo el ticket |
| [ ] | Foto 3: cédula de identidad del ganador |
| [ ] | Fotos sin OCR ni extracción automática de cédula |
| [ ] | **"Registrar entrega del premio"** solo con todos los campos y 3 fotos |
| [ ] | Al confirmar: estado **"Registrado"**, fecha/observador, boleto **"Premio entregado"**, comprobante visualizable |

### Estados del caso y cierre permanente

| Hecho | Ítem |
| --- | --- |
| [ ] | Cadena: Reportado → Validado → Asignado → En proceso → Registrado → Premio entregado |
| [ ] | **"Premio entregado"** irreversible |
| [ ] | Reescaneo muestra: **"Este ticket ya fue registrado y el premio ya fue entregado."** |
| [ ] | Sin edición de datos; bloquea reclamación múltiple; queda en lectura para auditoría |

### Modelo mínimo A6

| Hecho | Ítem |
| --- | --- |
| [ ] | Tabla `CasosGanadores` |
| [ ] | Tabla `EntregasGanadores` |
| [ ] | Tabla `EvidenciasGanador` (TicketConQR, GanadorConTicket, CedulaIdentidad) |
| [ ] | `Boletos`: `EstadoDelPremio` (Vigente, PremioEntregado, Rechazado) y `FechaEntregaPremio` |

---

# Anexo normativo — I. Perfil Administrador

| Hecho | Código | Requisito |
| --- | --- | --- |
| [ ] | RS-001 | 1. INFORMACIÓN GENERAL — proyecto New Rich, objetivo y tecnologías |
| [ ] | RS-002 | 2. DESCRIPCIÓN GENERAL DEL SISTEMA — Android vendedor, Android observador, Web administrador |
| [ ] | RS-003 | 3. ARQUITECTURA GENERAL — SQL Server central, API .NET, tres clientes |
| [ ] | RS-004 | 4. PERFILES — Vendedor, Observador, Administrador |
| [ ] | RS-005 | 5. SOLICITUD PARA SER VENDEDOR — formulario externo Google Forms (RF-001) |
| [ ] | RS-006 | 6. CREACIÓN DEL VENDEDOR — alta manual, datos, contraseña temporal en pantalla (RF-002, RF-003) |
| [ ] | RS-007 | 7. GESTIÓN Y AUTENTICACIÓN DEL PDA — PDA instalado, asociación, primer login, validación de dispositivo (RF-004 a RF-007) |
| [ ] | RS-008 | 8. HORARIO DE OPERACIÓN — hora de cierre, bloqueo, venta en curso (RF-008 a RF-010) |
| [ ] | RS-009 | 48. PERFIL ADMINISTRADOR — web MVC .NET sobre IIS (RF-058) |
| [ ] | RS-010 | 49. LOGIN ADMINISTRADOR — usuario y contraseña (RF-059) |
| [ ] | RS-011 | 50. ADMINISTRACIÓN DE NÚMEROS GANADORES — número, lotería, fecha (RF-060) |
| [ ] | RS-012 | 51. MÚLTIPLES NÚMEROS GANADORES — misma fecha, distintas loterías (RF-061) |
| [ ] | RS-013 | 52. RESTRICCIÓN DE NÚMERO GANADOR — UNIQUE (FechaJuego, LoteriaId) (RF-062) |
| [ ] | RS-014 | 53. ADMINISTRACIÓN DE USUARIOS — crear, modificar, activar, desactivar, eliminar, asociar PDA (RF-063) |
| [ ] | RS-015 | 54. ADMINISTRACIÓN DE PDA — registrar, asociar, desasociar, activar, desactivar, consultar (RF-064) |
| [ ] | RS-016 | 55. ADMINISTRACIÓN DE LOTERÍAS — crear, modificar, activar, desactivar, eliminar (RF-065) |
| [ ] | RS-017 | 56. CONFIGURACIONES VARIABLES — cierre, vigencia, loterías, notificaciones, máximos y alertas (RF-066) |
| [ ] | RS-018 | 57. NOTIFICACIÓN POR REPETICIÓN DE NÚMERO (RF-067) |
| [ ] | RS-019 | 58. NOTIFICACIÓN POR VALOR ALTO (RF-068) |
| [ ] | RS-020 | 59. CENTRO DE NOTIFICACIONES — módulo + campana con contador (RF-069) |
| [ ] | RS-021 | 60. BÚSQUEDA ADMINISTRATIVA — por número, vendedor, boleto, lotería (RF-070) |
| [ ] | RS-022 | 61. VISUALIZACIÓN DE BOLETO — tirilla equivalente (RF-071) |
| [ ] | RS-023 | 62. KPI ADMINISTRATIVOS — usuarios, ventas, números; observador solo lectura (RF-072) |
| [ ] | RS-024 | 63. KPI POR VENDEDOR — ventas, total, boletos, números, loterías (RF-073) |
| [ ] | RS-025 | 64. ELIMINACIÓN DE USUARIOS — solo admin, con búsqueda previa (RF-074) |
| [ ] | RS-026 | 65. CONFIRMACIÓN DE ELIMINACIÓN — modal Sí/No con advertencia de pérdida (RF-075) |
| [ ] | RS-027 | 66. ELIMINACIÓN EN CASCADA — accesos, PDA, boletos, ventas, juegos, relaciones (RF-076) |
| [ ] | RS-028 | 72. SEGURIDAD — autenticación, roles, sesiones, TLS, hash, QR, duplicidad |
| [ ] | RS-029 | 73. SEGURIDAD DEL PDA — usuario + contraseña + PDA autorizado/asociado + activo + horario |
| [ ] | RS-030 | 74. CONTROL DE HORARIO EN EL PDA — hora del servidor, no reloj local |
| [ ] | RS-031 | 75. REQUERIMIENTOS NO FUNCIONALES — rendimiento, disponibilidad, integridad, concurrencia, seguridad, escalabilidad, SQL Server |
| [ ] | RS-032 | 76. MATRIZ DE PERMISOS — tres perfiles |
| [ ] | RS-033 | 77. REGLAS DE NEGOCIO — RN-001 a RN-050 |
| [ ] | RS-034 | 79. FLUJO DE CIERRE DE JORNADA |
| [ ] | RS-035 | 81. FLUJO DE NÚMEROS GANADORES |
| [ ] | RS-036 | 82. MODELO CONCEPTUAL DE INFORMACIÓN |
| [ ] | RS-037 | 83. CRITERIOS GENERALES DE ACEPTACIÓN |
| [ ] | RS-038 | 84. DEFINICIÓN DE LO QUE EL SISTEMA HACE Y NO HACE |
| [ ] | RS-039 | 85. REQUERIMIENTOS EXPLÍCITAMENTE APROBADOS |
| [ ] | RS-040 | 86. APROBACIÓN DEL DOCUMENTO |

### RS-031 — no funcionales (detalle)

| Hecho | Código | Ítem |
| --- | --- | --- |
| [ ] | RNF-001 | Rendimiento adecuado para venta rápida |
| [ ] | RNF-002 | Disponibilidad en horarios operativos |
| [ ] | RNF-003 | Ventas transaccionales |
| [ ] | RNF-004 | Concurrencia de múltiples vendedores |
| [ ] | RNF-005 | Comunicación HTTPS/TLS |
| [ ] | RNF-006 | Escalabilidad de vendedores y dispositivos |
| [ ] | RNF-007 | Base de datos centralizada SQL Server |

### RS-032 — matriz de permisos (detalle)

| Hecho | Funcionalidad |
| --- | --- |
| [ ] | Iniciar sesión (los tres perfiles) |
| [ ] | Restablecer contraseña / desbloquear usuario (solo admin) |
| [ ] | Crear apuesta / imprimir boleto / generar PDF (solo vendedor) |
| [ ] | Consultar propias ventas (vendedor y admin) |
| [ ] | Consultar ventas (observador y admin) |
| [ ] | Consultar históricos (los tres) |
| [ ] | Validar QR y recibo (observador y admin) |
| [ ] | Ver vendedores (observador y admin) |
| [ ] | Crear / modificar / eliminar usuarios (solo admin) |
| [ ] | Administrar PDA y loterías (admin; observador lectura) |
| [ ] | Registrar números ganadores / horario / vigencia / notificaciones (solo admin) |
| [ ] | Ver KPI (observador y admin) |
| [ ] | Chat: vendedor con admin; observador con admin; admin con ambos |

### RS-033 — reglas de negocio (detalle)

| Hecho | Código | Regla |
| --- | --- | --- |
| [ ] | RN-001 | Un vendedor solo opera desde un PDA autorizado |
| [ ] | RN-002 | Primer acceso con contraseña temporal |
| [ ] | RN-003 | Debe cambiar la contraseña en el primer acceso |
| [ ] | RN-004 | Tipo de boleto inmutable COMBINADA o INDIVIDUAL; no mezclar |
| [ ] | RN-005 | COMBINADA: número, valor, múltiples loterías; INDIVIDUAL: líneas con una lotería |
| [ ] | RN-006 | COMBINADA: total = valor × loterías; INDIVIDUAL: suma de valores |
| [ ] | RN-007 | Admin configura máximos; INDIVIDUAL no supera 6 |
| [ ] | RN-008 | Al máximo, Agregar se desactiva y la API rechaza excedentes |
| [ ] | RN-009 | Código público único de 7 dígitos, ceros a la izquierda, unicidad en BD |
| [ ] | RN-010 | QR con clave de validación en payload cifrado/autenticado |
| [ ] | RN-011 | Cliente paga al confirmar JUGAR |
| [ ] | RN-012 | No almacenar información personal del comprador |
| [ ] | RN-013 | Boleto se paga al portador |
| [ ] | RN-014 | El sistema no calcula el valor del premio |
| [ ] | RN-015 | El vendedor informa el valor del premio al comprador |
| [ ] | RN-016 | Ganador = coincidencia número + lotería + fecha |
| [ ] | RN-017 | Boleto ganador dentro de vigencia |
| [ ] | RN-018 | QR auténtico y del sistema |
| [ ] | RN-019 | Boleto debe existir en BD |
| [ ] | RN-020 | Ganador cobrado muestra PAGADO |
| [ ] | RN-021 | Ganador pendiente muestra GANADOR |
| [ ] | RN-022 | Vigencia inicial 30 días calendario |
| [ ] | RN-023 | Admin puede modificar la vigencia |
| [ ] | RN-024 | Una fecha puede tener múltiples números ganadores |
| [ ] | RN-025 | Una fecha + lotería = un solo número ganador |
| [ ] | RN-026 | El mismo número puede ganar en distintas loterías |
| [ ] | RN-027 | Admin configura hora de cierre |
| [ ] | RN-028 | Después del cierre no hay nuevas apuestas |
| [ ] | RN-029 | Venta en curso puede finalizar |
| [ ] | RN-030 | Tras esa venta, el vendedor se desconecta |
| [ ] | RN-031 | PDA requiere conexión para crear/confirmar ventas (sin offline no autorizado) |
| [ ] | RN-032 | Borrador en memoria si falla conexión; confirmar solo tras reconectar y revalidar |
| [ ] | RN-033 | Servidor es fuente definitiva |
| [ ] | RN-034 | Si falla impresión, guardar como PDF |
| [ ] | RN-035 | PDF se puede enviar manualmente al cliente |
| [ ] | RN-036 | Vendedor registra soporte directo con administrador |
| [ ] | RN-037 | Observador no atiende chats de vendedores; chat solo con admin |
| [ ] | RN-038 | Admin atiende vendedores y chatea con observadores |
| [ ] | RN-039 | Cerrar conversación / eliminar historial solo con permiso de modificación |
| [ ] | RN-040 | No eliminar imágenes originales del dispositivo |
| [ ] | RN-041 | Admin puede eliminar usuarios y datos relacionados |
| [ ] | RN-042 | El sistema no almacena el valor monetario del premio ganado (salvo registro manual de entrega en A6) |
| [ ] | RN-043 | Resultado ganador por número + lotería + fecha |
| [ ] | RN-044 | 3 contraseñas incorrectas → `estadoBloqueado`; solo admin desbloquea |
| [ ] | RN-045 | Contador de intentos se resetea con contraseña correcta, desbloqueo o restablecimiento |
| [ ] | RN-046 | Solo vendedores pertenecen a grupos |
| [ ] | RN-047 | Grupo obligatorio al crear vendedor; un solo grupo; admin crea grupos |
| [ ] | RN-048 | Cambio de grupo mueve histórico y datos asociados |
| [ ] | RN-049 | KPI dinámicos por general o grupo; observador solo consulta |
| [ ] | RN-050 | Descarga KPI en PDF con filtros aplicados |

---

# Anexo normativo — II. Perfil Observador

| Hecho | Código | Requisito |
| --- | --- | --- |
| [ ] | RS-041 | 30. PERFIL OBSERVADOR — Android, solo lectura, validar QR/recibo, KPI lectura, chat con admin (RF-040) |
| [ ] | RS-042 | 31. HOME OBSERVADOR — número ganador, lotería, fecha, info operativa (RF-041) |
| [ ] | RS-043 | 32. CONSULTA DE VENDEDORES — nombre, alias, estado, PDA (RF-042) |
| [ ] | RS-044 | 33. CONSULTA DE VENTAS — filtros vendedor, fechas, número, lotería (RF-043) |
| [ ] | RS-045 | 34. CONSULTA DE NÚMEROS — número, valor, vendedor, fecha, loterías, estado (RF-044) |
| [ ] | RS-046 | 35. CONSULTA DE DISPOSITIVOS — PDA conectados/desconectados, usuario, estado (RF-045) |
| [ ] | RS-047 | 36. CONSULTA DE CONFIGURACIONES — solo lectura (RF-046) |
| [ ] | RS-048 | 37. VALIDACIÓN DE BOLETOS MEDIANTE QR — escanear, cámara, autenticidad, consultar (RF-047) |
| [ ] | RS-048A | 37A. VALIDACIÓN POR RECIBO DE VENTA — `AOL-` + 7 dígitos o 7 dígitos; mismos mensajes; solo lectura (RF-047A) |
| [ ] | RS-049 | 38. BOLETO INEXISTENTE — `NO SE ENCONTRÓ INFORMACIÓN` (RF-048) |
| [ ] | RS-050 | 39. BOLETO EXISTENTE — código, vendedor, fecha/hora, juegos, números, valores, loterías, total, estado, resultado, vigencia (RF-049) |
| [ ] | RS-051 | 40. VALIDACIONES DEL BOLETO — existencia, QR, número, lotería, fecha, resultado, vigencia, cobro (RF-050) |
| [ ] | RS-052 | 41. FILTROS DE BOLETOS — por jugar, jugados, ganadores, no ganadores, vencidos, pagados (RF-051) |
| [ ] | RS-053 | 42. CHAT INTERNO — vendedor inicia con admin; admin ↔ observador (RF-052) |
| [ ] | RS-054 | 43. REGLAS DEL CHAT — canales y restricciones por perfil (RF-053) |
| [ ] | RS-055 | 44. CONEXIÓN DIRECTA DE SOPORTE — sin asignación por disponibilidad (RF-054) |
| [ ] | RS-056 | 45. ENVÍO DE IMÁGENES — galería, almacenamiento, cámara (RF-055) |
| [ ] | RS-057 | 46. ELIMINACIÓN DEL HISTORIAL DEL CHAT — observador no cierra ni elimina (RF-056) |
| [ ] | RS-058 | 47. DESCARGA DE IMÁGENES — admin descarga; nombre `YYYY-MM-DD_HH-MM-SS` (RF-057) |
| [ ] | RS-059 | 80. FLUJO DE VALIDACIÓN DEL BOLETO — QR o recibo; admin único que pasa a PAGADO |

---

# Anexo normativo — III. Perfil Vendedor

| Hecho | Código | Requisito |
| --- | --- | --- |
| [ ] | RS-060 | 9. PERFIL VENDEDOR — login usuario, contraseña, validación de dispositivo (RF-011) |
| [ ] | RS-061 | 10. HOME DEL VENDEDOR — total del día, Juego nuevo, menú, estado de sesión (RF-012) |
| [ ] | RS-062 | 11. CREACIÓN DE JUEGOS — COMBINADA/INDIVIDUAL, número, valor, loterías, máximos (RF-013 a RF-016) |
| [ ] | RS-063 | 12. MÚLTIPLES JUEGOS POR BOLETO — máximo configurado; total = suma de juegos (RF-017, RF-018) |
| [ ] | RS-064 | 13. MODIFICACIÓN DEL BOLETO — agregar, eliminar con modal, cancelar boleto (RF-019 a RF-021) |
| [ ] | RS-065 | 14. CONFIRMACIÓN DE LA VENTA — JUGAR, modal total, pagado al portador, sin datos del comprador (RF-022, RF-023) |
| [ ] | RS-066 | 15. REGISTRO DE LA VENTA — IDs, vendedor, fecha/hora, juegos, total, estado, sincronización (RF-024) |
| [ ] | RS-067 | 16. ESTADOS DEL BOLETO — Por jugar, Jugado, Ganador, No ganador, Vencido, Pagado/cobrado |
| [ ] | RS-068 | 17. IDENTIFICADOR DEL BOLETO — GUID interno + código público 7 dígitos único (RF-025) |
| [ ] | RS-069 | 18. CÓDIGO QR — payload cifrado AES-GCM, hash de clave, validación en servidor (RF-026) |
| [ ] | RS-070 | 19. IMPRESIÓN DEL BOLETO — automática en térmica integrada (RF-027) |
| [ ] | RS-071 | 20. FORMATO DEL BOLETO — COMBINADO e INDIVIDUAL, leyenda al portador y vigencia |
| [ ] | RS-072 | 21. FALLA DE IMPRESIÓN — venta persiste, boleto local, PDF, envío manual (RF-028) |
| [ ] | RS-073 | 22. FINALIZACIÓN DE LA VENTA — registro, códigos, QR, impresión/PDF, total del día, Home (RF-029) |
| [ ] | RS-074 | 23. HISTÓRICO DEL VENDEDOR — calendario, máximo 10 días atrás, solo lectura (RF-030 a RF-032) |
| [ ] | RS-075 | 24. RESULTADOS Y NÚMEROS GANADORES — consulta y comparación (RF-033) |
| [ ] | RS-076 | 25. DEFINICIÓN DE BOLETO GANADOR — número + lotería + fecha + vigencia + QR + no cobrado (RF-034) |
| [ ] | RS-077 | 26. CÁLCULO DEL PREMIO — el sistema NO calcula premios (RF-035) |
| [ ] | RS-078 | 27. COBRO DEL PREMIO — admin autoriza PAGADO; mensajes GANADOR/PAGADO/NO GANADOR/VENCIDO (RF-036, RF-037) |
| [ ] | RS-079 | 28. VIGENCIA DEL PREMIO — configurable, inicial 30 días (RF-038) |
| [ ] | RS-080 | 29. MÚLTIPLES LOTERÍAS POR JUEGO — validación por Número + Lotería + Fecha (RF-039) |
| [ ] | RS-081 | 67. DISPONIBILIDAD DE CONEXIÓN PARA VENTAS — no offline no autorizado; aviso de reconexión (RF-077) |
| [ ] | RS-082 | 68. PÉRDIDA DE CONEXIÓN DURANTE UNA VENTA — borrador en memoria; no confirmar sin red |
| [ ] | RS-083 | 69. REANUDACIÓN DESPUÉS DE RECUPERAR CONEXIÓN — revalidar y confirmar una sola vez (RF-078) |
| [ ] | RS-084 | 70. CONTROL DE DUPLICIDAD DE CONFIRMACIÓN — id de intento e idempotencia (RF-079) |
| [ ] | RS-085 | 71. SERVIDOR COMO FUENTE DEFINITIVA — PDA no registra ventas de forma autónoma (RF-080) |
| [ ] | RS-086 | 78. FLUJO COMPLETO DE UNA VENTA |

---

# Anexo normativo — IV. Tirilla de impresión

| Hecho | Código | Requisito |
| --- | --- | --- |
| [ ] | RS-087 | 87. TIRILLA DE IMPRESIÓN — formato COMBINADO e INDIVIDUAL, `RECIBO DE VENTA AOL-`, QR, leyenda al portador y vigencia 30 días |
