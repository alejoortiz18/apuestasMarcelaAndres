# New Rich
## Diagrama funcional del sistema

> Documento visual para revisión del cliente
>
> Versión: 1.0
> Fecha: 2026-09-03

---

## 1. Propósito

Este documento representa los flujos principales de **New Rich**, incluyendo las operaciones del Administrador, Vendedor y Observador.

La API .NET y SQL Server son la autoridad central del sistema. El PDA puede utilizar una base de datos local tipo Lite para operar con códigos offline preasignados, pero esa base local no reemplaza al servidor.

---

## 2. Actores

| Actor | Responsabilidades principales |
| --- | --- |
| Administrador | Configura la operación, usuarios, PDA, loterías, códigos offline, resultados, premios, reportes y permisos. |
| Vendedor | Crea y vende apuestas, imprime boletos, consulta resultados y puede operar offline con códigos preasignados. |
| Observador | Consulta la operación, valida boletos y registra ganadores y entregas de premios. |
| API / Servidor | Autentica, valida, registra, cifra, controla estados, duplicidad, límites y trazabilidad. |
| PDA vendedor | Ejecuta la aplicación del vendedor y almacena códigos offline en una base Lite local. |
| PDA observador | Ejecuta la consulta, validación y registro de entregas. |

---

## 3. Arquitectura general

```mermaid
flowchart TB
    A[Administrador<br/>Web MVC] <--> API[API .NET<br/>Autoridad central]
    V[Vendedor<br/>PDA Android] <--> API
    O[Observador<br/>PDA Android] <--> API
    API <--> DB[(SQL Server)]
    V <--> L[(Base local Lite<br/>3.000 a 5.000 códigos)]
```

---

## 4. Acceso y autenticación

```mermaid
flowchart TD
    A[Usuario abre la aplicación] --> B[Ingresa usuario y contraseña]
    B --> C{¿Usuario válido y activo?}
    C -->|No| X[Rechazar acceso y mostrar motivo]
    C -->|Sí| D{¿PDA registrado, activo y asociado?}
    D -->|No| X
    D -->|Sí| E{¿Usuario bloqueado?}
    E -->|Sí| Y[Impedir acceso hasta desbloqueo administrativo]
    E -->|No| F{¿Contraseña correcta?}
    F -->|No| G[Registrar intento fallido]
    G --> H{¿Tres intentos consecutivos?}
    H -->|Sí| Y
    H -->|No| X
    F -->|Sí| I{¿estadoValidado = true?}
    I -->|Sí| J[Solicitar cambio obligatorio de contraseña]
    J --> K[Guardar contraseña definitiva]
    K --> L[estadoValidado = false]
    I -->|No| M[Validar horario y sesión]
    L --> M
    M --> N{¿Horario permitido?}
    N -->|No| Z[Mostrar: LOS JUEGOS ESTÁN CERRADOS]
    N -->|Sí| O[Crear sesión y cargar Home]
```

---

## 5. Administración de usuarios, grupos y PDA

```mermaid
flowchart TD
    A[Administrador abre Usuarios] --> B{¿Acción?}
    B -->|Crear vendedor| C[Registrar datos y seleccionar grupo]
    C --> D[Asignar PDA autorizado]
    D --> E[Crear contraseña temporal]
    E --> F[estadoValidado = true]
    B -->|Modificar vendedor| G[Actualizar datos permitidos]
    B -->|Cambiar grupo| H[Reemplazar grupo y conservar relaciones]
    B -->|Restablecer contraseña| I{¿Vendedor activo?}
    I -->|No| X[Rechazar operación]
    I -->|Sí| J[Invalida sesiones abiertas]
    J --> K[Generar contraseña temporal]
    K --> L[estadoValidado = true]
    B -->|Desbloquear| M[estadoBloqueado = false]
    B -->|Eliminar| N[Solicitar confirmación explícita]
    N --> O[Eliminar usuario y datos relacionados]
```

---

