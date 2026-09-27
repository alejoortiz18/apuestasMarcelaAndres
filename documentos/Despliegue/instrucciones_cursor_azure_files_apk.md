# Instrucción para Cursor: completar integración de Azure Files para almacenar y distribuir APK

## Objetivo

Completar en el proyecto existente de la API .NET la integración con **Azure Files** para guardar, consultar y descargar las versiones APK de la aplicación Android.

La API y la base de datos ya están desplegadas y funcionando en Azure App Service. La cuenta de almacenamiento y el recurso compartido ya fueron creados desde el portal. Las variables de entorno ya se agregaron al App Service.

Cursor debe **inspeccionar primero el repositorio y la arquitectura actual**, implementar únicamente lo necesario, ejecutar pruebas y validaciones, y documentar claramente cualquier paso que requiera intervención manual en Azure.

No rehacer el API, no reemplazar su arquitectura y no modificar funcionalidades ajenas a la gestión de APK.

---

## 1. Infraestructura existente (confirmar antes de usar)

### Azure

- Grupo de recursos: `appProd`
- App Service de la API: `api-ventas-prod`
- Cuenta de almacenamiento: `storageapp`
- Servicio: Azure Files, recurso compartido clásico SMB
- Modelo: pago por uso
- Nivel de acceso del recurso compartido: **Transacción optimizada**
- Redundancia: **LRS**
- Acceso de red pública: habilitado desde todas las redes
- Recurso compartido creado: el nombre observado durante la configuración es `apptogerversion`. **Verificar el nombre exacto en Azure antes de asumirlo**, porque debe coincidir exactamente con el recurso compartido real.

### Variables de entorno ya agregadas en App Service

| Variable | Contenido esperado |
|---|---|
| `AzureFilesConnection` | Cadena de conexión de la cuenta `storageapp` (secreto; no mostrar ni registrar) |
| `AzureFilesShareName` | Nombre exacto del recurso compartido existente; se espera `apptogerversion`, verificar en Azure |

Importante:
- En Azure App Service las variables de entorno se exponen a .NET como configuración.
- No copiar la cadena de conexión a `appsettings.json`, archivos versionados, documentación, logs, respuestas HTTP ni pruebas.
- No solicitar la cadena de conexión al usuario ni imprimirla.
- No modificar ni eliminar la configuración existente de conexión a la base de datos.
- No crear otra cuenta de almacenamiento ni otro recurso compartido.
- No habilitar redes privadas, VNet ni Private Endpoint para esta implementación.
- No habilitar acceso anónimo al recurso compartido.
- No modificar firewall/redes sin necesidad y autorización.

---

## 2. Inspección obligatoria del proyecto

Antes de escribir código:

1. Identificar la solución y el proyecto API que se despliega en `api-ventas-prod`.
2. Identificar versión de .NET, estructura de `Program.cs`, inyección de dependencias, configuración, logging y manejo global de errores.
3. Identificar el patrón actual de controladores/endpoints, DTOs, validaciones, autenticación/autorización y respuestas HTTP.
4. Revisar si ya existe módulo de versiones móviles, gestión de APK, carga de archivos, almacenamiento externo o tabla de versiones en la base de datos.
5. Revisar cómo se ejecutan pruebas unitarias/de integración y cómo se publica actualmente el API.
6. Revisar `appsettings.*.json` y asegurar que no contienen secretos de Azure.
7. Resumir brevemente lo encontrado y luego implementar respetando los patrones existentes.

No inventar nombres de proyectos, rutas, tablas, roles ni contratos de API si ya hay convenciones en el repositorio.

---

## 3. Paquete y configuración .NET

Agregar al proyecto API el paquete oficial NuGet `Azure.Storage.Files.Shares`, en una versión estable compatible con el Target Framework y las dependencias existentes. No actualizar paquetes no relacionados.

Leer la configuración mediante `IConfiguration`/Options con estos nombres exactos:

- `AzureFilesConnection`
- `AzureFilesShareName`

Validar que ambos valores existan. Si falta alguno, mostrar un error claro que indique el nombre de la configuración faltante, **nunca su valor secreto**.

Registrar el cliente/servicio mediante Dependency Injection siguiendo el estilo existente. Reutilizar clientes de forma segura; no crear clientes innecesariamente en cada petición.

No poner la cadena de conexión en código ni en `appsettings.json`.

---

