# Requerimiento: Nueva opción de juego «Juego números» en el PDA

## 1. Objetivo

Agregar una tercera opción de juego en el PDA para que el vendedor pueda registrar apuestas ingresando varios números y seleccionando una o varias loterías disponibles del día.

La funcionalidad debe permitir ingresar el valor total que el cliente desea apostar, agregar o eliminar líneas de números y calcular automáticamente el valor correspondiente a cada número en cada lotería seleccionada.

Todas las fechas y horas de este juego se registran y se manipulan en hora de Colombia, según la regla del apartado 8.

La jugada se registra como una venta más, con el QR actual, para que se pueda validar como ganadora igual que los demás boletos. Lo nuevo (la marca del juego en el esquema `manual` y el endpoint de registro) no modifica nada de lo existente, según el apartado 9.

## 2. Nueva opción de juego

En el menú de juegos del PDA se debe agregar una tercera opción llamada **«Juego números»**, sin modificar ni afectar el funcionamiento de las dos opciones de juego existentes.

Al ingresar a esta opción, el sistema mostrará un formulario para registrar la jugada.

## 3. Selección de loterías

En la parte superior de la pantalla se deben mostrar las loterías disponibles para el día actual en Colombia.

- El vendedor podrá seleccionar una o varias loterías mediante casillas de selección múltiple.
- Solo se deben mostrar las loterías que estén disponibles para jugar en ese momento, de acuerdo con su configuración y horario. El día actual y la hora de cierre se evalúan con la fecha y hora de Colombia.
- El sistema debe permitir seleccionar y deseleccionar loterías antes de registrar la jugada.
- Para continuar, debe existir al menos una lotería seleccionada.

**Ejemplo:**

- [x] Chontico
- [ ] Valle
- [ ] Bogotá
- [x] Medellín

En este ejemplo, el cliente jugará todos los números ingresados en las loterías Chontico y Medellín.

## 4. Campo para ingresar el valor total apostado

Debajo de la selección de loterías se debe mostrar un campo llamado **«Valor jugado»**.

Este campo permitirá ingresar el valor total que el cliente desea apostar por toda la jugada.

Requisitos:

- El campo debe mostrar automáticamente los separadores de miles mientras el vendedor escribe.
- El valor debe visualizarse en formato monetario para facilitar su lectura.
- El sistema debe validar que el valor ingresado sea válido y mayor que cero.
- El valor ingresado corresponde al total de la jugada, no al valor individual de cada número ni al valor individual por lotería.

**Ejemplo:** si el vendedor ingresa $10.000 y selecciona dos loterías, los $10.000 corresponden al valor total que se distribuirá entre todos los números jugados en todas las loterías seleccionadas.

## 5. Ingreso, agregado y eliminación de números

Debajo del campo «Valor jugado» se debe mostrar una sección con el texto **«Número a jugar»**.

Cada línea permitirá ingresar un número y tendrá controles para agregar o eliminar líneas, con las siguientes reglas:

- El vendedor ingresará un número en el campo correspondiente.
- Cada línea debe mostrar un botón con el símbolo **«−»** para eliminar esa línea.
- El botón con el símbolo **«+»** solo debe mostrarse en la última línea de números.
- Al presionar «+», el sistema debe agregar una nueva línea vacía al final. El botón «+» debe quedar únicamente en esa nueva última línea.
- Las líneas anteriores deben conservar el botón «−» y no deben mostrar el botón «+».
- Al eliminar una línea mediante «−», el sistema debe retirar esa línea y reorganizar los controles para que el botón «+» permanezca únicamente en la última línea.
- El vendedor podrá agregar tantas líneas como permita la configuración y las reglas existentes del sistema, si aplica.
- Cada línea debe permitir ingresar un número de acuerdo con las reglas de longitud y validación que correspondan a este tipo de juego.
- El sistema debe permitir visualizar todos los números ingresados antes de registrar la apuesta.
- Agregar una línea en la interfaz no debe generar por sí solo un registro de apuesta.
- Al agregar o eliminar una línea, el sistema debe actualizar automáticamente la cantidad de números, la cantidad de apuestas individuales y el valor correspondiente a cada apuesta, de acuerdo con las loterías seleccionadas y el valor total ingresado.

