---
name: swapuestas-ux-ui-rules
description: "Use when creating, modifying, or reviewing UX/UI for SWApuestas, especially ASP.NET MVC Razor views, responsive layouts, tables, pagination, navigation, banners, forms, dialogs, alerts, and shared web controls."
---

# SWApuestas UX/UI Rules

This is the living UX/UI rulebook for the SWApuestas MVC application. Apply these rules to every new view and update this file when the team agrees on a new shared behavior.

## Idioma y caracteres permitidos

- Todo texto visible o generado para el usuario debe estar en español de Colombia: títulos, botones, enlaces, etiquetas, placeholders, ayudas, mensajes de validación, alertas, notificaciones, estados, tooltips, textos de accesibilidad y contenido de tablas.
- Usar vocabulario claro y natural para usuarios colombianos. Mantener consistencia en términos como `Guardar`, `Cancelar`, `Buscar`, `Limpiar`, `Siguiente`, `Anterior`, `Inicio` y `Ultimo`.
- Tildes permitidas: usar las tildes propias del español cuando la ortografia lo requiera.
- Letra ñ permitida: usar `ñ` cuando corresponda en palabras del español.
- Emojis y emoticonos: no permitidos en textos de usuario.
- Caracteres especiales ajenos al español de Colombia: no permitidos en textos de usuario.
- No usar letras de otros alfabetos, caracteres decorativos, comillas tipográficas, guiones tipográficos ni símbolos monetarios no requeridos.
- Preferir texto plano y signos básicos del español. Los símbolos funcionales indispensables de la interfaz, como `+`, `%`, `/`, `:`, `?` y `#`, solo se usan cuando tienen un significado real para el usuario.
- Los nombres de clases, propiedades, métodos, variables, rutas, claves JSON, nombres de tablas y otros identificadores técnicos deben mantenerse en ASCII, sin tildes, `ñ` ni espacios.
- Textos centralizados: almacenar los mensajes compartidos en recursos centralizados o constantes tipadas. No duplicar mensajes en vistas, controladores o scripts.
- Antes de completar una vista, revisar que no queden textos en inglés ni caracteres no justificados y que el atributo HTML `lang` corresponda a `es-CO`.

## General principles

- Design MVC views mobile-first and verify mobile, tablet, and desktop layouts.
- Reuse the existing design system, Bootstrap utilities, colors, spacing, typography, and icon conventions. Todo cambio o creación de interfaz del administrador debe seguir este libro UX/UI.
- Keep visual hierarchy clear: page title, context, primary action, content, feedback, and secondary actions.
- Use sentence case and labels that describe the user's action or the data they will see.
- Every interactive control must have a visible focus state, an accessible name, a usable keyboard path, and a touch-friendly target.
- Provide loading, empty, success, error, disabled, and permission-restricted states where applicable.
- Do not allow content to overlap, become clipped, or require unintended horizontal scrolling.
- Keep responsive styles close to the component or view they support; avoid global rules that break other MVC pages.

- En **Casos de premios**, el detalle de un caso reportado muestra la fotografía del ticket enviada desde el PDA, con un tamaño máximo contenido y texto alternativo descriptivo. En el listado de seguimiento, la segunda columna de datos (después de Ticket) es **Venta offline**: muestra el consecutivo `OFF-######` o un guion `-` si la venta fue en línea.

## Tables: mandatory behavior

Every data table created in the MVC application must include pagination. A table is not complete without the following:

- A page-size control that allows the user to choose the number of rows displayed.
- The maximum page size is 15 rows. Nunca ofrecer un valor mayor que 15 filas.
- The page-size control must have an accessible label, for example `Filas por pagina`.
- Use sensible options up to the maximum, such as 5, 10, and 15. Preserve the project's established options if they already exist.
- Pagination controls must include `Inicio`, `Anterior`, paginas numeradas (1, 2, 3...), `Siguiente`, and `Ultimo` when those actions are applicable.
- Numbered pages must display the current page and available page numbers in a predictable order: 1, 2, 3, and so on. Use an ellipsis only when there are many pages and keep the first and last page reachable.
- Disable unavailable navigation controls instead of hiding them without explanation.
- Preserve the selected page size while navigating and return to a valid page when filters or data changes reduce the total pages.
- Show a concise result summary, such as `Mostrando 1-15 de 42`, when the total is available.
- Preserve filter and sort state when changing pages.
- Resaltar la fila bajo el cursor del mouse o el foco del teclado para identificar la fila activa. El resaltado no debe ser la unica indicacion de datos seleccionados.
- Use a stronger persistent style for a selected row when selection exists, distinct from the temporary hover style.
- Ensure row highlighting works with keyboard navigation and does not depend only on `:hover`.
- On narrow screens, use a deliberate strategy: responsive columns, wrapped content, controlled table overflow, or a compact row/card representation. Important values and row actions must remain available.
- Keep action buttons in a stable column and provide accessible names that include the affected record when needed.
- En cada fila de tabla de datos: una casilla de selección (checkbox) a la izquierda y, si existe consulta de detalle, el botón **Ver** a la derecha de la fila. Excepción: en Ventas > Por lotería y en Ventas offline (resumen por usuario y PDA) la tabla de resumen no usa casilla; **Ver** abre la lista detallada. En **Ventas > Por lotería** cada columna de datos del resumen (excepto **Acción**) tiene flecha para ordenar; el primer clic ordena de menor a mayor y el segundo de mayor a menor. En Ventas offline, **Ver** sobre cada código abre un popup con el detalle del código. En **Usuarios**, **Ver** abre un popup con el detalle y no navega a otra pagina.
- Las acciones administrativas (Editar, Restablecer contraseña, Desbloquear, Eliminar, Asociar, Desasociar, Inactivar, Activar, Autorizar pago y equivalentes) van en un panel debajo de la tabla y la paginación, no dentro de la fila.
- En **Recaudo > Resumen**, la columna Recaudador de la tabla de grupos es una lista con buscador. Se puede cambiar uno o varios grupos. **Guardar** está junto a **Editar porcentajes** y queda deshabilitado si no hay cambios. Al pulsar **Guardar** pide la contraseña del administrador una sola vez y persiste las asignaciones. No hay botón Retirar asignación.
- El panel permanece visible; sus botones se habilitan al seleccionar una fila. Una sola fila seleccionada a la vez, excepto en PDA, donde se permite seleccionar uno o varios dispositivos. La fila seleccionada tiene un estilo persistente distinto del hover.
- El título del panel describe el tipo de registro (por ejemplo *Seguridad y acciones de usuario*). El texto de ayuda indica que hay que seleccionar un registro para habilitar las acciones. En PDA el texto indica que se puede seleccionar uno o varios dispositivos.

## Page shell

### Lienzo interno

- El administrador interno usa fondo marfil (`#f4efe4`), tarjetas crema con borde dorado, menu lateral verde oscuro (`#102820`) con borde e iconos dorados, y acentos `#c9a44a` / `#f0d078`.
- Titulos de pagina en Cinzel; texto de interfaz en Manrope. El lema del login y de la marca va en espanol de Colombia, no en ingles.
- La barra superior flota como pastilla crema. El pie muestra derechos y `Visión · Estrategia · Resultados`.
- El resaltado de filas y el foco usan tinte dorado, no verde menta.
- No colocar recuadros ni marcas de agua del globo sobre el contenido; el globo del menu va detras de las ultimas opciones del lateral.

### Banner, encabezado y cabecera

