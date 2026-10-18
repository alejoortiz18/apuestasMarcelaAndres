# Especificación funcional: Utilidades y análisis financiero

**Estado:** Propuesta funcional para desarrollo\
**Módulo:** Utilidades\
**Perfiles:** Administrador y Vendedor Web\
**Objetivo:** Centralizar los ingresos por ventas y los gastos
operativos del sistema, ofrecer indicadores gerenciales confiables y
permitir el control administrativo de los tipos de gasto y de los
registros de descuentos y comisiones existentes.

------------------------------------------------------------------------

## 1. Objetivos y principios de diseño

1.  Crear un módulo llamado **Utilidades** para los perfiles
    Administrador y Vendedor Web.
2.  Centralizar en una sola vista los ingresos provenientes de las
    ventas y los gastos registrados por todos los usuarios.
3.  Mostrar indicadores financieros claros, trazables y filtrables por
    periodo, usuario y tipo de movimiento.
4.  Calcular las ganancias netas como ingresos por ventas menos gastos
    registrados, sin duplicar ni alterar los datos de origen.
5.  Permitir que el Administrador gestione los tipos de gasto y tenga
    control administrativo sobre los registros de descuentos y
    comisiones que actualmente se manejan en **Por cubrir** y
    **Cubierto**.
6.  Priorizar la experiencia de usuario: filtros visibles, cifras
    consistentes, navegación simple, estados vacíos claros,
    confirmaciones para acciones sensibles y acceso al detalle desde
    cada KPI.
7.  Mantener la integridad de los registros financieros: auditoría,
    permisos por rol, validaciones y trazabilidad de cambios.

> **Criterio contable:** las ganancias netas que se muestran en este
> módulo son un resultado operativo calculado con los ingresos de ventas
> identificados y los gastos registrados. No deben presentarse como
> utilidad contable definitiva si no se incluyen todos los costos,
> devoluciones, anulaciones, premios u otros conceptos aplicables. La
> definición exacta de cada concepto debe validarse con las reglas
> actuales del sistema.

------------------------------------------------------------------------

## 2. Navegación y permisos

### 2.1 Vendedor Web

El menú tendrá dos elementos principales:

-   **Ventas:** conservar todos los elementos que actualmente existen en
    el menú del Vendedor Web, sin eliminar ni cambiar su comportamiento.
-   **Utilidades:** nuevo módulo con los subítems:
    -   **Resumen:** panel de indicadores y análisis.
    -   **Registrar gasto:** formulario para registrar un gasto.
    -   **Movimientos:** listado de gastos registrados y consulta de
        ingresos consolidados, según los permisos disponibles.

El menú actual de Ventas debe trasladarse visualmente bajo el grupo
**Ventas**, preservando rutas, permisos, operaciones y funcionalidades
existentes. No se deben duplicar ni perder opciones durante la
reorganización.

### 2.2 Administrador

El menú del Administrador tendrá el nuevo elemento **Utilidades**, con:

-   **Resumen**
-   **Registrar gasto**
-   **Movimientos**
-   **Configuración de gastos**

Además, el Administrador tendrá acceso completo a los registros de
descuentos y comisiones que actualmente se encuentran en **Por cubrir**
y **Cubierto**, con las acciones permitidas por las reglas de negocio y
con auditoría.

### 2.3 Matriz de permisos

  -----------------------------------------------------------------------
  Funcionalidad           Vendedor Web            Administrador
  ----------------------- ----------------------- -----------------------
  Ver KPI consolidados    Sí                      Sí
  del sistema                                     

  Ver gastos de todos los Sí, según el alcance    Sí
  usuarios                operativo definido      

  Registrar gastos        Sí                      Sí

  Consultar movimientos y Sí                      Sí
  aplicar filtros                                 

  Editar o anular gastos  Según política          Sí, con auditoría
  propios                 configurable            

  Editar o anular gastos  No                      Sí, con auditoría
  de otros usuarios                               

  Crear, editar, activar  No                      Sí
  o desactivar tipos de                           
  gasto                                           

  Eliminar físicamente    No                      No recomendado; usar
  tipos de gasto                                  desactivación
  utilizados                                      

  Administrar registros   No                      Sí, con trazabilidad
  de descuentos y                                 
  comisiones de Por                               
  cubrir/Cubierto                                 

  Consultar historial de  Solo el alcance         Sí
  cambios                 permitido               
  -----------------------------------------------------------------------

