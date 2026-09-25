# Checklist de Validación del Sistema

## 1. Notificaciones y detalle de ventas

- [ ] **1. Detalle de venta en notificaciones**
  - En la vista **Notificaciones**, al presionar el botón **Ver**, se debe mostrar un detalle completo de la venta.
  - Debe incluir:
    - Valor de la venta.
    - Número jugado.
    - Lotería.

- [ ] **2. Histórico del número jugado**
  - En la notificación se debe mostrar el histórico del número jugado.
  - Debe incluir:
    - Número jugado.
    - Fecha.
    - Hora.
    - Valor.
    - Lotería.
    - Total.

---

## 2. Impresión y reporte de tickets

- [ ] **3. Bloquear reimpresión de tirilla**
  - No permitir reimprimir la tirilla una vez realizado un juego.

- [ ] **4. Reportar ticket defectuoso**
  - Crear un botón para **Reportar ticket** cuando la tirilla se imprima de forma defectuosa.
  - El sistema debe solicitar obligatoriamente al usuario:
    - Texto explicando por qué desea reportar el ticket.
  - Al realizar el reporte:
    - Enviar una copia del ticket al chat de soporte del administrador.
    - Enviar junto con la copia el texto ingresado por el usuario.

---

## 3. Chats

- [ ] **5. Chat de soporte**
  - Crear un chat destinado exclusivamente a:
    - Reportar tickets defectuosos.
    - Enviar al administrador una copia del ticket reportado.
    - Incluir la explicación proporcionada por el usuario.

- [ ] **6. Chat de atención al cliente**
  - Crear un chat independiente destinado a la interacción general con los usuarios.

---

## 4. KPI

- [ ] **7. Modificar KPI de juego**
  - Cambiar el indicador de **Ticket Promedio**.
  - Mostrar:
    - Número más jugado.
    - Valor más jugado.

- [ ] **8. Filtros en tabla "Ingreso por persona"**
  - En la tabla de **Ingreso por persona**, agregar filtros/ordenamiento en cada campo.
  - Verificar que se pueda organizar la información por cada columna.

- [ ] **9. Descarga de PDF de KPI**
  - Revisar la descarga del PDF generado desde los KPI.
  - Verificar:
    - Generación correcta del PDF.
    - Información completa.
    - Formato correcto.
    - Valores correctos.
    - Sin errores de visualización.

---

## 5. Códigos offline

- [ ] **10. Regeneración automática de códigos offline**
  - Los códigos offline deben regenerarse automáticamente.
  - Cuando el dispositivo encuentre conexión con el servidor, debe enviar:
    - Cantidad de códigos offline gastados.
    - Registro de la cantidad máxima de códigos configurada para el usuario/dispositivo.
  - El servidor debe validar la información recibida.
  - El servidor debe calcular automáticamente:
    - Cantidad de códigos utilizados.
    - Cantidad de códigos que deben generarse nuevamente.
    - Cantidad máxima configurada.
  - El sistema debe generar automáticamente los códigos offline que correspondan.
  - La regeneración automática debe ejecutarse **máximo una vez por día**.

---

## 6. Ganadores y juegos pagados

- [ ] **11. Resaltar ganadores**
  - Resaltar toda la fila del ganador en **verde**.

- [ ] **12. Resaltar juegos pagados**
  - Resaltar toda la fila de los juegos pagados en **azul**.

- [ ] **13. Registrar ganador**
  - En la vista **Registrar ganador**, en la tabla:
    - Mostrar la cantidad de ganadores.
    - Agregar botón **Ver**.

- [ ] **14. Detalle de ganadores**
  - El botón **Ver** debe cargar una vista con:
    - Toda la información de la boleta.
    - Información de los ganadores.
    - Botón **Ver boleta**.

- [ ] **15. Perfil observador**
  - En el perfil **Observador**, permitir visualizar el ganador con su detalle completo.

---

## 7. Vendedor

- [ ] **16. Total de ventas**
  - En el perfil/vista del vendedor, dentro del apartado **Histórico**, mostrar:
    - Total de ventas.

- [ ] **17. Formato de valores entregados**
  - Aplicar separador de miles a los valores ingresados para el valor entregado por parte del vendedor.
  - Ejemplo:
    - `1000` → `1.000`
    - `250000` → `250.000`

---

## 8. Administración y seguridad

- [ ] **18. Eliminación de PDA**
  - Un PDA solamente puede ser eliminado si cumple:
    - 30 días de inactividad.

- [ ] **19. Confirmación de operaciones administrativas**
  - Para todas las acciones realizadas desde el administrador que impliquen:
    - Crear datos.
    - Actualizar datos.
    - Eliminar datos.
  - El sistema debe solicitar una **contraseña de confirmación** antes de ejecutar la operación.

- [ ] **20. Llave de acceso al sistema administrador**
  - Crear una **llave de acceso** para ingresar al sistema administrador.

- [ ] **21. Usuario superusuario**
  - Crear un usuario **Super Usuario**.
  - El Super Usuario:
    - No requiere llave de acceso.
    - Tiene todos los derechos/permisos de administrador.

---

## 9. Números exentos de juego

- [ ] **22. Configuración de números exentos**
  - Desde el administrador se debe poder configurar una lista de números que estarán exentos/restringidos para el juego.

- [ ] **23. Validación durante el juego**
  - Cuando un usuario intente jugar un número configurado como exento:
    - El sistema debe impedir la operación.
    - No debe registrar el juego.
    - No debe generar la tirilla.
    - Debe mostrar un mensaje indicando claramente por qué no se permite utilizar ese número.

---

# Validación final

- [ ] **Todos los puntos anteriores fueron implementados.**
- [ ] **Todos los puntos fueron probados manualmente.**
- [ ] **No se encontraron errores durante las pruebas.**
- [ ] **Los cambios fueron validados en ambiente de pruebas.**
- [ ] **Los cambios fueron validados en el dispositivo/PDA.**
- [ ] **Los cambios fueron validados en el sistema administrador.**
- [ ] **Los cambios fueron validados en el perfil vendedor.**
- [ ] **Los cambios fueron validados en el perfil observador.**
- [ ] **Los cambios fueron validados en el flujo completo de juego → ticket → ganador → pago.**