**Ejemplo de números ingresados:**

1. 1234
2. 4323
3. 5676
4. 2398
5. 9878

## 6. Cálculo del total apostado y valor por apuesta individual

El sistema debe calcular automáticamente el total de apuestas individuales generadas y el valor que corresponde a cada una.

Se deben tener en cuenta los siguientes valores:

- **Valor total jugado:** valor ingresado por el vendedor.
- **Cantidad de números ingresados:** cantidad de números válidos registrados en el formulario.
- **Cantidad de loterías seleccionadas:** cantidad de loterías elegidas por el vendedor.

Cada número ingresado se jugará en cada una de las loterías seleccionadas. Por lo tanto, la cantidad total de apuestas individuales será el resultado de multiplicar la cantidad de números por la cantidad de loterías seleccionadas.

**Fórmulas:**

Total de apuestas individuales = cantidad de números × cantidad de loterías seleccionadas

Valor por apuesta individual = valor total jugado ÷ total de apuestas individuales

### Ejemplo de cálculo

El cliente desea apostar $10.000, selecciona dos loterías e ingresa cinco números.

| Concepto | Cantidad o valor |
|---|---:|
| Valor total jugado | $10.000 |
| Loterías seleccionadas | 2 |
| Números ingresados | 5 |
| Total de apuestas individuales | 10 |
| Valor por apuesta individual | $1.000 |

En este escenario, cada número se jugará tanto en Chontico como en Medellín.

Por ejemplo, el número 1234 generará dos apuestas individuales:

- 1234 en Chontico: $1.000.
- 1234 en Medellín: $1.000.

El mismo procedimiento se aplicará a los otros cuatro números. De esta manera, las diez apuestas individuales sumarán los $10.000 ingresados inicialmente.

**Regla para divisiones no exactas:** el sistema debe evitar errores de redondeo. Si el valor total no puede dividirse exactamente entre la cantidad de apuestas individuales, deberá distribuir el sobrante entre algunas apuestas individuales para que la suma final coincida exactamente con el valor total ingresado, sin perder ni agregar dinero. La distribución debe ser consistente y verificable.

## 7. Resumen de la jugada

Antes de registrar la apuesta, el sistema debe mostrar un resumen con la información ingresada y los cálculos realizados.

El resumen debe incluir:

- Loterías seleccionadas.
- Valor total jugado.
- Lista completa de números ingresados.
- Cantidad de números ingresados.
- Cantidad de loterías seleccionadas.
- Cantidad total de apuestas individuales generadas.
- Valor por apuesta individual.
- Fecha y hora del juego, en hora de Colombia.
- Mensaje de despedida: **«¡Mucha suerte!»**

### Ejemplo de visualización

**Juego números**

**Loterías**

- [x] Chontico
- [ ] Valle
- [ ] Bogotá
- [x] Medellín

**Valor jugado:** $10.000

**Números a jugar**

| Número |
|---:|
| 1234 |
| 4323 |
| 5676 |
| 2398 |
| 9878 |

**Total apostado:** $10.000  
**Cantidad de números ingresados:** 5  
**Cantidad de loterías seleccionadas:** 2  
**Cantidad de apuestas individuales:** 10  
**Valor por apuesta individual:** $1.000  
**Fecha y hora del juego:** fecha y hora del registro, en hora de Colombia

**¡Mucha suerte!**

La distribución visual de los números podrá adaptarse al ancho de la pantalla del PDA, organizándolos en una o varias columnas para facilitar su lectura. El resumen debe ser legible y permitir verificar los datos antes de confirmar la jugada.

## 8. Fecha y hora de Colombia

Regla obligatoria: toda fecha u hora que se cree, registre, guarde, calcule, compare o muestre en «Juego números» debe estar en la hora oficial de Colombia (UTC−5, sin horario de verano). No se usa UTC ni se hacen conversiones de zona horaria en ningún punto del flujo.

Requisitos:

- La fecha y hora del juego se crea directamente en hora de Colombia en el momento del registro.
- En la base de datos las fechas se crean y se guardan en hora de Colombia. No se guardan en UTC ni con desplazamiento de zona horaria.
- Los valores por defecto de las columnas de fecha en SQL Server, si se usan, deben producir hora de Colombia. No se usa `GETUTCDATE()` ni `SYSUTCDATETIME()`.
- En el API no se usa `DateTime.UtcNow` para las fechas de este juego.
- Las fechas guardadas se leen, comparan, reportan e imprimen tal como están, sin convertirlas.
- El servidor es la fuente de la fecha y hora del registro. La hora del PDA no debe usarse para decidir la fecha del juego ni para validar cierres.
- El API toma la fecha y hora directamente de SQL Server en hora de Colombia (`SYSDATETIMEOFFSET() AT TIME ZONE 'SA Pacific Standard Time'`) y la guarda tal cual.
- El día actual, la disponibilidad de las loterías y la hora de cierre de cada sorteo se evalúan con la fecha y hora de Colombia.
- La fecha del sorteo asociada a cada apuesta individual corresponde al día calendario de Colombia.
- El resumen, el tiquete y cualquier consulta posterior de la jugada muestran la fecha y hora en hora de Colombia.
- Los cortes por día (inicio y fin del día) se calculan con la medianoche de Colombia.

## 9. Registro de la venta, QR y validación del ganador

Regla obligatoria: la jugada de «Juego números» se registra donde hoy se crean las ventas y los juegos, para que su QR se cree igual que el de las demás ventas y el tiquete se pueda validar como ganador igual que cualquier otro boleto.

Lo que ya existe (estructura de tablas, procedimientos, vistas, funciones, controladores, endpoints y clases) funciona correctamente y no se modifica, para no causar daños en el sistema. Lo nuevo va en el esquema `manual` o en clases y endpoints con la palabra «Manual».

### Base de datos

- La jugada se guarda en las tablas actuales de ventas: una venta, un boleto y un juego por cada combinación de número y lotería, con el valor que le corresponde según el reparto del apartado 6. Cada juego queda asociado a una sola lotería.
- La venta y sus juegos quedan con el tipo de apuesta `INDIVIDUAL`, que es lo que representan: cada número es una apuesta individual en cada lotería.
- El boleto lleva el mismo código público de 7 dígitos, la misma clave de validación y el mismo QR cifrado que las demás ventas.
- No se cambia la estructura de las tablas existentes: no se agregan ni cambian columnas, índices, restricciones ni disparadores. No se modifican los procedimientos, vistas ni funciones existentes.
- Para saber qué ventas son «Juego números», se crea en el esquema `manual` la tabla `manual.VentasJuegoNumeros` (venta, valor jugado, cantidad de números, de loterías y de apuestas, y fecha de registro en hora de Colombia) y el procedimiento `manual.JuegoNumerosMarcar`, que la llena dentro de la misma transacción de la venta.
- No se validan topes en este juego, pero sus apuestas suman a los acumulados de las loterías como cualquier otra venta.

### API

- El registro se hace en un endpoint nuevo: `POST api/JuegoNumerosManual/Registrar`, en `JuegoNumerosManualController`. No se modifica ningún endpoint existente ni el servicio actual de ventas.
- Las demás clases nuevas del API para este juego (servicio, contratos, repositorio) llevan la palabra «Manual» en el nombre.
- El endpoint nuevo repite la lógica actual de creación de la venta: código público, clave de validación, QR cifrado, alertas al administrador por número repetido y por valor alto, y generación del recaudo.
- La consulta y la validación del tiquete usan los endpoints que ya existen, sin cambios.
- Cada jugada viaja con un identificador propio. Si el PDA reintenta el registro con el mismo identificador, el API devuelve la jugada ya registrada y no la duplica.

### Solo con conexión

- «Juego números» solo se puede jugar con conexión al servidor. No hay registro sin conexión ni sincronización posterior.
- El formulario lo indica con un aviso. Si al registrar no hay conexión, el PDA lo informa, la jugada sigue en pantalla y el vendedor puede intentar de nuevo cuando vuelva la conexión.
- Si el horario del PDA está cerrado, no se puede iniciar ni registrar la jugada.

### Tiquete