Los permisos deben validarse en el servidor/API, no solamente ocultando
opciones en la interfaz. Las acciones administrativas sensibles deben
registrar quién las realizó, cuándo y qué cambió.

------------------------------------------------------------------------

## 3. Resumen: panel de KPI

El panel debe funcionar como una herramienta de análisis gerencial, no
únicamente como un listado de valores. Debe permitir identificar
rápidamente cuánto ingresó, cuánto se gastó, qué resultado se obtuvo y
dónde se concentran los movimientos.

### 3.1 Filtros globales

Ubicar los filtros en una zona visible, por encima de los KPI. Todos los
indicadores y gráficos del panel deben actualizarse con los mismos
filtros.

Filtros mínimos:

-   **Periodo:** hoy, ayer, últimos 7 días, últimos 30 días, mes actual,
    mes anterior, rango personalizado y todo el histórico.
-   **Fecha desde / hasta:** cuando se seleccione rango personalizado.
-   **Usuario:** todos o un usuario específico.
-   **Tipo de gasto:** todos o un tipo determinado.
-   **Origen de venta:** esquema/tablas de origen identificadas,
    incluyendo `dbo` y el esquema manual.
-   **Estado del movimiento:** activo, anulado u otros estados
    existentes, de acuerdo con las reglas de negocio.
-   **Comparación:** periodo anterior equivalente, cuando haya datos
    suficientes.

Mostrar de forma visible el periodo aplicado, la fecha/hora de
actualización y los filtros activos. Incluir acciones **Aplicar
filtros** y **Limpiar filtros** si el volumen de datos hace conveniente
aplicar los filtros bajo demanda.

### 3.2 KPI principales

Mostrar tarjetas en la parte superior, con formato de moneda colombiana
(COP), separadores legibles y valores exactos accesibles al abrir el
detalle.

1.  **Ventas brutas:** suma de las ventas válidas del periodo, según la
    definición de negocio.
2.  **Otros ingresos:** solo si el sistema tiene fuentes adicionales de
    ingresos identificadas y aprobadas. No sumar valores desconocidos
    como ingresos.
3.  **Ingresos totales:** suma de los conceptos de ingreso definidos
    para el sistema, sin duplicar ventas.
4.  **Gastos totales:** suma de los gastos válidos registrados por todos
    los usuarios en el periodo.
5.  **Ganancia neta operativa:** ingresos totales menos gastos totales
    incluidos en el cálculo.
6.  **Margen neto operativo (%):** ganancia neta operativa / ingresos
    totales × 100. Si los ingresos son cero, mostrar "N/A" en lugar de
    dividir por cero.
7.  **Cantidad de gastos:** número de registros de gasto válidos.
8.  **Gasto promedio:** gastos totales / cantidad de gastos válidos. Si
    no hay gastos, mostrar cero o "N/A" según el estándar visual del
    sistema.
9.  **Usuarios que registraron gastos:** cantidad de usuarios diferentes
    que registraron gastos en el periodo.

Cada tarjeta debe mostrar, cuando aplique, variación frente al periodo
anterior equivalente, con diferencia absoluta y porcentual. Una
variación no debe mostrarse como 0 % si el periodo anterior tiene base
cero; usar "Sin base de comparación".

**Importante:** "Ganancia neta operativa" debe tener una definición
visible mediante ayuda contextual. No debe confundirse con el saldo de
caja ni con la utilidad contable total.

### 3.3 KPI de gastos y control

Agregar indicadores y desgloses para:

-   Gastos por usuario.
-   Gastos por tipo de gasto.
-   Gastos por día, semana y mes.
-   Gastos por observación o concepto, cuando sea útil y seguro.
-   Mayor gasto individual del periodo.
-   Tipos de gasto con mayor participación porcentual.
-   Tendencia de ingresos, gastos y ganancia neta operativa.
-   Comparación entre el periodo actual y el anterior.
-   Cantidad y valor de registros anulados, mostrados por separado y
    excluidos de los totales activos.
-   Gastos pendientes de revisión, si se implementa un flujo de
    aprobación.

Los gráficos deben incluir títulos descriptivos, unidades, leyendas y
estados vacíos. Al seleccionar un segmento, barra o punto, abrir el
detalle filtrado correspondiente.

### 3.4 Presentación recomendada

Orden sugerido del panel:

1.  Encabezado **Utilidades** con periodo seleccionado y última
    actualización.
