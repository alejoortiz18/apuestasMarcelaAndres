# Requerimiento funcional y técnico: Nuevo módulo de Recaudo

Proyecto: Sistema de apuestas y loterías
Módulo: Recaudo
Aplicación operativa: APK Android para recaudadores
Administración: Nuevas vistas en el sitio web administrativo.

## 1. Contexto actual

Actualmente, el sistema de apuestas se encuentra desarrollado y funcionando en producción.

Cuenta con:

- Vendedores que realizan ventas de loterías mediante un PDA.
- Dispositivos PDA asignados a los vendedores.
- Grupos existentes a los que pertenecen los vendedores.
- Administrador encargado de gestionar la operación.
- Registro de ventas almacenado en la base de datos actual.

Se incorporará un nuevo perfil denominado Recaudador, cuya función principal será recoger el dinero correspondiente a las ventas realizadas por los vendedores.

El dinero que debe entregar cada vendedor se calculará mediante un porcentaje configurable sobre sus ventas diarias.

La diferencia entre el valor que debe entregar y el dinero efectivamente recibido se conservará como saldo pendiente, acumulándose en los siguientes días hasta su cancelación.

El nuevo módulo deberá integrarse con la información existente de vendedores, grupos y ventas, sin modificar las estructuras ni los procesos actuales que se encuentran en producción.

## 2. Objetivos del nuevo módulo

1. Crear un nuevo perfil denominado Recaudador.
2. Permitir al administrador configurar individualmente los grupos y vendedores asignados.
3. Configurar porcentajes de recaudo por grupo.
4. Calcular automáticamente las obligaciones de cada vendedor.
5. Registrar pagos completos y parciales.
6. Acumular automáticamente las deudas pendientes.
7. Consultar el estado de recaudo por vendedor, grupo y recaudador.
8. Consultar históricos de pagos y deudas.
9. Visualizar indicadores y gráficos de comportamiento.
10. Desarrollar una APK independiente para el recaudador.
11. Incorporar nuevas vistas administrativas.
12. Mantener aisladas las nuevas estructuras de base de datos, procedimientos, controladores y vistas.

## 3. Nuevo perfil: Recaudador

### 3.1. Creación del usuario

Se reutilizará el método existente de creación de usuarios y perfiles.

Al crear un usuario con el perfil Recaudador:

- No será obligatorio asignarlo a un grupo.
- No se exigirá seleccionar grupos durante la creación.
- La asignación de grupos y vendedores será responsabilidad exclusiva de `configRecaudo`.
- Tendrá acceso únicamente a las funcionalidades correspondientes a su perfil.

### 3.2. Función principal

El recaudador podrá:

- Consultar los grupos asignados.
- Consultar los vendedores y personas asignadas por el administrador.
- Consultar las ventas diarias.
- Visualizar los valores por cobrar.
- Registrar el dinero recibido.
- Confirmar los cobros.
- Consultar saldos pendientes.
- Consultar historiales.
- Visualizar métricas y gráficos.

# 4. Configuración administrativa: `configRecaudo`

Se creará una nueva sección administrativa para gestionar los parámetros del módulo.

### 4.1. Administración de recaudadores

El administrador podrá:

- Consultar todos los usuarios con perfil Recaudador.
- Seleccionar individualmente un recaudador.
- Asignarle uno o varios grupos.
- Configurar porcentajes por grupo.
- Asignarle vendedores individuales.
- Retirar asignaciones.
- Consultar y modificar configuraciones.

Cada recaudador tendrá una configuración independiente.

### 4.2. Asignación de grupos y porcentajes

El administrador podrá seleccionar un grupo y establecer el porcentaje que se deberá recaudar sobre las ventas diarias de cada vendedor perteneciente a dicho grupo.

El porcentaje se aplicará individualmente a cada vendedor, no sobre la suma global del grupo para distribuirla posteriormente.

#### Ejemplo de configuración

La configuración del grupo deberá mostrar en una misma fila:

- Porcentaje configurado.
- Total por recaudar.
- Total recaudado.

Los tres valores deberán utilizar el mismo tamaño de letra destacado que representa el porcentaje del ejemplo.

## Configuración del grupo

Grupo 1

5 vendedores

