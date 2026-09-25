

# Requerimiento: Validación de cantidad de dígitos según valor apostado

## 1. Objetivo

El sistema debe validar la cantidad máxima de dígitos que el vendedor puede ingresar en el campo **Valor Apostado**, tomando como referencia el primer dígito ingresado.

Esta validación aplica **únicamente para el perfil Vendedor**.

## 2. Validación del primer dígito

El campo **Valor Apostado no debe permitir iniciar el valor con el dígito `0`**.

Si el vendedor intenta ingresar `0` como primer dígito, el sistema debe impedir el ingreso.

## 3. Cantidad máxima de dígitos

Una vez ingresado el primer dígito, el sistema debe aplicar las siguientes reglas:

### Valores que comienzan entre 1 y 5

Si el valor comienza con:

* `1`

* `2`

* `3`

* `4`

* `5`

El sistema debe permitir ingresar **máximo 5 dígitos en total**.

Ejemplos:

* `1` + 4 dígitos → `12345`

* `2` + 4 dígitos → `23456`

* `3` + 4 dígitos → `34567`

* `4` + 4 dígitos → `45678`

* `5` + 4 dígitos → `56789`

### Valores que comienzan entre 6 y 9

Si el valor comienza con:

* `6`

* `7`

* `8`

* `9`

El sistema debe permitir ingresar **máximo 4 dígitos en total**.

Ejemplos:

* `6` + 3 dígitos → `6789`

* `7` + 3 dígitos → `7890`

* `8` + 3 dígitos → `8901`

* `9` + 3 dígitos → `9012`

## 4. Tabla de validación

| Primer dígito | Máximo de dígitos permitidos |

| ------------- | ---------------------------: |

| `0`           |             **No permitido** |

| `1`           |                            5 |

| `2`           |                            5 |

| `3`           |                            5 |

| `4`           |                            5 |

| `5`           |                            5 |

| `6`           |                            4 |

| `7`           |                            4 |

| `8`           |                            4 |

| `9`           |                            4 |

## 5. Comportamiento esperado

* El sistema debe impedir que el vendedor ingrese `0` como primer carácter.

 *Si el primer dígito es del* `1` *al* `5`*, el campo permitirá máximo* *5 dígitos**.

 *Si el primer dígito es del* `6` *al* `9`*, el campo permitirá máximo* *4 dígitos**.

* Una vez alcanzado el máximo permitido, el sistema debe impedir el ingreso de caracteres adicionales.

 *Esta validación aplica únicamente al* *perfil Vendedor**.