2.  Filtros globales.
3.  Fila de KPI financieros principales.
4.  Gráfico temporal de ingresos, gastos y ganancia neta.
5.  Distribución de gastos por tipo.
6.  Distribución de gastos por usuario.
7.  Tabla de movimientos recientes.
8.  Alertas o hallazgos: aumento atípico del gasto, gasto concentrado en
    un tipo o periodo con resultado negativo, siempre con reglas
    explícitas y sin conclusiones engañosas.

En pantallas pequeñas, las tarjetas deben reorganizarse sin perder
legibilidad. Evitar saturar el panel: los indicadores secundarios pueden
ubicarse en secciones expandibles.

------------------------------------------------------------------------

## 4. Registro de gastos

### 4.1 Formulario

El usuario debe poder registrar un gasto mediante un formulario sencillo
que incluya:

-   **Tipo de gasto:** obligatorio; selección desde el catálogo
    configurado.
-   **Fecha del gasto:** obligatoria; por defecto, fecha actual.
-   **Valor:** obligatorio, mayor que cero, con formato COP y
    almacenamiento numérico preciso.
-   **Observación:** obligatoria o configurable según el tipo de gasto;
    longitud máxima definida por el sistema.
-   **Usuario que registra:** tomado de la sesión autenticada; no
    editable desde el formulario.
-   **Referencia o soporte:** opcional, si la arquitectura admite
    adjuntos o referencias documentales.
-   **Guardar** y **Cancelar**.

Tipos iniciales sugeridos: transporte, almuerzo/alimentación, entrega de
premio, pago de servicios, mensajería/entregas, materiales/insumos,
mantenimiento, comisiones/gastos bancarios y otros. El Administrador
podrá configurar el catálogo. Estos valores son iniciales sugeridos, no
una lista cerrada.

### 4.2 Validaciones y experiencia de usuario

-   Validar campos obligatorios en cliente y servidor.
-   Rechazar valores negativos, cero o formatos inválidos.
-   Evitar doble envío por clic repetido; usar protección contra
    duplicados cuando sea viable.
-   Mostrar confirmación de guardado con el valor, tipo y fecha
    registrados.
-   Después de guardar, actualizar los KPI y el listado sin exigir una
    recarga completa cuando la arquitectura lo permita.
-   Si ocurre un error, conservar los datos del formulario y mostrar un
    mensaje accionable.
-   No permitir que el usuario suplante a otro usuario mediante campos
    manipulables.
-   Registrar fecha de creación y, cuando corresponda, fecha de
    actualización.

### 4.3 Edición y anulación

Los gastos no deberían borrarse físicamente una vez registrados. Para
corregirlos, utilizar edición controlada o anulación lógica.

-   El Vendedor Web podrá editar/anular sus propios registros si la
    política de negocio lo permite.
-   El Administrador podrá editar/anular registros de cualquier usuario.
-   Solicitar motivo de edición o anulación.
-   Guardar valores anteriores y nuevos, usuario, fecha y motivo en el
    historial de auditoría.
-   Los movimientos anulados deben permanecer consultables,
    identificarse claramente y excluirse de los KPI activos.
-   No permitir la edición de registros cerrados o conciliados si el
    sistema incorpora ese estado, salvo un flujo administrativo
    explícito.

------------------------------------------------------------------------

## 5. Catálogo y configuración de tipos de gasto

Solo el Administrador podrá ingresar a **Utilidades \> Configuración de
gastos**.

### 5.1 Campos del catálogo

-   Identificador único.
-   Nombre del tipo de gasto.
-   Descripción opcional.
-   Estado: activo/inactivo.
-   Fecha de creación y actualización.
-   Usuario que creó o actualizó el registro.
-   Opcional: código, categoría gerencial y si requiere observación
    obligatoria.

### 5.2 Operaciones

-   Crear un tipo de gasto.
-   Consultar y buscar tipos existentes.
-   Editar nombre, descripción y configuración.
-   Activar o desactivar.
-   Consultar cuántos movimientos están asociados a cada tipo.

No eliminar físicamente un tipo que ya tenga movimientos asociados.
Desactivarlo para impedir nuevos registros y conservar el historial. Los
gastos antiguos deben seguir mostrando el tipo original aunque este se
encuentre inactivo.

El sistema debe impedir nombres duplicados cuando corresponda, ignorando
diferencias de mayúsculas/minúsculas y espacios sobrantes.