Porcentaje

# 80%

Total por recaudar

# $500.000

Total recaudado

# $300.000

Valores ilustrativos del periodo de recaudo.

Los indicadores monetarios se actualizarán automáticamente después de cada pago confirmado.

Porcentaje: Valor configurado por el administrador para calcular las obligaciones individuales.

Total por recaudar: Suma de las obligaciones correspondientes a los vendedores del grupo durante el periodo consultado.

Total recaudado: Suma de los pagos efectivamente registrados para los vendedores del grupo durante el periodo consultado.

Los indicadores deberán actualizarse sin necesidad de recargar manualmente la página.

Los cambios de porcentaje no deberán alterar obligaciones históricas previamente generadas.

### 4.3. Asignación exclusiva de grupos y vendedores

El administrador podrá asignar grupos completos o vendedores individuales a un recaudador.

Reglas obligatorias:

1. Un grupo únicamente podrá estar asignado a un recaudador.
2. Un vendedor únicamente podrá estar asignado a un recaudador.
3. No se permitirá asignar un grupo a varios recaudadores.
4. No se permitirá asignar un vendedor a varios recaudadores.
5. Un vendedor perteneciente a un grupo asignado heredará la asignación de ese grupo.
6. No se permitirá asignar individualmente a otro recaudador un vendedor que ya esté cubierto por un grupo asignado.
7. Tampoco se permitirá asignar un grupo a un recaudador si alguno de sus vendedores ya está asignado individualmente a otro recaudador, salvo que previamente se resuelva la asignación existente.
8. Las restricciones deberán validarse en la interfaz, API y base de datos.
9. Si existe una asignación duplicada, el sistema deberá impedirla y mostrar un mensaje indicando el conflicto.

El sistema deberá evitar que un vendedor aparezca duplicado en las listas de recaudo.

### 4.4. Configuración independiente

Se conservará la relación entre:

- Recaudador.
- Grupo asignado.
- Porcentaje configurado.
- Vendedores individuales asignados.
- Estado de la asignación.
- Fecha de creación y modificación.

Los cambios de configuración deberán preservar los registros históricos.

# 5. Aplicación APK del Recaudador

Se desarrollará una nueva aplicación Android (APK), tomando como referencia las aplicaciones actuales del vendedor y observador.

No se desarrollará un sitio web para el recaudador.

La APK tendrá sus propias pantallas, navegación y funcionalidades, integradas con las nuevas API del módulo Recaudo.

### 5.1. Pantalla principal

Al ingresar, el recaudador visualizará:

- Grupos asignados por el administrador.
- Vendedores y personas asignadas por el administrador.
- Usuarios pendientes de cobro.
- Usuarios cobrados.
- Indicadores generales.
- Opción para consultar todos los usuarios asignados.

### 5.2. Organización por grupo

Cada grupo se visualizará en una sección independiente.

Dentro de cada grupo se mostrarán los vendedores correspondientes.

Columnas mínimas:

|   |
| - |

Campo

|   |
| - |

Descripción

|   |
| - |

Vendedor

|   |
| - |

Nombre del vendedor

|   |
| - |

Total vendido

|   |
| - |

Ventas del día

|   |
| - |

Valor a cobrar

|   |
| - |

Obligación calculada según porcentaje

|   |
| - |

Total pendiente

|   |
| - |

Deuda anterior más obligación actual, descontando pagos aplicados

|   |
| - |

Valor recibido

|   |
| - |

Valor ingresado por el recaudador

|   |
| - |

Estado

|   |
| - |

Pendiente, parcial o cancelado

|   |
| - |

Acción

|   |
| - |

Botón para registrar el cobro

### 5.3. Listas de pendientes, cobrados y todos

Al comenzar su labor, el recaudador visualizará los grupos y las personas a las que debe ir a cobrar.

La aplicación tendrá tres opciones:

- Pendientes por cobrar: usuarios a los que todavía no se les ha registrado el cobro del periodo.
- Cobrados o Recaudados: usuarios a los que ya se les registró un pago durante el periodo.
- Todos: totalidad de vendedores y personas asignadas, independientemente de su estado.

Los usuarios pendientes deberán mostrarse en color verde, indicando que son personas a las que el recaudador debe visitar.