## 6. Configuración operativa

```mermaid
flowchart TD
    A[Administrador abre Configuración] --> B{¿Qué desea configurar?}
    B -->|Loterías| C[Crear, modificar, activar o desactivar loterías]
    B -->|Horario| D[Definir hora de cierre]
    B -->|Vigencia| E[Definir días para reclamar premios]
    B -->|Alertas| F[Configurar repetición de número y valor mínimo]
    B -->|Tipos de apuesta| G[Definir límites COMBINADO e INDIVIDUAL]
    B -->|Sincronización offline| H[Definir modo Manual o Automático por PDA]
    C --> I[Guardar configuración en SQL Server]
    D --> I
    E --> I
    F --> I
    G --> I
    H --> I
    I --> J[API publica configuración vigente]
```

---

## 7. Inicio del vendedor y códigos disponibles

```mermaid
flowchart TD
    A[Autenticación positiva] --> B[Cargar Home vendedor]
    B --> C[Consultar saldo de códigos offline]
    C --> D[Mostrar códigos disponibles: Generado + Descargado]
    D --> E{¿Hay códigos pendientes por descargar?}
    E -->|No| F[Mostrar Home normalmente]
    E -->|Sí| G{¿Modo de sincronización?}
    G -->|Manual| H[Mostrar ventana de códigos listos]
    H --> I[Descargar]
    H --> J[Más tarde]
    H --> K[Buscar y descargar códigos offline]
    K --> L[Consultar API e intentar descarga]
    G -->|Automático| M[Descargar sin mostrar ventana]
    I --> N[Guardar en base Lite y actualizar contador]
    L --> N
    M --> N
    J --> O[Conservar códigos pendientes y contador]
```

**Mensaje de descarga manual:** `Tiene códigos offline listos para descargar`.

**Configuración:** el vendedor define Manual o Automático directamente en su PDA.

---

## 8. Creación de una apuesta

```mermaid
flowchart TD
    A[Vendedor pulsa JUEGO NUEVO] --> B[Seleccionar tipo de apuesta]
    B --> C{¿Tipo seleccionado?}
    C -->|COMBINADO| D[Un número + un valor + múltiples loterías]
    C -->|INDIVIDUAL| E[Una línea: número + valor + una lotería]
    D --> F[Agregar juego]
    E --> G[Agregar línea y mostrar tabla]
    G --> H{¿Editar o quitar línea?}
    H -->|Editar| I[Actualizar línea y recalcular total]
    H -->|Quitar| J[Confirmar eliminación y recalcular total]
    H -->|Continuar| K[Revisar boleto]
    F --> K
    I --> K
    J --> K
    K --> L{¿Máximo alcanzado?}
    L -->|Sí| M[Desactivar botón Agregar]
    L -->|No| N[Permitir nueva jugada]
    M --> O[JUGAR]
    N --> O
```

### Reglas de estructura

- **COMBINADO:** un boleto contiene un solo juego por defecto. El administrador puede configurar otro máximo.
- **INDIVIDUAL:** cada línea utiliza una sola lotería y permite hasta 6 líneas como máximo.
- No se pueden mezclar tipos en el mismo boleto.
- Las loterías mostradas son las habilitadas por el administrador para la fecha.

---

## 9. Cálculo y confirmación de la venta

```mermaid
flowchart TD
    A[Boleto revisado] --> B[Calcular total]
    B --> C{¿Tipo?}
    C -->|COMBINADO| D[Total = valor x cantidad de loterías]
    C -->|INDIVIDUAL| E[Total = suma de valores de líneas]
    D --> F[Mostrar total y opciones Aceptar / Cancelar]
    E --> F
    F --> G{¿Vendedor confirma JUGAR?}
    G -->|No| H[Cancelar o continuar editando]
    G -->|Sí| I{¿Hay conexión?}
    I -->|No| J[Conservar borrador y solicitar reconexión]
    I -->|Sí| K[API valida e inicia transacción]
    K --> L{¿Confirmación duplicada?}
    L -->|Sí| M[Devolver resultado idempotente]
    L -->|No| N[Registrar venta y boleto]
    N --> O[Generar código, clave y QR]
```