------------------------------------------------------------------------

## 6. Consolidación de ingresos por ventas

### 6.1 Fuentes

Los ingresos deben obtenerse de todas las tablas donde actualmente se
almacenan ventas, incluyendo las fuentes del esquema `dbo` y las del
esquema manual.

Antes de implementar el cálculo, se debe inspeccionar el modelo de datos
y documentar:

-   Tablas que representan ventas.
-   Campos de importe, fecha, estado y usuario asociados.
-   Relación entre cabecera y detalle, si existe.
-   Estados que representan venta válida, anulada, devuelta o pendiente.
-   Posibles duplicados entre fuentes.
-   Reglas de descuentos, premios, pagos y anulaciones que afecten el
    total.
-   Diferencias entre fecha de creación, fecha de venta y fecha de
    liquidación.

No se deben inventar nombres de tablas o columnas ni asumir que todos
los importes representan ingresos reconocidos.

### 6.2 Reglas de consolidación

1.  Crear una capa o consulta de consolidación que unifique las fuentes
    de ventas.
2.  Normalizar fecha, importe, estado, usuario y origen.
3.  Definir una clave de negocio que permita detectar duplicados cuando
    una misma venta pueda aparecer en más de una fuente.
4.  Excluir anulaciones y registros no válidos de los ingresos activos,
    según las reglas reales del sistema.
5.  Evitar sumar simultáneamente cabeceras y detalles cuando ambos
    representen el mismo valor.
6.  Mantener identificable la fuente original para auditoría y
    conciliación.
7.  Permitir filtrar por origen para analizar diferencias entre `dbo` y
    el esquema manual.
8.  Comparar los totales consolidados con los reportes actuales de
    ventas y documentar cualquier diferencia antes de publicar el
    módulo.

### 6.3 Rendimiento

Si el volumen de ventas es alto, evaluar vistas SQL, consultas
agregadas, índices adecuados o tablas de resumen. No duplicar
información financiera sin una estrategia de actualización y
conciliación. Los KPI deben consultar datos con tiempos razonables sin
bloquear las operaciones de venta.

------------------------------------------------------------------------

## 7. Cálculos y definiciones de los KPI

Las fórmulas deben centralizarse para que tarjetas, gráficos, tablas y
exportaciones utilicen los mismos criterios.

-   **Ingresos totales** = suma de los ingresos válidos incluidos en el
    alcance aprobado.
-   **Gastos totales** = suma de los gastos no anulados dentro del
    periodo y filtros seleccionados.
-   **Ganancia neta operativa** = ingresos totales − gastos totales.
-   **Margen neto operativo (%)** = ganancia neta operativa / ingresos
    totales × 100, si los ingresos son mayores que cero.
-   **Gasto promedio** = gastos totales / cantidad de gastos válidos, si
    la cantidad es mayor que cero.
-   **Participación por tipo (%)** = gasto del tipo / gastos totales ×
    100, si los gastos totales son mayores que cero.
-   **Variación absoluta** = valor del periodo actual − valor del
    periodo de comparación.
-   **Variación porcentual** = (valor actual − valor anterior) / valor
    anterior × 100, únicamente si el valor anterior permite el cálculo.

Las sumas deben realizarse con precisión decimal apropiada para moneda.
No utilizar tipos de punto flotante binario para almacenar importes
financieros. Definir una única zona horaria de negocio para agrupar y
filtrar por fecha; confirmar que sea la zona horaria usada por la
aplicación.

### 7.1 Alcance del resultado

Los gastos registrados por cualquier usuario afectan el resultado global
del sistema. El panel debe mostrar, por separado:

-   Resultado global de todos los usuarios.
-   Gastos registrados por usuario.
-   Ingresos por origen y, solo si la fuente lo permite con fiabilidad,
    por usuario.
-   Resultado por periodo.

No atribuir una venta a un usuario basándose en una suposición. Si no
existe una relación fiable entre venta y usuario, mostrar el ingreso
global y dejar el desglose por usuario como no disponible.

------------------------------------------------------------------------

## 8. Movimientos y detalle analítico

Crear una vista de consulta con una tabla paginada, ordenable y
filtrable.

### 8.1 Columnas de gastos

-   Fecha del gasto.
-   Tipo de gasto.
-   Observación.
-   Valor.
-   Usuario que registró.
-   Estado.
-   Fecha de creación.
-   Último usuario que modificó, cuando aplique.
-   Acciones disponibles según el rol.