## 4. Servicio de almacenamiento

Crear un servicio dedicado (nombre según convenciones del proyecto, por ejemplo `IApkStorageService` / `AzureFilesApkStorageService`) responsable de operaciones Azure Files.

Debe poder:

1. Comprobar que el recurso compartido existe y que la API puede acceder.
2. Crear, si no existe, un directorio dedicado para APK dentro del recurso compartido (por ejemplo `apks`). No crear carpetas fuera del recurso compartido.
3. Subir un APK usando un nombre de archivo seguro y estable.
4. Consultar metadatos básicos: nombre, tamaño y fecha de modificación, cuando Azure Files los proporcione.
5. Descargar un archivo mediante streaming, sin cargar archivos grandes completos en memoria innecesariamente.
6. Comprobar si un archivo existe.
7. Devolver errores controlados y registrables sin incluir credenciales.

Requisitos:
- Usar clientes oficiales del SDK (`ShareClient`, `ShareDirectoryClient`, `ShareFileClient` o equivalentes).
- Usar operaciones asíncronas y `CancellationToken` cuando corresponda.
- No concatenar rutas proporcionadas directamente por el usuario. Validar nombres y bloquear `..`, separadores de ruta y caracteres inválidos.
- No sobrescribir silenciosamente una versión existente. Preferir rechazar duplicados; reemplazar solo si existe una operación administrativa explícitamente autorizada.
- No borrar versiones automáticamente.
- No registrar el contenido del APK ni secretos.
- Manejar errores de red, autenticación, recurso compartido inexistente, archivo inexistente, cuota/capacidad y timeout con logs útiles y respuestas HTTP apropiadas.
- No devolver rutas internas del Storage ni la cadena de conexión al cliente.

---

## 5. API para versiones APK

Inspeccionar primero si ya existe un controlador/endpoint para versiones móviles. Si existe, ampliarlo manteniendo compatibilidad. Si no existe, crear un módulo pequeño coherente con la arquitectura actual.

### A. Cargar/publicar APK
- Recibir `multipart/form-data` con el APK y los metadatos mínimos que el proyecto ya maneje (por ejemplo versión, número de compilación, plataforma y notas de versión).
- Proteger el endpoint con la autenticación/autorización administrativa existente. Si no existe mecanismo adecuado, no inventar una clave débil ni dejar la carga pública: documentar la decisión y solicitar definición antes de habilitar publicación pública.
- Validar extensión `.apk`, nombre, tamaño máximo configurable y que el archivo no esté vacío.
- No confiar únicamente en el `Content-Type` enviado por el cliente.
- Guardar con nombre seguro, preferiblemente generado a partir de una versión validada, sin rutas proporcionadas por el usuario.
- Responder con identificador/versión publicada y metadatos, no con credenciales ni ruta SMB.
- Si ya existe persistencia de versiones en BD, reutilizarla. Si no existe, determinar si se necesita registrar versión/estado/fecha para ofrecer “última versión”; no crear tablas/migraciones sin seguir el patrón actual y documentar el cambio.

### B. Consultar versión disponible
- Proveer o adaptar un endpoint para que Android consulte la versión vigente y sepa si hay actualización.
- Responder con los campos que ya utilice el cliente móvil (versión, compilación, notas, obligatoriedad si aplica).
- No asumir que el archivo más reciente por fecha es la versión vigente si existe un estado o versión configurada en BD.

### C. Descargar APK
- Proveer o adaptar endpoint de descarga por identificador/versión.
- Entregar el archivo con `application/vnd.android.package-archive` y nombre de descarga apropiado.
- Preferir streaming.
- Verificar que la versión solicitada exista y esté habilitada para descarga.
- No exponer acceso anónimo directo al recurso compartido.
- Respetar la autenticación de descarga actual. Si Android necesita descargar sin iniciar sesión para actualizarse, confirmar que solo se expone el APK publicado y que no se exponen operaciones administrativas de carga/listado.

### D. Listado administrativo (si aplica)
- Listar versiones y metadatos desde la fuente de verdad existente (BD si existe).
- No enumerar indiscriminadamente archivos del recurso compartido a usuarios finales.

Usar rutas, nombres y contratos compatibles con el proyecto actual; documentar los endpoints finales en el informe.

---

## 6. Seguridad