- The banner/header identifies the application and the current user context without taking excessive vertical space.
- Include the page title or clear route context in the main content, not only in the header.
- Keep account, notification, help, and sign-out actions discoverable and keyboard accessible.
- En la barra superior, **Avisos** es una campana (icono SVG, sin emoji) con el número de notificaciones nuevas. Al abrirla se listan las nuevas y cada ítem navega al detalle. **Ver todas las notificaciones** abre el módulo. El contador se oculta cuando no hay nuevas.
- Cuando se genera una notificación para el administrador, llega en tiempo real: se actualiza la campana y aparece un recuadro de aviso junto a la barra, visible unos 3 segundos, con el mensaje y el enlace **Ver**. El recuadro no cubre el menú lateral ni las acciones principales. Respetar `prefers-reduced-motion`.
- On mobile, collapse secondary actions into an accessible menu without hiding the primary action.

### Side menu o menu lateral

- The side menu groups navigation by task and uses clear labels and familiar icons. Ventas y Recaudo son acordeones compactos: el encabezado es una fila con borde dorado y no se estira para llenar el alto del lateral. El espacio sobrante queda debajo de los grupos cerrados. En Recaudo el primer subítem se llama **Resumen** y cada subítem lleva icono de trazo, igual que en Ventas.
- Highlight the current section with text and a visual state; do not rely on color alone.
- Support collapsed and mobile states without trapping keyboard focus.
- En el pie del menu el globo dorado queda detras de las ultimas opciones y la tarjeta de cuenta con icono de salida. **Cerrar sesion** se confirma al abrir la cuenta. No mostrar la frase de disciplina en el menu.
- Las barras de desplazamiento son delgadas, doradas y redondeadas, sin flechas grises del sistema. En el menu el riel es transparente para no tapar el globo.
- The content area must resize or reflow when the menu changes; it must not be covered by the menu.
- Do not show links for actions the current role cannot use unless the product explicitly needs them disabled with an explanation.
- En **Instalaciones** hay una tarjeta **Registro PDA** con el botón **Instalar**. Ese botón abre el mismo asistente de **PDA > Registrar PDA**.
- En **Registrar PDA**, antes del tipo de usuario el administrador elige el **Equipo que va a registrar**: *PDA de venta* o *Celular*. Son dos tarjetas de opción con borde dorado, título y ayuda corta; la elegida queda con fondo resaltado y barra dorada a la izquierda. La lista de perfiles se arma según esa elección: el PDA de venta admite vendedor, recaudador y observador; el celular solo recaudador y observador. Cada clase tiene su propio camino de instalación y su propio mensaje de error, así que un ajuste en una no cambia la otra.
- Al terminar bien **Registrar PDA**, el diálogo de éxito muestra *Quedó registrado como:* seguido del nombre con que aparece el equipo en el listado (por ejemplo `PDA-4F2A9C10`), en negro, negrita, mayúscula y un punto más grande que el texto del diálogo. La bitácora de avance nunca muestra ese código y el administrador nunca lo escribe.
- En **KPI** los filtros de periodo, grupo, vendedor, comparación y fechas actualizan todos los indicadores, barras y tablas. Las tablas del informe no usan casilla; **Revisar** navega al módulo relacionado. **Descargar PDF** conserva los filtros aplicados.
- En **Ventas offline** hay tres pestañas: **Información general**, **Generar códigos** y **Registro QR vendidos**. El resultado de registrar un QR vendido se muestra en un diálogo modal, no en una franja de texto.
- En **Soporte técnico** y **Atención al cliente** no hay título de pagina duplicado, pie ni buscador: el hilo llena el alto. Controles (iniciar, encabezado del hilo, adjunto y enviar) van compactos. En Atención al cliente, **Iniciar conversación** es una sola fila encima del chat, sin etiquetas visibles (siguen en `sr-only`). A la izquierda van las conversaciones. En Soporte técnico un texto indica que los reportes llegan desde el PDA. **Marcar atendida** cierra la conversación y conserva el historial. No hay tabla ni casilla. El listado y el hilo tienen barra de desplazamiento vertical propia. La conversación con actividad reciente sube al inicio. El adjunto de imagen o PDF usa un botón circular con icono de clip; al elegir archivo aparece un chip con el nombre y **Quitar archivo**.
- En **Configuración** un solo **Guardar configuración** persiste horario (apertura y cierre en formato 12 h con AM/PM; no pueden ser iguales), vigencia, límites, alertas, códigos offline, la retención de históricos (6 meses fijos y visibles, y 1, 2 o 3 meses a eliminar), los minutos de inactividad para cerrar la sesión del administrador y del PDA, el texto parametrizable de la tirilla y el mensaje al superar el tope (también se descarga al PDA para usarlo sin conexión). La sección **Versión de la aplicación** es un formulario aparte: sube el APK, el nombre de versión y el número de compilación; muestra hasta tres versiones y marca la última como vigente. No altera el registro de PDA por USB. La sección **Números restringidos** permite agregar y eliminar números de 3 o 4 dígitos; esos números no se pueden jugar y el PDA los descarga con la configuración. La sección **Días de venta** muestra lunes a domingo; para cada día se marcan las loterías vendibles y **Guardar días de venta** persiste esa matriz. El catálogo de loterías usa casilla, paginación y el panel **Editar** / **Deshabilitar** o **Habilitar**. Al **Agregar lotería** se pide al menos un día de juego.
- El formulario de lotería (en **Configuración** y en **Loterías**) pide la **Jornada** con el dropdown con buscador (`_CampoJornada`), después del tope y antes del horario. Las jornadas son fijas: Mañana cierra hasta las 12:00, Tarde hasta las 18:00 y Noche el resto del día; el administrador no las crea ni las renombra. Al elegir la jornada, la **Hora de fin** solo admite horas de esa jornada y muestra `HoraFinSegunJornada` si queda fuera. Si se cambia la hora de fin desde **Horario habilitado**, la lotería pasa sola a la jornada que corresponde. **Habilitar** / **Deshabilitar** conserva la jornada.
- El **Catálogo de loterías** de Configuración tiene, encima del buscador, un filtro de jornada en píldoras (Mañana, Tarde, Noche y Todas; la elegida va en verde oscuro). La tabla muestra la columna **Jornada** y se ordena por Lotería, Jornada, Horario habilitado (por hora de fin) o Estado con los encabezados `.table-sort`: el primer clic ordena de menor a mayor y el segundo al revés; la flecha dorada indica el orden actual. Filtro, orden y búsqueda se conservan al paginar, buscar y guardar horarios.
- En **Topes por lotería**, entre el subtítulo y la tabla van las mismas píldoras de jornada (Mañana, Tarde, Noche y Todas) como pestañas (`role="tab"`, flechas, Inicio y Fin del teclado). Filtran en la misma página por la hora de cierre, sin recargar. Abre en **Todas** y recuerda la última pestaña elegida en la misma pestaña del navegador. Si una jornada no tiene loterías se muestra el estado vacío. **Guardar topes** sigue enviando todas las loterías, también las ocultas por el filtro.
- En el catálogo, **Guardar horarios** está deshabilitado (con texto de ayuda en `title`) hasta que cambie la hora de inicio o de fin de alguna lotería. La fila cambiada se resalta en dorado claro con barra dorada a la izquierda. Los cambios pendientes se recuerdan al pasar de página en la misma pestaña y se envían juntos; solo viajan las loterías cambiadas. Si el API rechaza, los cambios siguen pendientes y el error nombra la lotería; si se guarda, se limpian. Si no había nada que guardar, se avisa con el diálogo **Aviso** ("No hay horarios modificados para guardar."), no con franja. Al editar horarios desde el catálogo solo se exige que la hora editada quede dentro del horario de actividad del PDA.
- Al enviar cualquier formulario del administrador, o al seguir un enlace de la misma página (paginar, ordenar, filtrar), la pantalla vuelve al mismo punto de desplazamiento en lugar de subir al inicio.
- En **Loterías** (Ventas por lotería), todas las columnas de datos del resumen se ordenan con `.table-sort`, menos **Acción**. **Hora cierre** muestra la hora de fin de cada lotería.