### 8.2 Consulta de ingresos

Mostrar ventas consolidadas con, como mínimo, fecha, identificador de
origen, fuente, importe y estado relevante. Mostrar datos personales
únicamente cuando sean necesarios y el usuario tenga permiso para
consultarlos.

### 8.3 Funciones de consulta

-   Búsqueda por texto.
-   Filtros por periodo, usuario, tipo, origen y estado.
-   Ordenamiento por fecha y valor.
-   Paginación del lado del servidor cuando el volumen lo requiera.
-   Totales del conjunto filtrado, no solo de la página visible.
-   Exportación a CSV o Excel, si la plataforma ya dispone de esta
    capacidad.
-   Acceso al detalle desde cada KPI o gráfico.
-   Indicadores de carga, error, sin resultados y filtros activos.

La exportación debe respetar permisos y filtros aplicados, y no debe
exponer información que el usuario no pueda consultar en pantalla.

------------------------------------------------------------------------

## 9. Control administrativo de descuentos y comisiones: Por cubrir y Cubierto

El Administrador debe disponer de control integral sobre los registros
existentes en los apartados **Por cubrir** y **Cubierto** del sitio
administrador.

Antes de modificar esta funcionalidad, revisar las reglas, tablas,
estados, permisos y acciones actuales para preservar su comportamiento.

### Capacidades requeridas

-   Consultar todos los registros de ambas secciones.
-   Filtrar por fecha, usuario, estado, tipo y demás campos disponibles.
-   Ver el detalle completo del registro.
-   Crear, editar, corregir, cambiar de estado o anular cuando la regla
    de negocio lo permita.
-   Consultar quién creó o modificó un registro y cuándo.
-   Registrar motivo en cambios sensibles.
-   Mantener historial de cambios de estado y de valores.
-   Evitar que un mismo registro quede simultáneamente en estados
    incompatibles.
-   Confirmar operaciones críticas antes de ejecutarlas.
-   Reflejar los cambios de forma consistente en las vistas actuales y
    en los nuevos KPI, si dichos conceptos forman parte del cálculo
    financiero aprobado.

**Precaución:** no sumar automáticamente descuentos o comisiones como
gastos o ingresos hasta verificar qué representan actualmente. Se debe
documentar si son conceptos informativos, valores por pagar, valores ya
pagados o movimientos que ya están incluidos en las ventas. Esto evita
duplicar importes.

------------------------------------------------------------------------

## 10. Modelo de datos propuesto

Los nombres son sugerencias y deben adaptarse a las convenciones
existentes.

### 10.1 Tabla `TiposGasto`

  --------------------------------------------------------------------------
  Campo                      Tipo conceptual         Reglas
  -------------------------- ----------------------- -----------------------
  `Id`                       Entero/identificador    PK

  `Nombre`                   Texto                   Obligatorio; único
                                                     según regla definida

  `Descripcion`              Texto nullable          Opcional

  `Activo`                   Booleano                Por defecto verdadero

  `RequiereObservacion`      Booleano                Por defecto verdadero o
                                                     según configuración

  `FechaCreacion`            Fecha/hora              Obligatoria

  `UsuarioCreacionId`        Identificador           Usuario autenticado

  `FechaActualizacion`       Fecha/hora nullable     Se actualiza al
                                                     modificar

  `UsuarioActualizacionId`   Identificador nullable  Usuario que modifica
  --------------------------------------------------------------------------

### 10.2 Tabla `Gastos`

  --------------------------------------------------------------------------
  Campo                      Tipo conceptual         Reglas
  -------------------------- ----------------------- -----------------------
  `Id`                       Identificador           PK

  `TipoGastoId`              Identificador           FK a `TiposGasto`

  `FechaGasto`               Fecha/hora              Obligatoria

  `Valor`                    Decimal monetario       Mayor que cero

  `Observacion`              Texto                   Según configuración del
                                                     tipo

  `UsuarioRegistroId`        Identificador           Usuario autenticado

  `Estado`                   Texto/catálogo          Activo, anulado u otros
                                                     estados aprobados

  `MotivoAnulacion`          Texto nullable          Obligatorio al anular

  `FechaCreacion`            Fecha/hora              Auditoría

  `FechaActualizacion`       Fecha/hora nullable     Auditoría

  `UsuarioActualizacionId`   Identificador nullable  Auditoría
  --------------------------------------------------------------------------

