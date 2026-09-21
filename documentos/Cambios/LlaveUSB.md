# Llave Externa de Autenticación para Administrador

## 1. Objetivo

Implementar un mecanismo adicional de seguridad para el acceso de los usuarios administradores mediante una **llave física externa**, almacenada en un medio físico USB.

La llave física debe ser **independiente del usuario y la contraseña**, pero debe quedar **asociada al usuario administrador** que la registre.

La llave física **no reemplaza el usuario ni la contraseña**.

Para ingresar a la aplicación web administrativa será obligatorio validar:

**Usuario + Contraseña + Llave física válida asociada al usuario**

---

# 2. Características de la llave física

La llave debe:

- Ser única.
- Ser generada por el sistema.
- Ser almacenada en una memoria USB.
- Estar protegida mediante mecanismos criptográficos.
- Estar asociada a un usuario administrador específico.
- Tener un identificador único.
- Poder ser activada, bloqueada o revocada.
- No contener la contraseña del usuario.
- No reemplazar las credenciales tradicionales.
- No permitir autenticación únicamente por la existencia de un archivo.

### Regla importante

La llave física debe demostrar criptográficamente que es una llave válida.

No se debe considerar válida simplemente porque exista un archivo determinado dentro de la USB.

---

# 3. Instalación y configuración de la llave

La configuración de una llave física de administrador debe realizarse desde el módulo **Instalaciones** de la aplicación web administrativa.

Dentro de **Instalaciones** debe existir una tarjeta denominada:

**Registrar llave de administrador**

---

## 3.1. Selección del usuario administrador

Al ingresar a **Registrar llave de administrador**, el sistema debe solicitar al administrador que seleccione el usuario al cual se asociará la nueva llave.

El listado debe mostrar **únicamente usuarios con perfil o rol de administrador**.

No debe ser posible seleccionar usuarios que no tengan permisos de administrador.

Ejemplo:

```text
Registrar llave de administrador

Usuario administrador:
[ Seleccionar usuario ▼ ]

[ Generar llave ]

```

La llave generada debe quedar asociada exclusivamente al usuario seleccionado.

---

## 3.2. Llave existente

Antes de generar una nueva llave, el sistema debe verificar si el usuario seleccionado ya tiene una llave activa.

Si el usuario ya posee una llave:

1. El sistema debe informar que existe una llave actualmente registrada.
2. Debe advertir que la generación de una nueva llave **invalidará la anterior**.
3. Debe solicitar confirmación antes de continuar.
4. Si el administrador confirma:
  - La llave anterior debe quedar invalidada o revocada.
  - La nueva llave debe generarse.
  - La nueva llave debe quedar asociada al usuario.
  - La nueva información debe registrarse en el servidor.
5. La llave anterior no debe volver a permitir autenticación.

Ejemplo:

```text
Este usuario ya tiene una llave activa.

Si continúa, la llave actual será invalidada
y deberá utilizar la nueva llave generada.

[ Cancelar ]    [ Generar nueva llave ]

```

---

# 4. Detección del medio USB

Para generar la llave física, el sistema debe detectar una memoria USB conectada al equipo desde el cual se está realizando la instalación.

Si no existe una USB compatible conectada, el sistema debe informar al administrador que debe conectar una.

Ejemplo:

```text
No se ha detectado una memoria USB.

Conecte la memoria USB que desea utilizar
para generar la llave de administrador.

[ Reintentar ]

```

---

# 5. Preparación del medio USB

Una vez detectada la USB, el sistema debe informar claramente que el medio será utilizado exclusivamente para crear la llave de autenticación.

Antes de inicializar la USB, debe mostrarse una advertencia indicando que **los datos existentes en el medio USB serán eliminados como parte del proceso de creación de la llave**.

Ejemplo:

```text
ADVERTENCIA

La memoria USB será preparada para utilizarse
como llave de autenticación.

Los datos existentes en esta memoria serán eliminados
durante el proceso de configuración.

[ Cancelar ]    [ Confirmar y continuar ]

```

La aplicación **no debe realizar ninguna operación de respaldo, recuperación, copia, análisis o administración sobre los archivos existentes en la USB**.

La aplicación únicamente debe considerar la USB como el medio físico destinado a la creación de la llave.

Una vez confirmada la operación, el sistema podrá preparar el medio para la creación de la llave.

---