- En **Configuración de recaudo**, debajo de la tabla **Grupos** están **Editar porcentajes** y **Guardar**. **Guardar** aplica los cambios de recaudador de uno o varios grupos y pide la contraseña; permanece deshabilitado si no hay cambios. Al pulsar **Editar porcentajes**, la columna Porcentaje de todos los grupos de la página se vuelve editable con el valor que ya tenía cada grupo, en un campo con sufijo `%`. **Editar porcentajes** se oculta y aparecen **Guardar porcentajes** y **Cancelar**, junto con una ayuda. El campo admite vacío o 0 (quita el porcentaje de un grupo sin recaudador); un grupo con recaudador necesita de 1 a 100. **Cancelar** o Escape restauran los valores. Guardar porcentajes persiste todos los grupos en un solo envío.
- En **Configuración de recaudo > Vendedores sin grupo** no hay **Retirar asignación**. La última columna, **Asignar un grupo**, tiene en cada fila un dropdown con buscador de grupos. Al elegir un grupo en cualquier fila aparecen debajo de la tabla **Guardar grupos** y **Cancelar**. Guardar pide la contraseña una sola vez y asigna todos los vendedores con grupo elegido; Cancelar limpia los dropdowns y oculta los botones.
- En **Configuración de recaudo > Integrantes del grupo**, cada encabezado de columna tiene un icono `?` y un tooltip que explica qué muestra la columna. Aparece al pasar el mouse o al llegar con el teclado (Tab), sobre fondo verde oscuro con borde dorado. Los textos viven en `UiTexts` (`AyudaColumna...`). Reutilizar `.th-ayuda` para tooltips de encabezados en otras tablas.
- Las tarjetas de resumen del grupo (Recaudador, Porcentaje, Total por recaudar, Total recaudado y Total pendiente) llevan el mismo `?` en la esquina superior derecha, con tooltip que explica el valor (`AyudaTarjeta...`). **Total pendiente** suma la columna Total pendiente de los integrantes e incluye deudas de días anteriores; **Total por recaudar** no las incluye.
- En **Configuración de recaudo**, la tabla **Grupos** muestra **Total por recaudar** y **Total recaudado** con las ventas y pagos de los integrantes, usando el porcentaje del grupo aunque no haya recaudador o el recaudador ya no exista. **Recaudador** sigue en No aplica hasta que haya uno asignado.
- En **Configuración de recaudo > Integrantes del grupo**, al seleccionar un vendedor se habilita **Eliminar del grupo** en el panel inferior; pide la contraseña del administrador y vuelve a la misma vista.