### 10.3 Historial de auditoría

Crear una tabla o mecanismo equivalente para guardar los cambios
relevantes:

-   Entidad y registro afectado.
-   Acción realizada.
-   Valores anteriores y nuevos, cuando aplique.
-   Motivo.
-   Usuario responsable.
-   Fecha y hora.
-   Identificador de correlación de la operación, si la arquitectura lo
    soporta.

El historial debe estar protegido contra modificaciones por usuarios
comunes. La estrategia concreta debe ajustarse al patrón de auditoría ya
utilizado por el sistema.

### 10.4 Integridad y relaciones

-   Definir claves foráneas e índices de acuerdo con la base de datos
    real.
-   Índices potenciales para fecha, tipo, usuario y estado, validados
    con el plan de ejecución.
-   Usar borrado lógico para registros financieros.
-   Aplicar migraciones controladas y probarlas primero en un entorno no
    productivo.
-   No alterar las tablas de ventas existentes salvo que sea
    indispensable y esté aprobado.

------------------------------------------------------------------------

## 11. API y arquitectura

Ajustar las rutas y convenciones al proyecto existente. Como referencia,
la API podría exponer operaciones equivalentes a:

-   `GET /api/utilidades/resumen`
-   `GET /api/utilidades/ingresos`
-   `GET /api/utilidades/gastos`
-   `POST /api/utilidades/gastos`
-   `GET /api/utilidades/gastos/{id}`
-   `PUT /api/utilidades/gastos/{id}`
-   `POST /api/utilidades/gastos/{id}/anular`
-   `GET /api/utilidades/tipos-gasto`
-   `POST /api/utilidades/tipos-gasto` --- solo Administrador
-   `PUT /api/utilidades/tipos-gasto/{id}` --- solo Administrador
-   `PATCH /api/utilidades/tipos-gasto/{id}/estado` --- solo
    Administrador
-   Endpoints administrativos equivalentes para consultar y administrar
    los registros de Por cubrir y Cubierto.

Las rutas son ilustrativas. Reutilizar autenticación, autorización,
manejo de errores, validaciones, logging y patrones de respuesta
existentes.

### Requisitos técnicos

-   Separar lógica de negocio, acceso a datos y controladores/endpoints.
-   Validar autorización en cada operación.
-   Evitar consultas N+1 y cargas completas innecesarias.
-   Usar paginación para listados grandes.
-   Mantener filtros y cálculos en el servidor para garantizar
    consistencia.
-   Usar transacciones cuando una operación modifique varios registros
    relacionados.
-   Evitar que la API confíe en el usuario enviado por el cliente.
-   Registrar errores técnicos sin filtrar información sensible al
    usuario final.

------------------------------------------------------------------------

## 12. Requisitos no funcionales

### Seguridad

-   Autenticación obligatoria mediante el mecanismo actual.
-   Autorización por rol y por registro.
-   Validación de entradas y protección contra inyección SQL.
-   Protección de endpoints administrativos.
-   Auditoría de cambios sensibles.
-   No exponer credenciales ni datos sensibles en logs.

### Rendimiento

-   Resumen y filtros deben operar mediante agregaciones en base de
    datos cuando sea posible.
-   Paginación del lado del servidor.
-   Índices definidos a partir de consultas reales.
-   Evitar cálculos independientes y divergentes entre componentes.
-   Definir un objetivo de rendimiento con base en el volumen real y
    medirlo en pruebas.

### Usabilidad y accesibilidad

-   Diseño adaptable a escritorio, tablet y móvil.
-   Etiquetas claras y formatos de moneda consistentes.
-   Contraste adecuado; no comunicar estados únicamente con colores.
-   Confirmaciones para anulación y acciones administrativas.
-   Mensajes de error concretos.
-   Navegación por teclado y etiquetas accesibles.
-   Estados de carga, vacío, error y éxito.
-   Fechas presentadas en la zona horaria de negocio.

### Consistencia

-   Todos los KPI deben utilizar las mismas reglas de
    inclusión/exclusión.
-   Las anulaciones deben reflejarse consistentemente en tablas,
    gráficos, exportaciones y totales.
-   Definir si el panel usa datos en tiempo real o una actualización
    periódica; indicar la última actualización.
-   Garantizar que filtros por fecha incluyan correctamente los límites
    del periodo.

------------------------------------------------------------------------

## 13. Criterios de aceptación