---

## 10. Emisión, impresión y contingencia

```mermaid
flowchart TD
    A[Venta confirmada] --> B[Generar boleto seguro]
    B --> C[Generar código público de 7 dígitos]
    C --> D[Generar clave de validación y guardar hash]
    D --> E[Crear QR cifrado y autenticado]
    E --> F{¿Impresora disponible?}
    F -->|Sí| G[Imprimir tirilla]
    F -->|No| H[Conservar venta registrada]
    H --> I[Generar PDF de contingencia]
    G --> J[Mostrar boleto y volver a Home]
    I --> J
```

### Formatos de tirilla

```mermaid
flowchart LR
    A[Tipo COMBINADO] --> B[Un juego<br/>Número + valor + múltiples loterías<br/>Total = valor x loterías]
    C[Tipo INDIVIDUAL] --> D[Hasta 6 líneas<br/>Número + valor + una lotería<br/>Total = suma de valores]
```

Ambos formatos incluyen código `AOL-1234567`, fecha, hora, tipo de apuesta, estado, total, QR, leyenda de pago al portador y vigencia.

---

## 11. Consulta de resultados y validación QR

```mermaid
flowchart TD
    A[Administrador registra resultado] --> B[Seleccionar fecha, lotería y número ganador]
    B --> C{¿Ya existe resultado para fecha + lotería?}
    C -->|Sí| D[Rechazar duplicado]
    C -->|No| E[Guardar resultado]
    F[Observador escanea QR] --> G[Servidor verifica cifrado y autenticidad]
    G --> H{¿QR y boleto válidos?}
    H -->|No| I[Mostrar: NO SE ENCONTRÓ INFORMACIÓN]
    H -->|Sí| J[Validar número, lotería, fecha y vigencia]
    J --> K{¿Resultado coincide?}
    K -->|No| L[Mostrar: NO GANADOR]
    K -->|Sí| M{¿Boleto cobrado o entregado?}
    M -->|Cobrado| N[Mostrar: PAGADO]
    M -->|Entregado| O[Mostrar ticket ya registrado]
    M -->|Pendiente| P[Mostrar: GANADOR]
```

---

## 12. Operación online y pérdida de conexión

```mermaid
flowchart TD
    A[Vendedor prepara boleto] --> B{¿Conexión disponible?}
    B -->|Sí| C[Permitir JUGAR y confirmar en API]
    B -->|No| D{¿Hay códigos offline válidos?}
    D -->|No| E[Conservar borrador en memoria]
    E --> F[Mostrar aviso de reconexión]
    F --> G[Revalidar al recuperar conexión]
    G --> C
    D -->|Sí| H[Preguntar si desea modo offline]
    H -->|No| E
    H -->|Sí| I[Usar código offline local]
    I --> J[Crear QR desde payload precifrado]
    J --> K[Mostrar QR y solicitar evidencia]
```

---

## 13. Venta offline con código preasignado

```mermaid
flowchart TD
    A[PDA sin conexión] --> B{¿Código disponible y válido?}
    B -->|No| C[Informar que debe conectarse]
    B -->|Sí| D[Crear apuesta normal]
    D --> E[Consumir código local]
    E --> F[Asignar payload a número, valor y loterías]
    F --> G[Mostrar QR generado desde payload]
    G --> H[Tomar foto del QR]
    H --> I[Botón obligatorio Listo]
    I --> J[Guardar venta y estado Utilizado]
    J --> K[Enviar evidencia al chat del administrador]
    K --> L[Actualizar contador local disponible]
```

---

## 14. Registro administrativo de códigos offline