### Login

- En **login** y **cambiar contraseña** el fondo es verde oscuro con globos dorados. Arriba va el emblema circular NR, el nombre **New Rich** y el lema. El formulario es una tarjeta oscura con borde dorado, campos con icono y botón dorado **Ingresar**. Debajo aparecen **Disciplina**, **Visión** y **Éxito**. No usar recuadros negros sueltos ni el texto NEW RICH como imagen aparte del nombre.

### Main content

- Use one clear page heading and a consistent content width.
- Put primary actions near the heading or at the start of the relevant workflow.
- Organize dense administrative pages with sections, filters, summaries, and tables rather than unrelated floating cards.
- Preserve a predictable reading and tab order.

### Footer o pie de pagina

- Keep the footer concise and consistent across MVC views.
- Include version or support information only when it is useful to the user.
- Do not place essential navigation, errors, or actions only in the footer.
- Ensure the footer follows the content naturally on mobile and does not overlap fixed elements.

## Common controls

### Buttons and links

- Use buttons for actions and links for navigation.
- The label states the result of the action, for example `Guardar cambios`, `Eliminar usuario`, or `Ver boleto`.
- Mark destructive actions clearly and require confirmation when the requirement calls for it.
- Keep primary, secondary, and destructive styles consistent across views.
- Do not use text inside a rounded control when a familiar icon with an accessible tooltip and name is sufficient; use icon plus text when the action is unfamiliar or high risk.