1.  El Administrador y el Vendedor Web ven el elemento **Utilidades**.
2.  El menú actual del Vendedor Web queda agrupado bajo **Ventas** sin
    perder funciones ni permisos.
3.  Ambos perfiles pueden consultar los KPI consolidados permitidos.
4.  Ambos perfiles pueden registrar gastos y el sistema asigna
    automáticamente al usuario autenticado.
5.  Un gasto válido actualiza los indicadores correspondientes y aparece
    en Movimientos.
6.  Los KPI reflejan los gastos de todos los usuarios, no solamente los
    del usuario que inició sesión.
7.  El panel permite filtrar por fechas y usuario; los filtros se
    aplican coherentemente a tarjetas, gráficos y tablas.
8.  La ganancia neta operativa corresponde a ingresos incluidos menos
    gastos activos incluidos.
9.  Si no hay ingresos, el margen no genera división por cero y se
    muestra como no disponible.
10. Los tipos de gasto pueden ser creados, editados, activados y
    desactivados por el Administrador.
11. Los tipos usados no se eliminan físicamente ni desaparecen de
    movimientos históricos.
12. Los registros anulados se conservan para auditoría y no se cuentan
    como movimientos activos.
13. Las ventas de `dbo` y del esquema manual se consolidan con reglas
    documentadas y sin duplicaciones.
14. Los totales consolidados se concilian con los reportes actuales
    antes de liberar la funcionalidad.
15. El Administrador puede administrar los registros de Por cubrir y
    Cubierto conforme a las reglas existentes, con historial de cambios.
16. Descuentos y comisiones no se contabilizan dos veces ni se
    clasifican sin validar su significado.
17. Los usuarios sin permisos no pueden ejecutar operaciones
    administrativas llamando directamente a la API.
18. Los filtros, KPI, gráficos y exportaciones producen resultados
    consistentes para un mismo conjunto de datos.
19. Los cambios sensibles dejan registro de auditoría.
20. La interfaz funciona correctamente en escritorio y dispositivos
    móviles.

------------------------------------------------------------------------

## 14. Plan de implementación sugerido

### Fase 1: descubrimiento y validación

-   Inspeccionar los menús actuales de Administrador y Vendedor Web.
-   Identificar las tablas de ventas de `dbo` y del esquema manual.
-   Revisar los estados y cálculos actuales de ventas, descuentos,
    premios y comisiones.
-   Revisar los módulos Por cubrir y Cubierto.
-   Definir las reglas de inclusión, exclusión y conciliación.
-   Confirmar permisos por rol y volumen esperado.

### Fase 2: datos y servicios

-   Crear el catálogo de tipos de gasto.
-   Crear la tabla de gastos y auditoría.
-   Implementar validaciones, permisos y operaciones de
    registro/edición/anulación.
-   Construir y probar la capa de consolidación de ingresos.
-   Implementar un servicio único para los cálculos de KPI.

### Fase 3: interfaz de usuario

-   Reorganizar el menú del Vendedor Web.
-   Crear Resumen, Registrar gasto y Movimientos.
-   Crear Configuración de gastos para Administrador.
-   Incorporar filtros globales, tarjetas KPI, gráficos y navegación al
    detalle.
-   Adaptar la experiencia a dispositivos móviles.

### Fase 4: administración y conciliación

-   Incorporar el control administrativo de Por cubrir y Cubierto.
-   Implementar auditoría y motivos para acciones sensibles.
-   Comparar resultados con los reportes existentes.
-   Corregir diferencias antes de activar los KPI definitivos.

### Fase 5: pruebas y liberación

-   Pruebas unitarias de fórmulas.
-   Pruebas de integración con todas las fuentes de ventas.
-   Pruebas de permisos por rol.
-   Pruebas de edición, anulación y auditoría.
-   Pruebas con rangos de fecha, cambios de mes y periodos sin
    movimientos.
-   Pruebas de rendimiento con volúmenes representativos.
-   Liberación controlada y seguimiento de errores.

------------------------------------------------------------------------

## 15. Decisiones pendientes antes de programar

1.  Confirmar las tablas y columnas exactas que representan ventas
    válidas en `dbo` y en el esquema manual.
2.  Definir cómo se tratan devoluciones, anulaciones, descuentos,
    premios y otros conceptos dentro de ingresos y resultado.
3.  Confirmar si el Vendedor Web puede editar/anular sus propios gastos
    o si solo puede registrarlos y consultarlos.