Después de confirmar un pago:

1. El usuario desaparecerá de la lista de pendientes.
2. Aparecerá en la lista de cobrados.
3. Se actualizarán los indicadores del grupo.
4. Se incrementará el total recaudado.
5. Se actualizará el saldo pendiente.
6. Se conservará el historial del pago.

El objetivo es que el recaudador observe cómo disminuye progresivamente su lista de pendientes durante la jornada.

### 5.4. Registro del pago

El campo de valor recibido deberá:

- Permitir valores monetarios.
- Aplicar separadores de miles automáticamente.
- Mostrar pesos colombianos.
- Validar valores numéricos.
- Rechazar valores negativos.
- Permitir pagos parciales.
- Mostrar el saldo pendiente antes de confirmar.

#### Modal de confirmación

Al presionar el botón de cobro, se mostrará un modal con:

- Nombre del vendedor.
- Total a cobrar.
- Valor que se está cobrando.
- Valor que se recibirá.
- Saldo que quedará pendiente después del pago.

El modal tendrá dos botones:

- Aceptar: confirma y registra el pago.
- Cancelar: cierra el modal sin registrar ninguna operación.

El pago únicamente se registrará después de presionar Aceptar.

Al finalizar correctamente, se actualizarán las listas y los indicadores.

# 6. Manejo de deudas y pagos parciales

El sistema conservará los saldos pendientes y los acumulará automáticamente.

### Ejemplo

Día 1

- Debe entregar: $50.000.
- Pagó: $30.000.
- Saldo pendiente: $20.000.

Día 2

- Deuda anterior: $20.000.
- Nuevo valor: $60.000.
- Total pendiente: $80.000.
- Pagó: $50.000.
- Saldo acumulado: $30.000.

El vendedor deberá aparecer en rojo con una deuda acumulada de $30.000.

### Estados visuales

|   |
| - |

Estado

|   |
| - |

Representación

|   |
| - |

Cancelado

|   |
| - |

Verde

|   |
| - |

Pago parcial

|   |
| - |

Amarillo o naranja

|   |
| - |

Pendiente

|   |
| - |

Neutro

|   |
| - |

En deuda

|   |
| - |

Rojo

Cuando el vendedor esté completamente al día, se mostrará Cancelado en verde.

# 7. Panel administrativo de Recaudo

Se desarrollarán nuevas vistas administrativas independientes de las existentes.

### 7.1. Listado general de recaudadores

Columnas:

|   |
| - |

Campo

|   |
| - |

Descripción

|   |
| - |

Recaudador

|   |
| - |

Nombre

|   |
| - |

Grupos asignados

|   |
| - |

Grupos a cargo

|   |
| - |

Personas asignadas

|   |
| - |

Vendedores individuales

|   |
| - |

Total por recaudar

|   |
| - |

Obligaciones del periodo

|   |
| - |

Total recaudado

|   |
| - |

Dinero recibido

|   |
| - |

Saldo pendiente

|   |
| - |

Diferencia

|   |
| - |

Porcentaje recaudado

|   |
| - |

Avance

|   |
| - |

Acción

|   |
| - |

Ver detalle

Ejemplo:

|   |
| - |

Recaudador

|   |
| - |

Grupos

|   |
| - |

Por recaudar

|   |
| - |

Recaudado

|   |
| - |

Avance

|   |
| - |

Recaudador 1

|   |
| - |

Grupo 1, Grupo 2

|   |
| - |

$1.000.000

|   |
| - |

$600.000

|   |
| - |

60%

### 7.2. Detalle del recaudador

El administrador visualizará:

- Información del recaudador.
- Grupos y vendedores asignados.
- Total por recaudar.
- Total recaudado.
- Saldo pendiente.
- Porcentaje de recaudo.
- Historial de movimientos.
- Gráficos.

Cada grupo se mostrará en una sección independiente con sus respectivos vendedores y valores.

### 7.3. Tablas de usuarios y grupos cobrados y pendientes

El administrador dispondrá de dos tablas independientes.

Tabla de cobrados:

- Grupo.
- Vendedor o persona.
- Recaudador responsable.
- Valor que debía pagar.
- Valor recibido.
- Saldo pendiente.
- Fecha y hora del pago.
- Estado.