### Formularios

- Every input has a visible label, a suitable type, a clear required/optional state, and validation feedback.
- Preserve entered values after validation errors.
- Place errors next to the affected field and summarize them at the form level when useful.
- Use input constraints and server-side validation; client-side validation is supplementary.
- On mobile, fields and actions must fit the viewport without horizontal scrolling.
- En **Casos de premios > Reportar ticket**, el campo **Código del ticket** admite hasta 400 caracteres para pegar el código de 7 dígitos o el contenido completo del QR. El control es un área de texto con ajuste de línea; no recortar la entrada a 7 caracteres.
- **Validar ticket** consulta el boleto aunque no sea ganador. Si existe, muestra la tirilla de compra y un recuadro de estado con texto y color: verde para ganador, rojo para no ganó, dorado para por jugar o jugado sin resultado, terracota para vencido, azul para pagado y verde para premio entregado. El color no es la única señal: el recuadro incluye el estado y una frase. Si el ticket es ganador y aún no tiene caso, **Iniciar caso de premio** queda disponible.

### Filtros y busqueda

- Group filters near the table they control and provide `Buscar`, `Limpiar` or equivalent explicit actions.
- Show active filters and keep them when the user changes pages.
- Debounce only where it improves the experience and never hide whether a search is still running.
- Provide a useful empty state when no records match the filters.

### Dropdowns y listas desplegables

- Todo dropdown o lista desplegable debe incorporar un buscador dentro del mismo control.
- El buscador debe permitir filtrar las opciones mientras el usuario escribe, sin abrir una pagina diferente ni desplazar el contexto actual.
- Incluir una etiqueta accesible, texto de ayuda y un estado claro sin coincidencias cuando no existan opciones que cumplan la busqueda.
- Permitir navegar, seleccionar y cerrar el control con teclado, incluyendo flechas, Enter y Escape.
- Mantener visible la opcion seleccionada y permitir limpiarla cuando el campo no sea obligatorio.
- Para listas extensas, usar busqueda incremental y no cargar todas las opciones visualmente de una sola vez si afecta el rendimiento.
- En mobile, el dropdown y su buscador deben ajustarse al viewport, conservar un area tactil usable y no provocar scroll horizontal.
- Respetar el idioma español de Colombia en el placeholder y los mensajes, por ejemplo `Buscar una opcion` y `No se encontraron resultados`.

### Dialogos y menus

- Use dialogs for focused decisions, confirmation, short forms, or avisos puntuales (por ejemplo cuando no hay boletos en una lotería). No usar la franja roja de error para ese tipo de aviso.
- Las acciones administrativas protegidas (crear, actualizar o eliminar datos) piden la contraseña del administrador en el modal **Confirmar contraseña** al persistir, no al abrir el formulario. Cancelar cierra el modal y no ejecuta la acción. En PDA un clic de lote pide la contraseña una sola vez.
- Keep the title, content, primary action, and close action clear.
- Move focus into an opened dialog, keep it contained while open, and return it to the triggering control when closed.
- Menus close predictably with Escape and do not become inaccessible at small widths.

### Alerts, notifications, and feedback

- Success, warning, error, and informational feedback must be visually and textually distinct.
- Explain what happened and what the user can do next.
- Do not use color as the only signal.
- Toasts must not cover important actions, remain long enough to read, and have an accessible announcement strategy.
- El aviso de notificación nueva dura 3 segundos, anuncia el mensaje con `aria-live` y ofrece **Ver** para abrir el detalle.
- En el administrador, los mensajes de la página (registro guardado, error del API, errores generales del formulario, error de ingreso o de cambio de contraseña, llave de instalación y errores del registro de PDA) se muestran en el diálogo **Aviso** con el botón **Aceptar**, no en franjas verdes o rojas sobre el contenido. El texto sale de `AvisoPagina.Texto` (`AvisoModal`, luego `AvisoIngreso`, luego `ViewData["AvisoPagina"]`, luego los errores del modelo sin campo). Desde JavaScript se abre con `window.openAviso(texto)`. Siguen en línea los errores de cada campo debajo del campo, las advertencias fijas de las vistas **Eliminar**, el resumen del formulario de usuario en modal y el error dentro del modal **Confirmar contraseña**.