- Mantener privado el recurso compartido.
- No devolver ni registrar la cadena de conexión.
- No añadir secretos al repositorio ni a archivos de ejemplo con valores reales.
- Validar autorización en publicación, reemplazo y eliminación.
- No implementar eliminación si no es necesaria para el requisito actual.
- Validar extensión, tamaño, versión y nombre del archivo.
- Evitar path traversal y sobrescritura de archivos no relacionados.
- No exponer mensajes internos de Azure al cliente; registrar detalles técnicos de forma segura y devolver mensajes controlados.
- No modificar CORS, autenticación global, reglas de red o permisos existentes salvo necesidad estricta y explicada.
- Si se detecta que la cadena de conexión fue comprometida o versionada, detenerse y reportarlo; no copiarla en la salida.

---

## 7. Pruebas y validaciones obligatorias

Crear/actualizar pruebas siguiendo el framework existente. Como mínimo:

1. Configuración ausente: falla con mensaje claro y sin revelar secretos.
2. Nombre inválido/path traversal: rechazado.
3. Archivo vacío o mayor al límite: rechazado.
4. Archivo que no es APK: rechazado según la validación definida.
5. Publicación duplicada: comportamiento explícito y probado.
6. Archivo inexistente al descargar: HTTP 404.
7. Usuario no autorizado al publicar: HTTP 401/403 según esquema existente.
8. Descarga con nombre y content type correctos.
9. No se filtran connection strings en logs ni respuestas.

Para pruebas del servicio, preferir interfaces/mocks o emuladores compatibles; no hacer que pruebas unitarias dependan de Azure real.

### Validación real en Azure

Si el entorno tiene Azure CLI autenticado o herramientas autorizadas:

1. Verificar que la cuenta `storageapp` y el recurso compartido configurado existan.
2. Verificar que App Service tenga ambas variables (sin imprimir `AzureFilesConnection`).
3. Confirmar que el API aplicó la configuración/reinició si corresponde.
4. Ejecutar una operación real y controlada desde el API/App Service (por ejemplo comprobar existencia del share y directorio `apks`).
5. Subir un APK de prueba solo si el usuario proporcionó uno o autorizó expresamente usar uno. No crear ni publicar un APK ficticio.
6. Si se sube una prueba, descargarla y comparar tamaño/hash; eliminarla únicamente si fue creada para la prueba y la eliminación está autorizada.
7. No ejecutar despliegue de producción, cambios destructivos ni rotación de claves sin autorización explícita. Si el usuario solicita completar el despliegue, seguir el flujo de publicación existente, mostrar qué se desplegará y evitar cambios ajenos al módulo.
8. Si no se puede validar Azure desde Cursor, indicar exactamente qué validación queda pendiente y dar instrucciones concisas para hacerla.

No declarar “conexión exitosa” basándose únicamente en que las variables existen. Debe existir una operación real contra Azure Files que lo confirme.

---

## 8. Despliegue y compatibilidad

- Mantener intacta la conexión existente a SQL Server y el funcionamiento actual del API.
- No cambiar framework, runtime, configuración de IIS/App Service ni pipeline salvo necesidad comprobada.
- Ejecutar build y pruebas antes de desplegar.
- Revisar los cambios y evitar modificaciones no relacionadas.
- Seguir el mecanismo de despliegue existente.
- Después de desplegar, revisar logs/health endpoint y ejecutar una prueba funcional segura de conectividad.
- No borrar APK existentes ni modificar datos de producción.

---

## 9. Informe final requerido

Al terminar, presentar un informe conciso con:

1. Archivos creados/modificados.
2. Paquete NuGet y versión agregada.
3. Confirmación de uso de `AzureFilesConnection` y `AzureFilesShareName`, sin mostrar valores secretos.
4. Nombre real del recurso compartido confirmado, o indicar que quedó pendiente verificarlo.
5. Endpoints creados/modificados, método HTTP, ruta, autorización y ejemplo de uso sin secretos.
6. Resultado del build y pruebas.
7. Resultado de la validación real contra Azure Files.
8. Si hubo despliegue, indicar qué versión se desplegó y resultado; si no, explicar qué falta.
9. Pasos manuales pendientes en Azure o decisiones funcionales que requieran al usuario.

**Criterio de terminado:** la API puede conectarse realmente al recurso compartido privado, publicar un APK autorizado, consultar la versión publicada y descargarla mediante los endpoints definidos, sin afectar la base de datos ni el funcionamiento actual del API.