Tabla de pendientes:

- Grupo.
- Vendedor o persona.
- Recaudador responsable.
- Valor que debe pagar.
- Deuda anterior.
- Total pendiente.
- Estado.

Ambas tablas deberán actualizarse después de cada pago confirmado.

### 7.4. Filtro por grupo

Permitirá consultar:

- Nombre del grupo.
- Recaudador responsable.
- Porcentaje configurado.
- Vendedores.
- Total vendido.
- Total por recaudar.
- Total recaudado.
- Saldo pendiente.
- Historial.

### 7.5. Filtro por vendedor

Permitirá consultar:

- Nombre del vendedor.
- Grupo actual.
- Recaudador asignado.
- Recaudador que registró cada pago histórico.
- Ventas diarias.
- Porcentaje aplicado.
- Valor generado.
- Valor pagado.
- Deuda acumulada.
- Historial completo.

# 8. Historial de vendedores

El administrador y el recaudador podrán consultar los historiales correspondientes a sus permisos.

|   |
| - |

Campo

|   |
| - |

Descripción

|   |
| - |

Fecha

|   |
| - |

Día correspondiente

|   |
| - |

Ventas

|   |
| - |

Total vendido

|   |
| - |

Porcentaje

|   |
| - |

Porcentaje aplicado

|   |
| - |

Valor generado

|   |
| - |

Obligación diaria

|   |
| - |

Deuda anterior

|   |
| - |

Saldo anterior

|   |
| - |

Total por pagar

|   |
| - |

Obligación más deuda anterior

|   |
| - |

Valor pagado

|   |
| - |

Total recibido

|   |
| - |

Saldo pendiente

|   |
| - |

Deuda resultante

|   |
| - |

Recaudador

|   |
| - |

Usuario que recibió el dinero

|   |
| - |

Estado

|   |
| - |

Cancelado, parcial o pendiente

Un vendedor únicamente podrá tener un recaudador asignado a la vez.

El historial deberá conservar quién realizó cada pago, incluso si posteriormente cambia la asignación.

# 9. Gráficos y métricas

### 9.1. Gráfico histórico del vendedor

En la parte superior se mostrará la deuda total acumulada en rojo, con un tamaño de letra dos puntos superior al texto normal.

El gráfico mostrará por día:

- Valor pagado.
- Valor pendiente por pagar.

### 9.2. Gráfico del recaudador por grupo

Mostrará:

- Total que debía recaudar el grupo por día.
- Total recaudado por día.
- Valor pendiente por cobrar.
- Comportamiento histórico.

### 9.3. Métricas generales

El administrador podrá consultar:

- Total vendido.
- Total por recaudar.
- Total recaudado.
- Total pendiente.
- Porcentaje de recaudo.
- Vendedores al día.
- Vendedores en mora.
- Grupos con obligaciones pendientes.
- Recaudo diario por recaudador.
- Recaudo acumulado por grupo.

# 10. Historial de recaudos por recaudador

El administrador podrá filtrar por:

- Recaudador.
- Grupo.
- Vendedor.
- Fecha inicial y final.
- Estado del pago.

Cada movimiento mostrará:

- Fecha y hora.
- Recaudador.
- Vendedor.
- Grupo.
- Valor recibido.
- Obligación afectada.
- Saldo resultante.

El historial se conservará incluso cuando un vendedor o grupo deje de estar asignado al recaudador.

# 11. Integración con el sistema actual de ventas

Antes de implementar los cálculos, se deberá analizar:

1. Tablas donde se almacenan las ventas.
2. Relaciones entre ventas y vendedores.
3. Relaciones entre vendedores y grupos.
4. Fecha de cada venta.
5. Estados de las ventas.
6. Procedimientos almacenados utilizados.
7. Forma correcta de obtener el total vendido por vendedor y día.

Actualmente, las ventas no se anulan, reversan ni modifican.

El módulo Recaudo deberá consultar las ventas existentes y utilizarlas para calcular las obligaciones.

No se deberán duplicar las ventas ni modificar sus tablas.

# 12. Arquitectura de base de datos

### 12.1. Nuevo esquema

Todas las tablas nuevas deberán crearse bajo el esquema:

`recaudo`

### 12.2. Restricción de producción

Está estrictamente prohibido:

- Modificar tablas existentes.
- Eliminar columnas.
- Cambiar relaciones existentes.
- Alterar procedimientos actuales.
- Cambiar la lógica de ventas.
- Modificar vistas actuales.
- Cambiar controladores actuales.
- Alterar el funcionamiento del vendedor u observador.

### 12.3. Entidades nuevas

Se deberán diseñar tablas para:

- Configuración de recaudadores.
- Asignación de grupos.
- Asignación individual de vendedores.
- Porcentajes.
- Obligaciones diarias.
- Pagos.
- Aplicación de pagos.
- Historial de configuración.
- Auditoría.

Se deberá garantizar la exclusividad de asignación mediante validaciones y restricciones adecuadas.

### 12.4. Persistencia histórica

Las obligaciones conservarán:

- Ventas utilizadas.
- Porcentaje aplicado.
- Valor generado.
- Fecha de generación.

Los cambios posteriores de configuración no alterarán las obligaciones históricas.

# 13. Procedimientos almacenados

Todos los procedimientos nuevos utilizarán el prefijo:

`sp_recaudo_`

Ejemplos:

|   |
| - |

Procedimiento

|   |
| - |

Responsabilidad

|   |
| - |

`sp_recaudo_configurar_recaudador`

|   |
| - |

Configurar recaudador

|   |
| - |

`sp_recaudo_asignar_grupo`

|   |
| - |

Asignar grupo

|   |
| - |

`sp_recaudo_asignar_vendedor`

|   |
| - |

Asignar vendedor

|   |
| - |

`sp_recaudo_configurar_porcentaje`

|   |
| - |

Configurar porcentaje

|   |
| - |

`sp_recaudo_consultar_ventas`

|   |
| - |

Consultar ventas

|   |
| - |

`sp_recaudo_generar_obligaciones`

|   |
| - |

Generar obligaciones

|   |
| - |

`sp_recaudo_registrar_pago`

|   |
| - |

Registrar pagos

|   |
| - |

`sp_recaudo_consultar_vendedores`

|   |
| - |

Consultar asignados

|   |
| - |

`sp_recaudo_consultar_grupos`

|   |
| - |

Consultar grupos

|   |
| - |

`sp_recaudo_consultar_historial`

|   |
| - |

Consultar históricos

|   |
| - |

`sp_recaudo_metricas_recaudador`

|   |
| - |

Métricas

|   |
| - |

`sp_recaudo_metricas_grupo`

|   |
| - |

Métricas por grupo

No se modificarán los procedimientos existentes.

# 14. Nuevas API REST

Los nuevos controladores deberán incluir el identificador `Recaudo` en su nombre.

Las rutas utilizarán:

`/api/Recaudo/`

### 14.1. Convención de controladores y rutas

Ejemplo de controlador:

`BoletosRecaudo`

Ejemplo de ruta:

`POST /api/Recaudo/Grupos`

|   |
| - |

Método

|   |
| - |

Controlador

|   |
| - |

Endpoint

|   |
| - |

GET

|   |
| - |

`GruposRecaudo`

|   |
| - |

`/api/Recaudo/Grupos`

|   |
| - |

GET

|   |
| - |

`VendedoresRecaudo`

|   |
| - |

`/api/Recaudo/Vendedores`

|   |
| - |

GET

|   |
| - |

`ObligacionesRecaudo`

|   |
| - |

`/api/Recaudo/Obligaciones`

|   |
| - |

POST

|   |
| - |

`PagosRecaudo`

|   |
| - |

`/api/Recaudo/Pagos`

|   |
| - |

GET

|   |
| - |

`HistorialRecaudo`

|   |
| - |

`/api/Recaudo/Historial/Vendedor/{id}`

|   |
| - |

GET

|   |
| - |

`MetricasRecaudo`

|   |
| - |

`/api/Recaudo/Metricas`

Los controladores existentes no deberán modificarse ni reutilizarse para las nuevas funcionalidades.

### 14.2. API administrativas

Deberán permitir:

- Consultar recaudadores.
- Configurar asignaciones.
- Asignar y retirar grupos.
- Asignar y retirar vendedores.
- Configurar porcentajes.
- Consultar históricos.
- Consultar métricas.
- Consultar usuarios y grupos cobrados.
- Consultar usuarios y grupos pendientes.

### 14.3. API del recaudador

Deberán validar que el recaudador autenticado tenga autorización para consultar el vendedor o grupo solicitado.

La validación se realizará en el servidor, no únicamente en la APK.

# 15. Nuevas vistas y menú

Todos los nuevos elementos del menú deberán identificarse mediante la concatenación `Recaudo`.

Ejemplos:

|   |
| - |

Elemento

|   |
| - |

Identificador

|   |
| - |

Configuración

|   |
| - |

`configRecaudo`

|   |
| - |

Panel

|   |
| - |

`panelRecaudo`

|   |
| - |

Historial

|   |
| - |

`historialRecaudo`

|   |
| - |

Métricas

|   |
| - |

`metricasRecaudo`

|   |
| - |

Detalle

|   |
| - |

`detalleRecaudo`

No se modificarán las vistas existentes del administrador, vendedor u observador.

La APK tendrá sus propias pantallas y navegación.

# 16. Seguridad y permisos

### Administrador

Podrá:

- Crear y administrar configuraciones.
- Asignar grupos y vendedores.
- Configurar porcentajes.
- Consultar todos los recaudos.
- Consultar cualquier vendedor o grupo.
- Consultar históricos y métricas.
- Consultar usuarios y grupos cobrados y pendientes.

### Recaudador

Podrá:

- Consultar sus grupos asignados.
- Consultar sus vendedores asignados.
- Consultar las personas asignadas por el administrador.
- Registrar pagos.
- Consultar movimientos.
- Consultar historiales autorizados.
- Consultar sus métricas.

No podrá modificar configuraciones administrativas ni consultar información de otros recaudadores fuera de sus permisos.

# 17. Reglas técnicas adicionales

### 17.1. Indicadores de carga (Spinner)

Todas las operaciones deberán mostrar un indicador visual de carga mientras se procesan.

Aplica a:

- Consultas.
- Búsquedas.
- Registro de pagos.
- Transacciones.
- Carga de grupos.
- Carga de vendedores.
- Consulta de historiales.
- Actualización de métricas.
- Operaciones administrativas.

El spinner deberá:

1. Aparecer al iniciar la operación.
2. Permanecer visible mientras se espera la respuesta.
3. Desaparecer al finalizar correctamente.
4. Desaparecer si ocurre un error.
5. Evitar ejecuciones duplicadas mientras la operación esté procesándose.

Los errores deberán mostrar mensajes claros.

### 17.2. Formato monetario

Todos los valores expresados en pesos colombianos deberán visualizarse y escribirse con separadores de miles.

|   |
| - |

Valor numérico

|   |
| - |

Visualización

|   |
| - |

1000

|   |
| - |

$1.000

|   |
| - |

25000

|   |
| - |

$25.000

|   |
| - |

150000

|   |
| - |

$150.000

|   |
| - |

1000000

|   |
| - |

$1.000.000

Aplica a campos, tablas, indicadores, modales, historiales, gráficos, resúmenes y totales.

El formato visual no deberá alterar el valor numérico almacenado ni enviado a la API.

### 17.3. Integridad de los pagos

El registro de pagos deberá utilizar operaciones transaccionales para garantizar la consistencia de los movimientos y saldos.

### 17.4. Evitar duplicidad

El sistema deberá impedir:

- Generar dos veces la misma obligación diaria.
- Registrar pagos duplicados por reintentos.
- Asignar grupos o vendedores a varios recaudadores.
- Duplicar vendedores en las listas de recaudo.

### 17.5. Auditoría

Se conservará:

- Usuario que realizó la operación.
- Fecha y hora.
- Tipo de operación.
- Valor registrado.
- Obligación afectada.
- Identificador del movimiento.

### 17.6. Modificaciones posteriores

Por el momento, los pagos registrados no podrán eliminarse ni corregirse.

Los movimientos confirmados deberán conservarse como registros históricos inalterables.

### 17.7. Consistencia de métricas

Los indicadores deberán utilizar los mismos registros de obligaciones y pagos.

