# Requerimiento de cambios --- Sitio web MVC para vendedor y módulo "Revisada manual"

## 1. Objetivo

Implementar un nuevo sitio web para vendedores utilizando MVC,
independiente del sitio web MVC que actualmente utiliza el
administrador. El nuevo sitio debe permitir al vendedor realizar las
operaciones que actualmente realiza desde la aplicación móvil del PDA,
conservando la lógica de negocio existente y adaptando únicamente la
interfaz y los flujos que deban cambiar por tratarse de una aplicación
web.

Adicionalmente, se requiere ampliar la parametrización de líneas de
juego por vendedor e incorporar en el administrador un módulo llamado
**Revisada manual**, con las vistas **Resumen**, **Por cubrir** y
**Cubierto**.

Antes de desarrollar, revisar la arquitectura, las entidades, las reglas
de negocio, los permisos y los flujos actuales del sistema. Reutilizar
los servicios y la lógica existentes siempre que sea posible; no
duplicar reglas de negocio ni alterar el comportamiento actual del PDA o
del administrador fuera de lo solicitado.

------------------------------------------------------------------------

## 2. Nuevo sitio web MVC para el vendedor

### 2.1. Separación del sitio del administrador

-   Crear un sitio web MVC para el vendedor, separado del sitio MVC del
    administrador.
-   El sitio del vendedor debe tener su propia estructura de navegación
    y sus vistas, adaptadas a una aplicación web.
-   El sitio debe conservar la lógica de negocio y las operaciones que
    actualmente están disponibles para el vendedor en la aplicación
    móvil del PDA.
-   La interfaz debe diseñarse específicamente para web; no se debe
    copiar la vista del administrador ni limitarse a mostrar la interfaz
    móvil dentro del navegador.
-   El sitio debe estar restringido al rol de vendedor. Un usuario
    administrador no debe obtener acceso a este sitio por el solo hecho
    de tener acceso al sitio administrativo, y un usuario vendedor no
    debe poder ingresar al sitio del administrador, salvo que las reglas
    actuales de autorización contemplen expresamente algún permiso
    adicional.
-   Validar los permisos tanto en la navegación como en el servidor.
    Ocultar una opción del menú no debe ser la única medida de
    seguridad.
-   Reutilizar la autenticación, las validaciones y los servicios
    existentes cuando corresponda, manteniendo la separación de los
    sitios y sus permisos.

### 2.2. Funcionalidades del vendedor

El nuevo sitio debe contar con un menú propio que incluya las mismas
opciones funcionales que actualmente tiene el vendedor en el PDA,
adaptadas a la interfaz web.

-   Incluir la opción de **Alertas**, con el comportamiento que tiene
    actualmente en el PDA.
-   Incluir las demás opciones que ya utiliza el vendedor en la
    aplicación móvil, conservando su lógica y reglas de negocio.
-   No generar ni imprimir tirillas desde el sitio web del vendedor, ya
    que este flujo no contará con impresora. Revisar el proceso actual
    de registro de una jugada para evitar que dependa de la generación o
    impresión de una tirilla.
-   No modificar el funcionamiento de la aplicación móvil del PDA como
    consecuencia de estos cambios, salvo que sea indispensable y esté
    justificado por una regla compartida. En ese caso, preservar su
    comportamiento actual.

### 2.3. Parametrización de líneas de juego por vendedor

Actualmente, el administrador puede parametrizar la cantidad de líneas
de juego que se pueden crear desde el PDA. Se requiere ampliar esta
configuración para permitir también parametrizar la cantidad máxima de
líneas de juego que puede crear un vendedor desde el nuevo sitio web.

Requisitos:

-   Incorporar en la configuración del administrador un parámetro para
    definir la cantidad máxima de líneas por juego permitidas en el
    sitio web del vendedor.