```mermaid
flowchart TD
    A[Administrador abre Ventas Offline] --> B[Conectar lector o seleccionar QR recibido]
    B --> C[Escanear QR enviado por vendedor]
    C --> D[Validar autenticidad e integridad]
    D --> E{¿Código ya registrado?}
    E -->|Sí| F[Mostrar: QR ya registrado]
    E -->|No| G[Mostrar datos de venta, vendedor y PDA]
    G --> H{¿Administrador confirma Registrar?}
    H -->|No| I[Cancelar operación]
    H -->|Sí| J[Crear venta y boleto oficial]
    J --> K[Estado del código = Registrado]
    K --> L[Registrar administrador, fecha y VentaId]
    L --> M[Descontar código del saldo del vendedor y PDA]
    M --> N[Actualizar backend y SQL Server]
```

---

## 15. Sincronización y persistencia de códigos offline

```mermaid
flowchart TD
    A[PDA se conecta] --> B[Autenticar usuario, PDA y horario]
    B --> C{¿Autenticación positiva?}
    C -->|No| D[No sincronizar]
    C -->|Sí| E[Consultar códigos Generados asignados]
    E --> F{¿Modo Manual?}
    F -->|Sí| G[Mostrar ventana y esperar acción]
    F -->|No, Automático| H[Descargar sin mostrar ventana]
    G --> I[Descargar o buscar y descargar]
    H --> I
    I --> J{¿Capacidad Lite disponible?}
    J -->|No| K[Rechazar excedente y notificar]
    J -->|Sí| L[Guardar payload cifrado, estado y metadatos]
    L --> M[Marcar Descargado en servidor]
    M --> N[Actualizar contador del Home]
    O[Administrador asigna códigos en línea] --> P[Servidor notifica al PDA]
    P --> F
```

### Persistencia local

```mermaid
flowchart LR
    A[Código no utilizado] --> B[Permanece en BD Lite]
    B --> C[Cerrar aplicación]
    B --> D[Cerrar sesión]
    B --> E[Apagar PDA]
    C --> B
    D --> B
    E --> B
    F[Código Utilizado + QR enviado] --> G[Puede eliminarse localmente]
    G --> H[Actualizar contador]
```

La base Lite almacena entre 3.000 y 5.000 códigos según la capacidad configurada. Los códigos disponibles se calculan como:

`Generado + Descargado`

Los estados `Utilizado` y `Registrado` no forman parte del saldo disponible.

---

## 16. Control de horario offline

```mermaid
flowchart TD
    A[PDA descarga hora de cierre del servidor] --> B[Guardar timestamp y versión]
    B --> C[Operar online u offline]
    C --> D{¿Llegó la hora sincronizada?}
    D -->|No| C
    D -->|Sí| E[Cerrar sesión automáticamente]
    E --> F[Bloquear nuevas apuestas]
    F --> G[Mostrar: LOS JUEGOS ESTÁN CERRADOS]
    C --> H{¿Hora local fue manipulada?}
    H -->|Sí| I[Registrar anomalía y cerrar sesión]
    H -->|No| C
```

---

## 17. Flujo de validación y entrega de premios

```mermaid
flowchart TD
    A[Vendedor escanea ticket ganador] --> B[Prevalidar QR, boleto, vigencia y resultado]
    B --> C{¿Prevalidación correcta?}
    C -->|No| D[Mostrar motivo y no crear caso]
    C -->|Sí| E[Crear CasoGanador = Reportado]
    E --> F[Notificar al administrador]
    F --> G[Administrador solicita y valida foto del ticket]
    G --> H{¿Foto válida?}
    H -->|No| I[Caso = Rechazado]
    H -->|Sí| J[Caso = Validado]
    J --> K[Administrador asigna observador]
    K --> L[Caso = Asignado]
    L --> M[Observador abre Registrar ganador]
    M --> N[Caso = En proceso]
    N --> O[Ingresar datos del ganador y valor manual]
    O --> P[Capturar 3 fotografías obligatorias]
    P --> Q{¿Datos y fotos completos?}
    Q -->|No| R[Mantener botón deshabilitado]
    Q -->|Sí| S[Confirmar Registrar entrega del premio]
    S --> T[Crear EntregaGanador]
    T --> U[Caso = Registrado]
    U --> V[Boleto = Premio entregado]
    V --> W[Bloquear nuevas reclamaciones]
```