### Estados de carga y estados vacios

- Loading states preserve the layout and identify the content being loaded.
- Empty states explain why there is no content and offer the next useful action when one exists.
- Disable duplicate submissions while an operation is in progress and show a clear completion result.

## PDA del recaudador

- El inicio del recaudador muestra opciones y no la lista de cobros. Arriba, la tarjeta dorada muestra la fecha del día en Colombia y, debajo, **Total recaudar** junto a **Total recaudado**. Total recaudar suma lo generado para cobrar a todas las personas a cargo, también las que no tienen grupo. Total recaudado suma lo ya cobrado ese día. Debajo, **Resumen de turno** muestra el total de grupos asignados (sin contar «Sin grupo») y el total de vendedores asignados. Luego van los accesos. **Recaudar** abre la pantalla de cobro. Historial y Métricas siguen disponibles desde el inicio y desde la barra inferior.
- En **Recaudar**, arriba van botones con los grupos asignados y **Todos**. **Todos** incluye los vendedores de todos los grupos y los que no tienen grupo. Un grupo muestra solo los suyos.
- La lista abre en **Pendientes**. Al cobrar, el vendedor queda etiquetado **Ya cobrado**, sale de pendientes y pasa a **Cobrados**. Desde **Cobrados** se le puede cobrar de nuevo. La tarjeta muestra nombre, alias, estado, vendido, valor a cobrar y pendiente. En **Cobrados** también muestra **Total pagado** (lo que ese vendedor ya entregó hoy), en una cuadrícula de cifras. Conserva el campo y **Registrar cobro**.
- La búsqueda tiene etiqueta, campo y botón **Buscar**. Encuentra por nombre completo, alias, usuario o documento, también con una palabra incompleta.

## PDA del observador

- En **Más**, la opción **Vendedores** abre la lista de vendedores (solo ese rol), ordenada por nombre. Arriba van el buscador (nombre, alias, usuario o documento, sin importar tildes ni mayúsculas), los filtros de **Grupo** (Todos los grupos, cada grupo en orden alfabético y **Sin grupo** si aplica) y **Estado** (Todos, Bloqueados, Sin bloqueo), el texto *Mostrando X de Y vendedores* y **Limpiar filtros**.
- Cada tarjeta muestra nombre, usuario, documento, grupo y estado (Bloqueado en rojo, Activo en verde), con los botones **Desbloquear** (habilitado solo si está bloqueado) y **Restablecer contraseña**.
- Antes de ejecutar cualquiera de las dos acciones se pregunta nombrando al vendedor, con **Sí, desbloquear** o **Sí, restablecer** y **Cancelar**. Cancelar no cambia nada. Al restablecer, la contraseña temporal se muestra una sola vez en el mensaje de resultado. Mientras una acción está en curso no se acepta otra.

## Responsive MVC checklist

Before completing a view, verify:

- At mobile, tablet, and desktop widths, there is no unintended horizontal scroll.
- Tables, pagination, filters, forms, banners, side menu, dialogs, alerts, and footer remain usable.
- Long names, validation errors, empty states, permission changes, and large result counts do not break the layout.
- Text, controls, icons, and row actions do not overlap or become clipped.
- Keyboard focus and touch targets remain visible and usable.
- Reduced-motion preferences are respected.

## Living rulebook workflow

When a new shared UX decision is agreed:

1. Add it to the most specific section in this file.
2. State the user behavior and the acceptance condition.
3. Apply it to new views and update affected shared components.
4. Verify it at mobile, tablet, desktop, keyboard, empty, loading, error, and permission states when relevant.