-   Mantener independiente este parámetro del que ya existe para el PDA.
-   Aplicar el límite al crear o modificar un juego desde el sitio web
    del vendedor, según corresponda al flujo actual.
-   Validar el límite en el servidor; la validación de la interfaz no es
    suficiente.
-   Si el vendedor alcanza el límite configurado, impedir que agregue
    más líneas y mostrar un mensaje claro.
-   Conservar el comportamiento y el límite actual del PDA.

### 2.4. Opción "Entrega de premio" en el menú del vendedor

Agregar al menú del sitio web del vendedor una opción llamada **Entrega
de premio**.

Esta opción debe permitir consultar y registrar la información de la
entrega de premios. Debe incluir, como mínimo, los siguientes datos:

-   Valor jugado.
-   Valor ganado.
-   Fecha del sorteo.
-   Fecha de entrega.
-   Registro de quién entrega el premio.

La información debe guardarse y relacionarse con los registros
correspondientes de acuerdo con el modelo de datos existente. Antes de
crear nuevas tablas o campos, revisar si ya existen entidades o
registros que permitan almacenar esta información.

------------------------------------------------------------------------

## 3. Nuevo módulo administrativo: "Revisada manual"

Agregar en el menú principal del administrador un nuevo ítem llamado
**Revisada manual**, ubicado al mismo nivel que los ítems principales
actuales.

El módulo tendrá tres subítems:

1.  **Resumen**
2.  **Por cubrir**
3.  **Cubierto**

Cada vista debe respetar el diseño visual y los componentes generales
que ya utiliza el administrador, incluyendo tarjetas de indicadores,
filtros, tablas y controles de paginación cuando correspondan.

### 3.1. Subítem "Resumen"

#### Objetivo

Mostrar las jugadas pendientes que se van a cubrir, agrupadas por
lotería y número jugado, y permitir al administrador ejecutar la acción
de cubrir sobre los registros que efectivamente se muestran en pantalla.

Los datos de esta vista son los que el vendedor ingresa en **Registrar
juego** del sitio web del vendedor. Entran los dos tipos de registro:
**Por fila** y **Juego números**. Cada registro aporta su lotería, su
número, su valor y la fecha y hora en que se registró. La forma de
agrupar, filtrar, actualizar y cubrir sigue siendo la de esta vista.

#### Encabezado y filtros

- Mostrar tarjetas o cuadros con información relevante, siguiendo el patrón visual de las demás vistas administrativas.
- Incluir un filtro por fecha para consultar las jugadas del período seleccionado.
- Agregar un campo llamado **Valor mínimo para visualizar**, junto con los demás filtros de la parte superior. Este campo permite al administrador definir el valor mínimo de apuestas acumuladas que debe tener una combinación de lotería y número jugado para aparecer en las tablas de esta vista.
- Los indicadores y los resultados deben corresponder a los filtros aplicados.

#### Filtro por valor mínimo para visualizar

- El administrador podrá ingresar un valor monetario mínimo, por ejemplo, **$10.000**.
- El sistema debe evaluar el **valor de apuesta de cada registro individual**, respetando los filtros aplicados, incluido el rango de fechas.
- Mostrar las jugadas cuyo valor individual sea **igual o superior** al valor mínimo ingresado.
- No sumar los valores de varias apuestas para decidir si deben mostrarse. Cada apuesta se evalúa por separado.
- Mostrar la lotería y el número jugado asociados a cada apuesta que cumpla la condición.
- Las apuestas cuyo valor individual sea inferior al mínimo no deben mostrarse en esta vista mientras el filtro esté aplicado, pero sus registros no deben eliminarse ni cambiar de estado en la base de datos.
- Permitir modificar el valor mínimo para consultar otras jugadas. El filtro debe combinarse con los demás filtros existentes.
- Validar el valor ingresado para impedir valores negativos o entradas inválidas.