# 6. Inicialización de la USB

Una vez confirmada la operación:

1. El sistema debe verificar nuevamente el medio USB.
2. Debe preparar el medio para utilizarlo como llave.
3. Debe eliminar los datos existentes como parte del proceso de inicialización.
4. Debe generar el material criptográfico correspondiente.
5. Debe almacenar en la USB únicamente la información necesaria para funcionar como llave.
6. Debe registrar en el servidor la información necesaria para validar dicha llave.
7. Debe asociar la llave al usuario administrador seleccionado.
8. Debe marcar la llave como **ACTIVA**.

**No se debe asociar la llave al computador utilizado durante la instalación.**

Un mismo computador puede ser utilizado por uno o varios administradores.

La autorización depende de la relación entre:

**Usuario + Contraseña + Llave física asociada**

y no de un equipo específico.

---

# 7. Protección criptográfica de la llave

La información almacenada en la USB debe estar protegida mediante mecanismos criptográficos.

La implementación debe utilizar un mecanismo seguro que permita que:

```text
Información almacenada en USB
+
Información almacenada en servidor
=
Llave válida

```

La información almacenada exclusivamente en la USB **no debe ser suficiente para autenticarse**.

El servidor debe conservar la información necesaria para validar la autenticidad de la llave.

### Importante

No se debe almacenar en la USB una contraseña reutilizable que permita autenticarse directamente.

La solución puede utilizar mecanismos criptográficos apropiados, por ejemplo:

- Hash + Salt, cuando corresponda.
- Criptografía de clave pública/privada.
- Firma digital.
- Otro mecanismo criptográfico seguro apropiado para el escenario.

La elección final debe realizarse de acuerdo con la arquitectura de seguridad de la aplicación.

---

# 8. Protección contra copia de la llave

La llave debe diseñarse para que una simple copia de sus archivos a otra USB **no genere una segunda llave válida**.

No debe ser posible duplicarla mediante mecanismos como:

- `Ctrl + C / Ctrl + V`.
- Copiar y pegar desde el explorador.
- Click derecho → Copiar.
- Copiar los archivos manualmente.
- Crear una copia de la estructura de archivos.
- Clonar el contenido de la USB.
- Crear una imagen o clon del dispositivo.

El sistema no debe considerar válida una USB únicamente porque contiene los mismos archivos que la USB original.

---

# 9. Detección de manipulación o duplicación

El mecanismo de seguridad debe permitir detectar, cuando técnicamente sea posible, intentos de:

- Copiar la información de la llave.
- Duplicar la llave.
- Clonar el dispositivo.
- Alterar la información protegida.
- Utilizar una copia no autorizada.
- Modificar el contenido criptográfico de la llave.

Si el sistema determina que una llave ha sido comprometida o presenta una anomalía de seguridad, debe:

1. Invalidar la llave en el servidor.
2. Invalidar la información necesaria para la autenticación.
3. Marcar la llave como **COMPROMETIDA** o **REVOCADA**.
4. Impedir nuevos accesos utilizando esa llave.
5. Registrar el evento.
6. Permitir al administrador generar posteriormente una nueva llave.

### Importante

La detección de clonación o copia debe basarse en mecanismos criptográficos y de identificación de la llave.

No se debe asumir que una aplicación puede detectar cualquier copia física de una USB únicamente observando los archivos.

---

# 10. Validación de la llave durante el ingreso

Al intentar ingresar a la aplicación administrativa, el sistema debe solicitar:

1. Usuario.
2. Contraseña.
3. Llave física USB.

El sistema debe validar que la llave física corresponda al usuario que está intentando ingresar.

El computador utilizado **no forma parte de la asociación de la llave**.

Por lo tanto, un administrador puede utilizar su llave válida desde cualquier computador que cumpla los requisitos técnicos de la aplicación.

También pueden utilizar el mismo computador varios administradores, cada uno utilizando su propia llave.

---

# 11. Validación del usuario y la llave

El sistema debe validar conjuntamente:

### Usuario

- Que exista.
- Que esté activo.
- Que tenga permisos de administrador.

### Contraseña

- Que corresponda al usuario ingresado.
- Que sea válida según el mecanismo de autenticación actual.

### Llave

- Que exista físicamente.
- Que sea válida criptográficamente.
- Que esté activa.
- Que corresponda al usuario ingresado.
- Que no esté revocada.
- Que no esté bloqueada.
- Que no haya sido invalidada por una nueva llave.