4.  Determinar si los gastos requieren aprobación o quedan activos
    inmediatamente.
5.  Precisar las acciones administrativas permitidas sobre Por cubrir y
    Cubierto, preservando las reglas actuales.
6.  Confirmar si la atribución de ventas a un usuario existe de forma
    fiable.
7.  Definir la zona horaria y la frecuencia de actualización de los KPI.
8.  Confirmar si se necesitan adjuntos o soportes para los gastos.
9.  Definir la política de exportación y retención de auditoría.

Estas decisiones no deben bloquear el diseño de la interfaz ni el
catálogo de gastos, pero sí deben resolverse antes de cerrar la lógica
financiera y publicar los indicadores definitivos.

---

## 16. Regla arquitectónica obligatoria: aislamiento del módulo Utilidades

La funcionalidad de **Utilidades debe permanecer separada del resto del sistema** en todo lo relacionado con nuevas estructuras y lógica de negocio. Esta separación no significa duplicar las fuentes existentes de ventas: el módulo debe leer y consolidar la información de las tablas actuales, pero guardar sus nuevos registros en estructuras propias.

### 16.1 Base de datos y esquema

- Crear un esquema de base de datos independiente llamado **`utilidades`** para las nuevas tablas propias del módulo.
- Las nuevas tablas para gastos, tipos de gasto, auditoría y cualquier otro dato que se cree específicamente para esta funcionalidad deben pertenecer a ese esquema; por ejemplo, `utilidades.Gastos`, `utilidades.TiposGasto` y `utilidades.AuditoriaCambios`.
- No crear tablas nuevas de Utilidades dentro de `dbo` ni dentro de los esquemas existentes del negocio.
- No mover, renombrar, modificar ni duplicar las tablas existentes de ventas para implementar esta funcionalidad.
- Los ingresos se deben consultar desde las tablas de ventas existentes, incluidas las fuentes del esquema `dbo` y del esquema manual. La capa de acceso de Utilidades podrá consultar esas fuentes en modo lectura y consolidarlas según las reglas definidas en este documento.
- Cuando se necesite registrar un dato nuevo propio del módulo —por ejemplo, un gasto o un tipo de gasto—, debe almacenarse exclusivamente en las tablas del esquema `utilidades`.
- Definir las claves foráneas y relaciones entre esquemas solo después de revisar el modelo real y confirmar que son compatibles con la arquitectura y las reglas actuales. No introducir dependencias que alteren el funcionamiento de los módulos existentes.
- Si la plataforma utiliza migraciones, estas deben crear y mantener el esquema `utilidades` y sus tablas sin modificar accidentalmente las tablas ajenas al módulo.

### 16.2 Controladores del API

Los controladores nuevos deben identificarse claramente como parte de Utilidades usando la convención **`[Nombre]UtilidadesController`**. Ejemplos:

- `GastosUtilidadesController`
- `TiposGastoUtilidadesController`
- `ResumenUtilidadesController`
- `MovimientosUtilidadesController`

Los nombres son ejemplos y deben adaptarse a la organización del API existente, conservando la palabra `Utilidades` en el nombre del controlador. Las rutas también deben agruparse bajo una ruta propia, por ejemplo, `/api/utilidades/...`, sin apropiarse ni cambiar rutas de controladores existentes.

La lógica nueva debe mantenerse en servicios, repositorios o componentes propios del módulo Utilidades, con dependencias explícitas hacia las fuentes existentes únicamente para consultar los datos necesarios. Evitar mezclar la lógica de Utilidades dentro de controladores existentes de Ventas u otros módulos.

### 16.3 Criterios de aceptación adicionales

21. Todas las tablas nuevas que almacenan datos propios de Utilidades se crean en el esquema `utilidades`.
22. Las ventas se leen de las tablas existentes y no se duplican en el esquema `utilidades` para alimentar los KPI.
23. Los gastos, tipos de gasto y registros de auditoría nuevos se guardan en las tablas propias de `utilidades`.
24. Los controladores nuevos cumplen la convención `[Nombre]UtilidadesController` y sus rutas se agrupan bajo `/api/utilidades/` o la convención equivalente ya establecida.
25. La implementación no cambia el comportamiento ni la estructura de las tablas de ventas existentes.
26. Las migraciones y pruebas verifican que los objetos nuevos se creen en el esquema correcto y que las consultas a las fuentes existentes sigan funcionando.