**Ejemplo:** si el valor mínimo configurado es **$10.000**, una apuesta individual de $11.000 para la lotería Medellín, número 1234, debe mostrarse. Una apuesta individual de $8.000 para la lotería Medellín, número 5678, no debe mostrarse, aunque existan otras apuestas para esa misma lotería o número. Si el número 1234 se juega en Bogotá, cada apuesta se evalúa individualmente según su propio valor.

**Importante:** este filtro solo determina qué jugadas se visualizan; no modifica ni elimina registros. Se mantiene la regla de que el botón **Por cubrir** procesa exclusivamente los registros concretos que estaban incluidos en el grupo visible al momento de ejecutar la operación. Los registros no visibles no deben incorporarse a una operación ya iniciada. La actualización automática cada minuto debe refrescar la vista según los filtros vigentes, sin alterar operaciones ya ejecutadas.
#### Agrupación y presentación

-   Agrupar los registros por lotería y número jugado.
-   Cada grupo debe mostrar claramente la lotería y el número por el que
    se agrupan los registros.
-   Dentro de cada grupo, listar los valores apostados por los clientes,
    con separador de miles, y la hora correspondiente a cada registro.
-   Mostrar el total de los valores agrupados.
-   Incluir un botón **Por cubrir** para ejecutar la acción sobre ese
    grupo.
-   Si una misma lotería tiene varios números jugados, mostrar un grupo
    independiente por cada número.
-   Los grupos pertenecientes a una misma lotería deben estar
    visualmente contenidos en una misma sección, tarjeta o contenedor
    para que se identifique que pertenecen a esa lotería. Cuando el
    espacio lo permita, los grupos de números de una misma lotería
    pueden presentarse uno al lado del otro.

#### Ejemplo ilustrativo

**Lotería:** Medellín

**Número jugado:** 1234

       Valor apostado Hora
  ------------------- ------------
                5.000 8:50 a. m.
                6.000 3:00 p. m.
    **Total: 11.000** 

**Botón:** `Por cubrir`

#### Regla crítica: cubrir únicamente los registros visibles

Al pulsar el botón **Por cubrir**, el sistema debe cubrir únicamente los
registros que forman parte del grupo mostrado en pantalla en ese
momento.

-   La acción debe trabajar con los identificadores concretos de los
    registros que se mostraron al usuario, no con una consulta nueva y
    abierta que incluya automáticamente registros adicionales.
-   Si existen en la base de datos otras jugadas que todavía no se han
    mostrado en pantalla, esas jugadas no deben incluirse en la
    operación actual.
-   Los registros cubiertos deben corresponder exactamente al conjunto
    visible asociado al botón seleccionado.
-   La actualización automática de la vista debe ejecutarse cada **1
    minuto**.
-   Si aparecen nuevos registros después de la última actualización,
    estos deben incorporarse a la vista cuando corresponda, pero no
    deben añadirse retroactivamente a una operación de cubrir que ya se
    inició.
-   Implementar la operación de forma segura en el servidor, validando
    los identificadores recibidos, su estado actual y los permisos del
    administrador. Evitar que una doble solicitud o un clic repetido
    procese registros de forma inconsistente.
-   Al terminar la operación, actualizar el estado de los registros y
    refrescar la información visible para que el administrador pueda
    identificar cuáles continúan pendientes.

Esta regla es fundamental: **no se deben cubrir registros que no estaban
incluidos en el conjunto mostrado al momento de ejecutar la acción**.

------------------------------------------------------------------------

### 3.2. Subítem "Por cubrir"

#### Objetivo

Mostrar todos los registros que fueron procesados mediante la acción
**Por cubrir** de la vista anterior y que se encuentran en el estado
correspondiente a esta etapa del flujo.

#### Encabezado y filtros

-   Incluir un filtro por fecha.
-   Incluir un campo llamado **Comisión**, cuyo porcentaje debe ingresar
    el administrador.
-   Aplicar la comisión a los valores de los grupos que se muestran en
    esta vista.
