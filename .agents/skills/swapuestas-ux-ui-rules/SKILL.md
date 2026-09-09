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
- Reuse the existing design system, Bootstrap utilities, colors, spacing, typography, and icon conventions.
- Keep visual hierarchy clear: page title, context, primary action, content, feedback, and secondary actions.
- Use sentence case and labels that describe the user's action or the data they will see.
- Every interactive control must have a visible focus state, an accessible name, a usable keyboard path, and a touch-friendly target.
- Provide loading, empty, success, error, disabled, and permission-restricted states where applicable.
- Do not allow content to overlap, become clipped, or require unintended horizontal scrolling.
- Keep responsive styles close to the component or view they support; avoid global rules that break other MVC pages.

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
- En cada fila de tabla de datos: una casilla de selección (checkbox) a la izquierda y, si existe consulta de detalle, el botón **Ver** a la derecha de la fila. Excepción: en Ventas > Por lotería y en Ventas offline (resumen por usuario y PDA) la tabla de resumen no usa casilla; **Ver** abre la lista detallada. En Ventas offline, **Ver** sobre cada código abre un popup con el detalle del código.
- Las acciones administrativas (Editar, Restablecer contraseña, Desbloquear, Eliminar, Asociar, Desasociar, Inactivar, Activar, Autorizar pago y equivalentes) van en un panel debajo de la tabla y la paginación, no dentro de la fila.
- El panel permanece visible; sus botones se habilitan al seleccionar una fila. Una sola fila seleccionada a la vez, excepto en PDA, donde se permite seleccionar uno o varios dispositivos. La fila seleccionada tiene un estilo persistente distinto del hover.
- El título del panel describe el tipo de registro (por ejemplo *Seguridad y acciones de usuario*). El texto de ayuda indica que hay que seleccionar un registro para habilitar las acciones. En PDA el texto indica que se puede seleccionar uno o varios dispositivos.

## Page shell

### Banner, encabezado y cabecera

- The banner/header identifies the application and the current user context without taking excessive vertical space.
- Include the page title or clear route context in the main content, not only in the header.
- Keep account, notification, help, and sign-out actions discoverable and keyboard accessible.
- On mobile, collapse secondary actions into an accessible menu without hiding the primary action.

### Side menu o menu lateral

- The side menu groups navigation by task and uses clear labels and familiar icons.
- Highlight the current section with text and a visual state; do not rely on color alone.
- Support collapsed and mobile states without trapping keyboard focus.
- The content area must resize or reflow when the menu changes; it must not be covered by the menu.
- Do not show links for actions the current role cannot use unless the product explicitly needs them disabled with an explanation.
- En **KPI** los filtros de periodo, grupo, vendedor, comparación y fechas actualizan todos los indicadores, barras y tablas. Las tablas del informe no usan casilla; **Revisar** navega al módulo relacionado. **Descargar PDF** conserva los filtros aplicados.
- En **Soporte** el listado de conversaciones muestra nombre, rol, último mensaje y estado reales. **Buscar chat** filtra por persona o texto. **Marcar atendida** cierra la conversación y conserva el historial. No hay tabla ni casilla en este módulo.
- En **Configuración** un solo **Guardar configuración** persiste horario, vigencia, límites, alertas y códigos offline. El catálogo de loterías usa casilla, paginación y el panel **Editar** / **Deshabilitar** o **Habilitar**.

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
- Keep the title, content, primary action, and close action clear.
- Move focus into an opened dialog, keep it contained while open, and return it to the triggering control when closed.
- Menus close predictably with Escape and do not become inaccessible at small widths.

### Alerts, notifications, and feedback

- Success, warning, error, and informational feedback must be visually and textually distinct.
- Explain what happened and what the user can do next.
- Do not use color as the only signal.
- Toasts must not cover important actions, remain long enough to read, and have an accessible announcement strategy.

### Estados de carga y estados vacios

- Loading states preserve the layout and identify the content being loaded.
- Empty states explain why there is no content and offer the next useful action when one exists.
- Disable duplicate submissions while an operation is in progress and show a clear completion result.

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