Se actualizarán automáticamente después de cada pago confirmado.

# 18. Consideraciones funcionales que deben definirse durante el desarrollo

1\. Generación de obligaciones: determinar si se generan al cierre, al ingresar al módulo o mediante un proceso programado.

2\. Ventas posteriores al cierre: definir cómo se procesan ventas registradas después de generar las obligaciones.

3\. Pagos parciales: definir si se aplican primero a la deuda más antigua.

4\. Cambio de recaudador: conservar el historial de pagos anteriores y el recaudador que realizó cada movimiento.

5\. Cambio de grupo: cuando un vendedor cambie de grupo, el porcentaje aplicable a sus nuevas obligaciones será el configurado para el nuevo grupo. Las obligaciones históricas conservarán el porcentaje original.

6\. Cambio de porcentaje: establecer desde qué fecha entra en vigencia.

7\. Recaudo exclusivo: un vendedor únicamente podrá estar asignado a un recaudador a la vez.

8\. Asignación exclusiva de grupos: un grupo únicamente podrá estar asignado a un recaudador.

9\. Actualización de indicadores: total por recaudar, total recaudado y saldo pendiente se actualizarán después de cada pago confirmado.

10\. Estado de las listas: después de confirmar un pago, el usuario desaparecerá de pendientes y aparecerá en cobrados.

11\. Pagos inalterables: los pagos no podrán eliminarse ni corregirse por el momento.

# 19. Criterios de aceptación

- Se puede crear un recaudador sin asignarlo obligatoriamente a un grupo.
- El administrador puede asignar grupos y vendedores individualmente.
- Un grupo no puede estar asignado a varios recaudadores.
- Un vendedor no puede estar asignado a varios recaudadores.
- Se validan las asignaciones directas y las heredadas por grupo.
- El administrador puede configurar porcentajes independientes.
- La configuración muestra porcentaje, total por recaudar y total recaudado en una misma fila.
- Los tres indicadores utilizan el mismo tamaño de letra destacado.
- Los indicadores se actualizan automáticamente.
- El sistema obtiene correctamente las ventas existentes.
- Las obligaciones se calculan individualmente.
- El recaudador opera desde una APK.
- La APK muestra grupos y personas asignadas.
- Existen listas de pendientes, cobrados y todos.
- Los usuarios pendientes se muestran en verde.
- Después de confirmar un pago, el usuario pasa a cobrados.
- El pago requiere confirmación mediante modal.
- Se permiten pagos parciales.
- Todos los valores monetarios utilizan separadores de miles.
- Todas las operaciones muestran indicadores de carga.
- Las deudas se acumulan correctamente.
- Los vendedores al día aparecen en verde como Cancelado.
- Los vendedores con deuda aparecen en rojo.
- El administrador puede consultar todos los recaudadores.
- Existen tablas separadas para usuarios y grupos cobrados y pendientes.
- El administrador puede consultar históricos y responsables de cada pago.
- Se visualizan gráficos históricos.
- Las métricas reflejan correctamente los movimientos.
- Las nuevas tablas están bajo el esquema `recaudo`.
- Los procedimientos utilizan el prefijo `sp_recaudo_`.
- Los nuevos controladores incluyen `Recaudo`.
- Las API utilizan `/api/Recaudo/`.
- Las nuevas vistas y elementos de menú siguen la convención Recaudo.
- Los pagos confirmados no pueden eliminarse ni modificarse.
- No se modificaron tablas, procedimientos, controladores ni vistas existentes.
- Se realizaron pruebas de integridad, concurrencia y consistencia de saldos.
- Se verificó que los cambios no afecten el sistema en producción.

# 20. Principio fundamental de implementación

El módulo Recaudo debe construirse como una extensión independiente del sistema actual, reutilizando únicamente los mecanismos existentes necesarios para autenticar usuarios, identificar vendedores, consultar grupos y obtener las ventas.

La operación del recaudador se realizará exclusivamente desde una nueva APK Android, mientras que la administración y configuración se realizarán mediante nuevas vistas del sitio web administrativo.

Toda nueva funcionalidad deberá desarrollarse con estructuras, procedimientos, API, vistas, permisos y configuraciones propias, preservando íntegramente la operación que actualmente se encuentra en producción.