-   Mostrar los registros agrupados por lotería y número jugado, con una
    presentación similar a la vista **Resumen**.

#### Presentación de la información

-   Mostrar un contenedor principal por lotería.
-   Dentro de cada contenedor, mostrar tarjetas o cuadros independientes
    para los números cubiertos.
-   Para cada número, mostrar:
    -   Número jugado.
    -   Total de los valores cubiertos.
    -   Porcentaje de comisión aplicado.
    -   Valor monetario de la comisión.
    -   Total resultante de sumar el subtotal y la comisión.
-   Incluir el botón **Cubrir** para continuar con el siguiente paso del
    proceso.

#### Ejemplo ilustrativo

**Número jugado:** 1234

-   Total: **11.000**
-   Comisión: **50 %**
-   Valor de la comisión: **5.500**
-   Total con comisión: **16.500**

**Botón:** `Cubrir`

#### Reglas de cálculo

-   El valor de la comisión debe calcularse a partir del total
    correspondiente al grupo y del porcentaje ingresado por el
    administrador.
-   El total con comisión debe ser la suma del subtotal y el valor de la
    comisión.
-   Mostrar los porcentajes y los valores monetarios de forma clara,
    usando separador de miles para los importes.
-   Validar el valor ingresado para evitar porcentajes inválidos.
-   La comisión utilizada debe quedar registrada junto con la operación
    correspondiente para mantener la trazabilidad de los cálculos.
-   El botón **Cubrir** debe procesar únicamente el grupo o los
    registros seleccionados, respetando el estado actual y evitando
    procesamientos duplicados.

------------------------------------------------------------------------

### 3.3. Subítem "Cubierto"

#### Objetivo

Mostrar los grupos que avanzaron desde la vista **Por cubrir** al pulsar
el botón **Cubrir** y calcular el descuento que se aplicará al subtotal
con comisión para obtener el total a pagar.

#### Encabezado y filtros

-   Incluir un filtro por fecha.
-   Incluir un campo llamado **Descuento**, cuyo porcentaje debe
    ingresar el administrador.
-   Mostrar la información agrupada por lotería y número jugado,
    siguiendo una presentación visual similar a la vista **Por cubrir**.

#### Presentación de la información

Para cada grupo, mostrar:

-   Lotería.
-   Número jugado.
-   Subtotal con comisión, calculado en la etapa anterior.
-   Porcentaje de descuento ingresado por el administrador.
-   Valor monetario del descuento.
-   Total a pagar, después de aplicar el descuento.

#### Ejemplo ilustrativo

-   Subtotal anterior: **16.500**
-   Descuento: **10 %**
-   Valor del descuento: **1.650**
-   **Total a pagar: 14.850**

El ejemplo anterior aplica matemáticamente un descuento del 10 % sobre
16.500. Si se utiliza el valor de 15.300 como resultado esperado, se
debe confirmar una regla de cálculo diferente, ya que ese resultado
equivale a un descuento del 7,2727 % aproximadamente.

#### Reglas de cálculo

-   Calcular el descuento sobre el subtotal con comisión registrado en
    la etapa anterior.
-   Calcular el total a pagar restando el valor del descuento al
    subtotal con comisión.
-   Registrar el porcentaje y el valor monetario del descuento, así como
    el total resultante.
-   Validar el porcentaje para evitar valores inválidos.
-   Mantener la trazabilidad de los valores originales y de cada
    operación aplicada.
-   Evitar que un mismo grupo sea procesado más de una vez por
    solicitudes repetidas.
-   Conservar los datos y estados de las etapas anteriores; no
    sobrescribir los valores originales de las jugadas.

------------------------------------------------------------------------

## 4. Estados y trazabilidad del flujo de revisión manual

El flujo funcional esperado es:

1.  Las jugadas registradas en **Registrar juego** del sitio web del
    vendedor, tanto **Por fila** como **Juego números**, aparecen
    pendientes en **Resumen**.