---

## 18. Estados de un código offline

```mermaid
stateDiagram-v2
    [*] --> Generado: Administrador crea y asigna
    Generado --> Descargado: PDA descarga
    Descargado --> Utilizado: Vendedor usa en venta offline
    Utilizado --> Registrado: Administrador valida QR y registra
    Registrado --> [*]
```

## 19. Estados de un caso ganador

```mermaid
stateDiagram-v2
    [*] --> Reportado: Prevalidación del vendedor
    Reportado --> Validado: Administrador valida foto
    Reportado --> Rechazado: Administrador rechaza
    Validado --> Asignado: Administrador asigna observador
    Asignado --> EnProceso: Observador inicia registro
    EnProceso --> Registrado: Confirma entrega
    Registrado --> PremioEntregado: Actualiza boleto
    PremioEntregado --> [*]
    Rechazado --> [*]
```

---

## 20. Soporte por chat

```mermaid
flowchart LR
    V[Vendedor inicia soporte] --> A[Administrador recibe solicitud]
    V -->|Envía imágenes| A
    A -->|Responde| V
    A <--> O[Observador para comunicación administrativa]
    A --> C[Cerrar conversación]
    C --> D[Eliminar historial y adjuntos según permisos]
```

El vendedor no conversa por este canal con observadores. Los mensajes de evidencia de ventas offline marcados como permanentes no se eliminan al cerrar la conversación.

---

## 21. KPI e históricos

```mermaid
flowchart TD
    A[Administrador abre KPI] --> B[Seleccionar período]
    B --> C{¿Filtro?}
    C -->|General| D[Consolidar todos los vendedores]
    C -->|Grupo| E[Consolidar vendedores del grupo]
    C -->|Vendedor| F[Consolidar operación individual]
    D --> G[Mostrar ventas, boletos, números y loterías]
    E --> G
    F --> G
    G --> H[Actualizar indicadores y gráficos]
    H --> I[Descargar informe PDF]

    J[Vendedor abre Histórico] --> K[Seleccionar rango hasta 10 días]
    K --> L[Consultar ventas propias en solo lectura]
```

---

## 22. Reglas transversales

- El servidor es la fuente definitiva de autenticación, ventas, boletos, QR, estados, resultados y códigos offline.
- Una venta confirmada se registra una sola vez mediante una operación transaccional e idempotente.
- El código público del boleto tiene exactamente 7 dígitos y conserva ceros a la izquierda.
- El QR es cifrado y autenticado; el PDA no decide por sí solo la validez de un boleto.
- El tipo de apuesta es inmutable dentro del boleto y no se pueden mezclar modalidades.
- El valor del premio no se calcula ni se liquida automáticamente; el observador puede registrar el valor informado manualmente durante la entrega.
- Un boleto con estado `Premio entregado` no puede iniciar una nueva reclamación.
- Los códigos offline no utilizados permanecen en la base Lite aunque se cierre la aplicación, la sesión o el PDA.
- Solo los códigos utilizados y con evidencia QR enviada al administrador pueden eliminarse de la base local.

---

## 23. Trazabilidad principal

| Flujo | Requisitos relacionados |
| --- | --- |
| Autenticación y PDA | RS-001 a RS-010, RS-028 a RS-030 |
| Configuración administrativa | RS-011 a RS-027, RS-032, RS-040A |
| Venta y boleto | RS-060 a RS-080 |
| Impresión | RS-070 a RS-072, RS-087 |
| Conectividad | RS-081 a RS-086 |
| Operación offline | RS-088 a RS-101 |
| Validación y entrega de premios | RS-102 a RS-117 |