El acceso solamente debe permitirse cuando todas las validaciones sean correctas.

---

# 12. Comportamiento cuando no existe una llave válida

Si el administrador intenta ingresar y el sistema no detecta una llave física válida, **no debe permitir el acceso**.

El sistema debe informar claramente al usuario que debe conectar una llave válida.

Ejemplo:

```text
No se ha detectado una llave de administrador válida.

Conecte la llave USB asociada al usuario
para continuar.

[ Reintentar ]

```

Si se detecta una USB pero la llave no corresponde al usuario ingresado, el sistema debe informar:

```text
La llave conectada no corresponde al usuario seleccionado.

Conecte la llave de administrador correspondiente
para continuar.

[ Reintentar ]

```

Si la llave está revocada, bloqueada o invalidada:

```text
La llave de administrador no es válida.

La llave ha sido revocada, bloqueada o invalidada.

Contacte al administrador del sistema para generar
una nueva llave.

[ Aceptar ]

```

El sistema no debe permitir continuar utilizando únicamente usuario y contraseña.

---

# 13. Relación entre USB y usuario

La llave física debe mantener una asociación directa con el usuario administrador.

Ejemplo:

```text
Usuario: administrador01
       │
       └── Llave: KEY-8F72A91C
                  Estado: ACTIVA

```

Otro usuario debe utilizar su propia llave:

```text
Usuario: administrador02
       │
       └── Llave: KEY-71B42D55
                  Estado: ACTIVA

```

No debe ser posible utilizar:

```text
Usuario: administrador01
Llave: KEY-71B42D55

```

para obtener acceso como `administrador01`.

---

# 14. La llave no reemplaza usuario y contraseña

La llave física es un mecanismo adicional de seguridad.

El ingreso a la aplicación web continúa requiriendo:

```text
Usuario
+
Contraseña
+
Llave física válida asociada al usuario

```

Todos los elementos deben cumplir las validaciones correspondientes.

La llave física **no reemplaza el usuario ni la contraseña**.

---

# 15. Reemplazo de una llave dañada

Si la USB se daña, se pierde o deja de funcionar correctamente, el administrador debe poder reemplazarla.

El proceso será:

1. Ingresar a **Instalaciones**.
2. Seleccionar **Registrar llave de administrador**.
3. Seleccionar el usuario administrador correspondiente.
4. Conectar una nueva USB.
5. El sistema detectará el nuevo medio.
6. El sistema advertirá que la nueva llave reemplazará la anterior.
7. El administrador confirma.
8. El sistema genera una nueva llave.
9. La nueva llave queda asociada al usuario.
10. La llave anterior queda invalidada.
11. El servidor actualiza el registro.
12. La nueva llave pasa a estado **ACTIVA**.

---

# 16. Regeneración de llave

Cada vez que se genere una nueva llave para un usuario que ya posee una llave activa:

```text
LLAVE ANTERIOR
       ↓
   INVALIDADA
       ↓
LLAVE NUEVA
       ↓
     ACTIVA

```

La llave anterior no debe seguir siendo válida.

No debe existir un período en el que ambas llaves puedan utilizarse, salvo que el sistema implemente explícitamente múltiples llaves por usuario.

---

# 17. Actualización en el servidor

Cuando se genere una nueva llave, el servidor debe actualizar la información correspondiente al usuario.

Debe conservar como mínimo:

```text
Usuario
Identificador de llave
Estado
Fecha de creación
Fecha de activación
Fecha de última utilización
Información criptográfica necesaria

```

**No se debe almacenar información de asociación con el computador**, ya que la llave no está vinculada a una máquina específica.

La información de la llave anterior debe quedar registrada para mantener trazabilidad, pero su estado debe cambiar a:

```text
REVOCADA

```

o

```text
INVALIDADA

```

según el modelo utilizado.

---

# 18. Seguridad ante una llave anterior

Después de generar una nueva llave:

```text
USB anterior → NO VÁLIDA
USB nueva    → VÁLIDA

```

La llave anterior no debe poder utilizarse nuevamente aunque:

- El usuario y contraseña sean correctos.
- La USB anterior todavía funcione.
- Se conecte a cualquier computador.
- Se copien sus archivos.
- Se intente utilizar después de reiniciar el sistema.