2.  Al ejecutar **Por cubrir**, se registran únicamente los elementos
    visibles que correspondan al grupo seleccionado y pasan al estado de
    **Por cubrir**.
3.  En **Por cubrir**, el administrador ingresa la comisión y puede
    continuar mediante el botón **Cubrir**.
4.  Los grupos procesados pasan a **Cubierto**, donde el administrador
    ingresa el descuento y consulta el total a pagar.

Antes de implementar, revisar los estados y entidades existentes para
determinar si pueden reutilizarse. Si no existen estados adecuados,
definirlos de forma explícita y consistente. Cada transición debe
guardar, como mínimo, la fecha y hora de la operación, el usuario
administrador que la ejecutó, los registros incluidos y los valores
porcentuales y monetarios aplicados.

No asumir que una jugada está cubierta solo porque aparece en una vista.
El estado debe persistirse en el servidor y ser la fuente de verdad para
todas las vistas.

------------------------------------------------------------------------

## 5. Reglas generales de implementación

-   Revisar primero la solución actual, las convenciones del proyecto,
    las rutas, controladores, vistas, servicios, modelos, tablas y
    mecanismos de autorización.
-   Reutilizar la lógica de negocio existente siempre que corresponda.
-   Mantener separado el sitio MVC del vendedor del sitio MVC del
    administrador.
-   No duplicar cálculos ni reglas de negocio en las vistas. Los
    cálculos deben implementarse en una capa apropiada del servidor y
    validarse allí.
-   Aplicar autorización en cada endpoint y operación sensible.
-   Utilizar transacciones cuando una acción cambie el estado de varios
    registros o registre cálculos relacionados.
-   Evitar procesamientos duplicados ante doble clic, reintentos o
    solicitudes concurrentes.
-   Usar paginación del lado del servidor cuando el volumen de
    información lo requiera.
-   Los filtros por fecha deben tener límites claros e incluir
    correctamente los registros del rango seleccionado.
-   Las fechas guardadas en el sistema y bd deben tener formato hora colombia en forma DD-MM-YYY HH:MM:ss.
-   Ordenar las columnas de forma ascendente y descendente sin perder
    los filtros activos.
-   Mostrar importes con separador de miles y mantener precisión
    monetaria consistente en los cálculos.
-   Validar los campos de comisión y descuento en el servidor, además de
    hacerlo en la interfaz.
-   Mostrar mensajes claros para operaciones exitosas, errores de
    validación y conflictos de estado.
-   No generar ni imprimir tirillas desde el sitio web del vendedor.
-   No modificar funcionalidades existentes fuera del alcance descrito.
-   No inventar datos ni mostrar valores de ejemplo como si fueran
    registros reales.

------------------------------------------------------------------------

## 6. Criterios de aceptación

### Sitio web del vendedor

-   [ ] Existe un sitio MVC del vendedor separado del sitio del
    administrador.
-   [ ] Solo los usuarios autorizados como vendedores pueden ingresar al
    sitio del vendedor.
-   [ ] El menú del vendedor contiene las opciones funcionales que
    existen actualmente en el PDA, incluida **Alertas**.
-   [ ] Las operaciones conservan las reglas de negocio existentes.
-   [ ] El sitio web del vendedor no genera ni imprime tirillas.
-   [ ] El administrador puede configurar un límite de líneas de juego
    independiente para el sitio web del vendedor.
-   [ ] El límite de líneas se valida tanto en la interfaz como en el
    servidor.
-   [ ] Existe la opción **Entrega de premio** con valor jugado, valor
    ganado, fecha del sorteo, fecha de entrega y registro de quién
    entrega el premio.

### Módulo "Revisada manual"

