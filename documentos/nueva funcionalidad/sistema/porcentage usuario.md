# Requerimiento: Comisión individual por usuario y por día de la semana

## 1. Objetivo del cambio

Se realizará un cambio en el funcionamiento actual de las comisiones del sistema, específicamente en el módulo Cubierto.

Actualmente, el sistema utiliza dos porcentajes generales parametrizados en la parte superior de la vista Cubierto (comisión y descuento), los cuales se aplican a todos por igual.

Esos dos campos dejarán de utilizarse y se eliminarán de la vista. A partir de este cambio, lo que el vendedor y el recaudador se ganan se configura de manera individual para cada usuario y de acuerdo con el día de la semana en que se realice la venta. No queda un descuento general aparte: el descuento es el porcentaje del vendedor y, después, el porcentaje del recaudador.

Esta nueva funcionalidad aplicará únicamente para los usuarios que tengan los siguientes roles:

- Vendedor de PDA.
- Recaudador.

## 2. Configuración de porcentajes al crear un usuario

Al momento de crear un usuario con el rol de Vendedor de PDA o Recaudador, el sistema deberá mostrar una sección para configurar sus porcentajes de comisión.

Esta configuración deberá presentar los siete días de la semana:

- Lunes.
- Martes.
- Miércoles.
- Jueves.
- Viernes.
- Sábado.
- Domingo.

Cada día deberá tener un campo obligatorio para ingresar su respectivo porcentaje de comisión.

Validaciones de los campos:

- Todos los días deben tener un porcentaje asignado obligatoriamente.
- El campo solamente permitirá números enteros entre 0 % y 100 %. No admite decimales.
- No se permitirá dejar ningún día sin configurar.
- El valor ingresado deberá almacenarse individualmente para cada usuario.
- Los porcentajes de cada usuario serán independientes de los demás usuarios.

Esta configuración deberá estar disponible tanto al crear como al editar un usuario.

## 3. Nuevo funcionamiento del módulo Cubierto

Se deberá modificar el cálculo actual del módulo Cubierto.

El sistema ya no deberá tomar la comisión general ni el descuento general que actualmente se encuentran parametrizados en la parte superior de la vista.

En su lugar, deberá consultar los porcentajes configurados individualmente para cada usuario, teniendo en cuenta el día de la semana correspondiente a la fecha de la venta.

Funcionamiento:

1. El sistema identifica la fecha de la venta.
2. A partir de esa fecha, determina qué día de la semana corresponde.
3. Consulta el porcentaje entero configurado para ese día en el vendedor de la venta.
4. Identifica al recaudador asignado a ese vendedor. Ese recaudador es el mismo usuario que tiene grupos asignados para recaudar.
5. Aplica el porcentaje del vendedor sobre el valor total de la venta. Ese resultado es lo que el vendedor debe entregar al recaudador.
6. Sobre ese valor, aplica el porcentaje del recaudador para ese mismo día.
7. El resultado final es lo que el recaudador debe entregar a la administración.

La base del cálculo es el valor de la venta. El mismo cálculo se usa en el administrador y en las aplicaciones del vendedor y del recaudador.

## 4. Ejemplo práctico del cálculo de comisiones

Para entender el nuevo funcionamiento, se plantea el siguiente escenario:

Venta realizada por el vendedor:

- Fecha: lunes 2 de noviembre de 2026.
- Valor total vendido: $100.000.

Porcentajes configurados para el vendedor:

| Día       | Comisión |
| --------- | -------- |
| Lunes     | 10 %     |
| Martes    | 11 %     |
| Miércoles | 12 %     |
| Jueves    | 13 %     |
| Viernes   | 14 %     |
| Sábado    | 15 %     |
| Domingo   | 16 %     |

Como la venta se realizó el lunes 2 de noviembre de 2026, el sistema deberá identificar automáticamente que corresponde al lunes y aplicar el 10 % de comisión del vendedor.

Primer cálculo: descuento del vendedor

| Concepto                     | Valor    |
| ---------------------------- | -------- |
| Total vendido                | $100.000 |
| Comisión del vendedor (10 %) | $10.000  |
| Subtotal después de comisión | $90.000  |

Porcentajes configurados para el recaudador:

| Día       | Comisión |
| --------- | -------- |
| Lunes     | 4 %      |
| Martes    | 5 %      |
| Miércoles | 6 %      |
| Jueves    | 7 %      |
| Viernes   | 8 %      |
| Sábado    | 9 %      |
| Domingo   | 3 %      |

Como la venta corresponde al lunes, el sistema deberá consultar el porcentaje del lunes configurado para el recaudador, que en este ejemplo es del 4 %.

Segundo cálculo: descuento del recaudador

| Concepto                                  | Valor   |
| ----------------------------------------- | ------- |
| Subtotal después de comisión del vendedor | $90.000 |
| Comisión del recaudador (4 %)             | $3.600  |
| Total por recaudar para la administración | $86.400 |

VALOR FINAL POR RECAUDAR

# $86.400

Resultado después de aplicar consecutivamente la comisión del vendedor y la del recaudador.

Importante: El porcentaje del recaudador se calcula sobre el subtotal que queda después de descontar la comisión del vendedor, no sobre el valor original de la venta.

## 4.1. Qué ve cada rol

El mismo movimiento se muestra distinto según quién consulta.

Ejemplo de visualización, con venta de $100.000, vendedor al 10 % y recaudador al 5 % ese día:

| Quién consulta | Qué se muestra |
| -------------- | -------------- |
| Vendedor (APK) | La venta de $100.000 y lo que debe entregar al recaudador: $90.000. |
| Recaudador (APK) | Que el vendedor debe entregarle $90.000. También lo que él debe entregar a la administración: $90.000 menos su 5 %, es decir $85.500. |
| Administrador | El movimiento completo: venta total, porcentajes, valores descontados y lo que se debe entregar. |

Reglas de las aplicaciones:

- El APK del vendedor de PDA muestra cuánto debe pagar (entregar) al recaudador, ya descontado su porcentaje del día de la venta.
- El APK del recaudador muestra cuánto debe recaudar de ese vendedor y, aparte, cuánto debe entregar a la administración después de su propio porcentaje del día.
- El administrador ve estos movimientos con la venta total, los porcentajes, los descuentos y el valor a entregar.

## 5. Actualización del histórico de Cubierto

Se deberá actualizar la tabla de histórico de Cubierto para que permita visualizar detalladamente todos los movimientos realizados y los cálculos de las comisiones.

El histórico deberá permitir identificar como mínimo:

- Fecha de la venta.
- Día de la semana correspondiente.
- Nombre del vendedor.
- Nombre del recaudador.
- Total vendido.
- Porcentaje de comisión aplicado al vendedor.
- Valor descontado por comisión del vendedor.
- Subtotal después de aplicar la comisión del vendedor.
- Porcentaje de comisión aplicado al recaudador.
- Valor descontado por comisión del recaudador.
- Total final por recaudar a la administración.

Cada movimiento guarda el porcentaje que el usuario tenía configurado en el momento en que se creó. Si después se cambia el porcentaje de un día, los movimientos ya creados conservan el porcentaje y los valores con los que se calcularon. No se actualizan.

## 6. Actualización de informes y reportes del sistema

Debido a que cambia la lógica de cálculo de las comisiones, se deberán actualizar los módulos de informes y reportes que utilizan estos valores.

Se deberán revisar y ajustar, como mínimo, los siguientes:

- Informes de ventas.
- Informes de vendedores.
- Total de ingresos.
- Total de ganancias.
- Informes de recaudo.
- Informes de comisiones.
- Cualquier otro informe o indicador que dependa de los porcentajes de comisión o de los valores calculados en Cubierto.

Todos los informes que usen estos porcentajes o los valores calculados en Cubierto deberán utilizar la nueva lógica de comisiones individuales por usuario y por día de la semana. El ajuste cubre cada informe donde el cambio sea necesario.

Se deberá garantizar que los totales, subtotales, descuentos y ganancias presentados en los informes coincidan con los valores calculados en el módulo Cubierto.

## 7. Eliminación de la comisión y el descuento generales en Cubierto

Se deberán eliminar por completo los campos de comisión general y de descuento general que actualmente aparecen en la parte superior de la vista Cubierto.

El descuento no sigue existiendo como un tercer porcentaje. El descuento queda representado por:

1. El porcentaje que se gana el vendedor, aplicado sobre el valor de la venta.
2. El porcentaje que se gana el recaudador, aplicado sobre el subtotal que queda después del vendedor.

Adicionalmente:

- Eliminar ambos campos de la interfaz de Cubierto.
- Dejar de usarlos en los cálculos y procesos nuevos.
- Ajustar los procedimientos, consultas y demás componentes que actualmente dependan de esos dos porcentajes generales.
- Los movimientos ya guardados en el histórico conservan la comisión y el descuento con los que se calcularon. No se borran ni se recalculan.

El sistema deberá trabajar exclusivamente con los porcentajes individuales configurados para cada usuario y según el día de la semana.

## 8. Usuarios que ya existen

Los vendedores de PDA y los recaudadores que ya existen al momento del cambio reciben, por defecto, 1 % en cada uno de los siete días. Ese valor queda guardado como su configuración y se puede editar después.

## 9. Consideraciones importantes de implementación

1. Los porcentajes de comisión deben ser independientes para cada usuario.
2. La configuración debe aplicar exclusivamente a los roles Vendedor de PDA y Recaudador.
3. Los siete días de la semana deben tener un porcentaje entero obligatorio, incluso cuando sea 0 %. No se admiten decimales.
4. El día de la semana debe determinarse utilizando la fecha real de la venta, no la fecha en que se realiza el recaudo.
5. Cuando existan ventas de diferentes fechas, el sistema deberá aplicar el porcentaje correspondiente a cada fecha y día.
6. El recaudador del cálculo es el asignado al vendedor. Es el mismo usuario que tiene grupos asignados para recaudar.
7. El cálculo del porcentaje del recaudador siempre deberá realizarse sobre el subtotal resultante después de descontar la comisión del vendedor.
8. Cada movimiento conserva el porcentaje que estaba configurado cuando se creó. Un cambio posterior de la configuración no actualiza movimientos ya creados.
9. Los informes y reportes que dependan de estos valores deben reflejar la nueva lógica.
10. Los registros históricos anteriores al cambio deberán conservar su información y no deberán modificarse retroactivamente.
11. Los usuarios vendedor de PDA y recaudador que ya existen inician con 1 % en los siete días.
12. El APK del vendedor muestra lo que debe entregar al recaudador. El APK del recaudador muestra lo que debe recaudar y lo que debe entregar a la administración. El administrador ve el movimiento completo.

Resultado esperado: Cada vendedor de PDA y cada recaudador tendrá sus propios porcentajes de comisión para los siete días de la semana. El sistema identificará automáticamente el día correspondiente a cada venta y calculará los valores que se deben descontar, garantizando que Cubierto, sus históricos y todos los informes trabajen con la misma lógica de cálculo.