La validación final siempre debe consultar el estado registrado en el servidor.

---

# 19. Uso de un mismo computador por varios administradores

El sistema **no debe asociar la llave física a un computador específico**.

Un mismo computador puede ser utilizado por diferentes administradores.

Cada administrador debe ingresar utilizando:

```text
Usuario propio
+
Contraseña propia
+
Llave propia

```

Ejemplo:

```text
COMPUTADOR 1

Administrador A
Usuario A + Contraseña A + Llave A
                    ↓
                 ACCESO

Administrador B
Usuario B + Contraseña B + Llave B
                    ↓
                 ACCESO

```

La autorización debe depender de la correspondencia entre el usuario ingresado y la llave física conectada.

---

# 20. Principio de seguridad

La USB debe considerarse únicamente como una parte del mecanismo de autenticación.

La seguridad no debe depender exclusivamente de un archivo almacenado físicamente en ella.

El modelo debe ser:

```text
              SERVIDOR
                  │
       ┌──────────┴──────────┐
       │                     │
Información criptográfica   Usuario
       │                     │
       └──────────┬──────────┘
                  │
             VALIDACIÓN
                  │
       ┌──────────┴──────────┐
       │                     │
   USB válida        Usuario + Contraseña
       │                     │
       └──────────┬──────────┘
                  │
                  ↓
           ACCESO PERMITIDO

```

No existe ninguna asociación con el computador.

---

# 21. Regla fundamental

La implementación debe respetar estas reglas:

1. La llave física es diferente e independiente del usuario y contraseña.
2. La llave queda asociada a un usuario administrador.
3. Solo se pueden configurar llaves para usuarios administradores.
4. La instalación se realiza desde **Instalaciones → Registrar llave de administrador**.
5. El sistema debe detectar una memoria USB para generar la llave.
6. La USB será preparada por el sistema y los datos existentes serán eliminados después de la confirmación del administrador.
7. La aplicación no debe hacerse responsable de archivos ajenos al sistema almacenados previamente en la USB.
8. La aplicación no debe realizar copias de seguridad, recuperación ni administración de archivos ajenos al sistema.
9. Una llave no puede utilizarse simplemente copiando sus archivos a otra USB.
10. La información almacenada en la USB no es suficiente por sí sola para autenticarse.
11. El servidor debe conservar la información necesaria para validar la llave.
12. La llave debe estar asociada al usuario correspondiente.
13. **La llave no debe asociarse a ningún computador específico.**
14. Un mismo computador puede ser utilizado por uno o varios administradores.
15. Usuario y contraseña continúan siendo obligatorios.
16. Si no se detecta una llave válida, el sistema debe informar al usuario que debe conectar una llave válida.
17. Si la llave conectada no corresponde al usuario ingresado, el acceso debe ser rechazado.
18. Si se genera una nueva llave para un usuario que ya posee una, la anterior debe quedar invalidada.
19. Una llave perdida, dañada o comprometida puede ser reemplazada mediante la generación de una nueva.
20. Al generar la nueva llave, la anterior deja de ser válida.
21. Todas las operaciones relacionadas con las llaves deben quedar registradas para mantener trazabilidad.
22. La validación definitiva de la llave, su usuario asociado y su estado debe realizarse contra el servidor.

---

# 22. Resultado esperado

El sistema debe garantizar que el acceso administrativo requiera:

**Usuario + Contraseña + Llave física válida asociada al usuario**

y que:

- Una llave física por sí sola no sea suficiente para obtener acceso.
- Una copia de los archivos de la llave no sea suficiente para obtener acceso.
- Una USB clonada no sea suficiente para obtener acceso.
- Una llave revocada no pueda utilizarse.
- Una llave asociada a otro usuario no pueda utilizarse.
- Una llave válida pueda utilizarse desde diferentes computadores.
- Diferentes administradores puedan utilizar el mismo computador con sus respectivas llaves.
- La ausencia de una llave válida impida el acceso.
- El sistema informe al usuario que debe conectar una llave válida cuando esta no sea detectada.
- La generación de una nueva llave invalide automáticamente la llave anterior.
- La aplicación utilice la USB exclusivamente como medio físico para la llave de autenticación.
- La aplicación no asuma responsabilidad alguna sobre archivos, información o contenido ajeno al sistema que pudiera existir previamente en el medio USB.