- El tiquete usa la plantilla actual del recibo de venta, con el código de siempre y el QR actual. En el cuerpo lleva las loterías, los números en columnas, las cantidades, el valor por apuesta, el total apostado, la leyenda y «¡Mucha suerte!».
- Se imprime automáticamente al registrar y se puede imprimir de nuevo.

## 10. Validaciones y comportamiento del sistema

- No permitir registrar una jugada sin seleccionar al menos una lotería.
- No permitir registrar una jugada sin ingresar el valor total apostado.
- No permitir registrar una jugada sin ingresar al menos un número válido.
- Validar que todos los números ingresados cumplan las reglas del juego.
- Actualizar automáticamente los cálculos cuando cambie el valor apostado, la cantidad de números o las loterías seleccionadas.
- Si el vendedor agrega o elimina un número, recalcular la cantidad de apuestas individuales y su valor correspondiente.
- Si el vendedor selecciona o deselecciona una lotería, recalcular la distribución del valor total.
- La suma de todas las apuestas individuales debe coincidir exactamente con el valor total ingresado.
- No permitir valores inválidos o iguales a cero en el campo «Valor jugado».
- Antes de confirmar la jugada, el vendedor debe poder revisar el resumen completo.
- El registro final debe conservar la relación entre cada número jugado y cada lotería seleccionada, con sus respectivos valores.
- Los controles «+» y «−» deben comportarse según las reglas del apartado 5: «+» únicamente en la última línea y «−» en cada línea para eliminarla.
- Al eliminar una línea, no deben quedar líneas duplicadas ni controles «+» en posiciones incorrectas.
- Los cálculos deben considerar únicamente los números válidos ingresados y las loterías actualmente seleccionadas.
- No permitir registrar apuestas en loterías cuyo sorteo ya cerró según la hora de Colombia.
- No hay límite de líneas de números. Un número repetido en la misma jugada no se permite.
- El valor jugado se reparte en pesos enteros. Si la división no es exacta, el sobrante suma $1 a las primeras apuestas, en el orden de los números y luego de las loterías.
- Las jugadas de este juego quedan registradas como ventas `INDIVIDUAL` y se identifican con la tabla `manual.VentasJuegoNumeros`.

## 11. Criterios de aceptación

1. Se muestra la nueva opción «Juego números» en el PDA, sin alterar las opciones existentes.
2. El vendedor puede seleccionar múltiples loterías disponibles.
3. El campo «Valor jugado» muestra separadores de miles mientras se escribe.
4. El vendedor puede agregar líneas mediante el botón «+», visible únicamente en la última línea.
5. Cada línea muestra un botón «−» para eliminarla.
6. Al agregar una línea, el botón «+» aparece solo en la nueva última línea.
7. Al eliminar una línea, el sistema reorganiza los controles y mantiene el botón «+» únicamente en la última línea.
8. Cada número se considera una apuesta individual en cada lotería seleccionada.
9. El sistema calcula correctamente la cantidad de apuestas individuales.
10. El sistema distribuye el valor total entre las apuestas individuales y mantiene la suma exacta del valor ingresado, incluso cuando la división no sea exacta.
11. El resumen muestra las loterías, los números, el valor total, la cantidad de números, la cantidad de apuestas, el valor por apuesta y la fecha y hora del juego en hora de Colombia.
12. Los cálculos se actualizan al modificar cualquiera de los datos de la jugada.
13. El sistema impide registrar jugadas incompletas o con datos inválidos.
14. El registro final conserva la relación de cada número con cada lotería seleccionada y el valor correspondiente a cada apuesta individual.
15. Todas las fechas y horas del juego se crean, se guardan en la base de datos y se manipulan en hora de Colombia, sin UTC y sin conversiones de zona horaria.
16. La jugada queda registrada en las tablas actuales de ventas, con un juego por número en cada lotería, y marcada en `manual.VentasJuegoNumeros`. La estructura de las tablas, procedimientos, vistas y funciones existentes queda sin cambios.
17. El tiquete lleva el código y el QR actuales, y se consulta y valida como ganador con los mismos endpoints que los demás boletos.
18. El API expone el endpoint nuevo `api/JuegoNumerosManual/Registrar`. Ningún endpoint existente fue modificado.
19. Sin conexión no se puede registrar la jugada; el PDA lo informa y permite reintentar sin duplicarla.