-   [ ] El administrador ve **Revisada manual** en el menú principal.
-   [ ] El módulo contiene **Resumen**, **Por cubrir** y **Cubierto**.
-   [ ] **Resumen** muestra las jugadas de **Registrar juego** del sitio
    del vendedor, de **Por fila** y de **Juego números**, agrupadas por
    lotería y número, con importes, horas y totales.
-   [ ] La vista **Resumen** permite ingresar un valor mínimo de visualización y muestra las apuestas individuales cuyo valor sea igual o superior al valor configurado, junto con su lotería y número jugado, respetando los filtros de fecha.
-   [ ] El filtro por valor mínimo evalúa cada apuesta individualmente, sin sumar varias apuestas, y solo afecta la visualización; no modifica los registros ni permite incluir jugadas no visibles en una operación de cubrir.
-   [ ] La vista **Resumen** se actualiza automáticamente cada minuto.
-   [ ] La acción **Por cubrir** procesa únicamente los registros
    incluidos en el grupo visible al momento de la operación.
-   [ ] Los registros que no estaban incluidos en el conjunto visible no
    se incorporan a una operación ya iniciada.
-   [ ] **Por cubrir** permite ingresar una comisión y muestra subtotal,
    porcentaje, valor de comisión y total con comisión.
-   [ ] **Cubierto** permite ingresar un descuento y muestra subtotal,
    porcentaje, valor del descuento y total a pagar.
-   [ ] Los porcentajes, importes, estados, fechas y usuario que ejecuta
    cada operación quedan registrados para auditoría.
-   [ ] Las operaciones no duplican registros ni avanzan grupos que ya
    estén en un estado incompatible.
-   [ ] Las vistas muestran datos reales y actualizados de acuerdo con
    los filtros y los estados persistidos.

------------------------------------------------------------------------

## 7. Verificaciones y pruebas requeridas

Implementar y ejecutar pruebas para verificar, como mínimo:

1.  Acceso autorizado y denegado a los sitios de vendedor y
    administrador.
2.  Conservación de las operaciones y reglas actuales del PDA.
3.  Límite de líneas independiente para PDA y sitio web del vendedor.
4.  Registro y consulta de entregas de premios.
5.  Agrupación correcta por lotería y número en **Resumen**.
6.  Actualización automática cada minuto de la vista **Resumen**.
7.  Confirmación de que una operación solo cubre los identificadores
    visibles asociados al grupo seleccionado.
8.  Confirmación de que registros nuevos o no visibles no se agregan a
    una operación ya iniciada.
9.  Cálculo correcto de la comisión y del total con comisión.
10. Cálculo correcto del descuento y del total a pagar.
11. Prevención de operaciones duplicadas ante doble clic, reintentos o
    solicitudes concurrentes.
12. Persistencia de los estados y trazabilidad de las operaciones.
13. Verificación de que el sitio web del vendedor no genere tirillas.
14. Regresión de las funcionalidades actuales del administrador y del
    PDA.

------------------------------------------------------------------------

## 8. Indicaciones para ejecutar el desarrollo con Cursor

Trabaja sobre la solución existente y sigue las convenciones actuales
del repositorio. Antes de modificar archivos, identifica cómo están
implementados la autenticación, los roles, el menú administrativo, la
lógica de jugadas, la configuración de líneas del PDA, las alertas y las
entidades relacionadas con premios.

Implementa el requerimiento por etapas, reutilizando los servicios
existentes y evitando cambios innecesarios. No reemplaces
funcionalidades existentes ni elimines lógica que pueda seguir siendo
utilizada por el PDA. Si encuentras una regla de negocio o una
estructura de datos que no permite cumplir el requerimiento de forma
segura, documenta el hallazgo y propone el cambio mínimo necesario.

Al finalizar, entrega un resumen de los archivos modificados, las nuevas
rutas y vistas, los cambios de base de datos o migraciones, las reglas
de autorización implementadas y las pruebas ejecutadas. Señala
explícitamente cualquier punto que haya quedado pendiente de
confirmación.
