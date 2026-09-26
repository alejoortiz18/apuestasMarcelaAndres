# Despliegue de la aplicación MVC en IIS y preparación de APK para PDA/celulares

## 1. Alcance

Este documento aplica únicamente a la aplicación MVC de administración del proyecto NewRich y a la preparación de los archivos APK que el administrador puede instalar en dispositivos PDA y celulares desde la interfaz administrativa.

No cubre la API, la app MAUI de PDA, ni la base de datos SQL Server en esta guía como despliegue final completo; aquí se concentra en:

- publicar la app MVC en IIS en la ruta C:\inetpub\rich
- dejar la configuración de la MVC para que pueda consumir la API esperada
- preparar los APK para instalación desde el administrador

---

## 2. Requisitos previos

### 2.1 Requisitos del servidor

- Windows Server o Windows 10/11 con IIS habilitado
- Acceso administrativo local o por dominio
- Permisos para crear carpetas en C:\inetpub\rich
- .NET 10 Hosting Bundle instalado en el servidor
- Si la app usa HTTPS, un certificado válido o un certificado local para pruebas
- Si la app consume API, la API debe estar disponible en una URL accesible desde el servidor web y desde los clientes

### 2.2 Requisitos del proyecto

La app MVC se compila con .NET 10. Verifica que el servidor tenga el runtime correcto:

- .NET 10 Runtime
- ASP.NET Core 10 Runtime

Se recomienda instalar el Hosting Bundle de .NET 10 para que IIS pueda ejecutar la app correctamente.

---

## 3. Publicación de la aplicación MVC en IIS

### 3.1. Preparar la carpeta destino

Crear la carpeta destino:

```powershell
New-Item -ItemType Directory -Path "C:\inetpub\rich" -Force
```

Si necesitas limpiar versiones anteriores:

```powershell
Remove-Item "C:\inetpub\rich\*" -Recurse -Force -ErrorAction SilentlyContinue
```

### 3.2. Publicar la aplicación MVC

Desde el proyecto de administración, publicar la app con perfil de Release.

Ejemplo de línea de comando:

```powershell
cd "D:\Proyectos\NewRich\apuestasMarcelaAndres\Proyectos\Administrador"
"C:\Program Files\dotnet\dotnet.exe" publish "NewRich.Admin.csproj" -c Release -o "C:\inetpub\rich"
```

Importante:

- La salida debe quedar directamente dentro de C:\inetpub\rich
- La carpeta debe contener el contenido de la app publicada, incluidos archivos de configuración y web.config
- Si la publicación falla por incompatibilidad del proyecto con IIS, se debe revisar primero el runtime y la configuración de la app

### 3.3. Crear el sitio en IIS

1. Abrir Internet Information Services (IIS Manager)
2. Ir a Sitios
3. Click derecho > Agregar sitio web
4. Configurar:
   - Nombre: rich
   - Ruta física: C:\inetpub\rich
   - Puerto: según corresponda, por ejemplo 80 o 8080
   - Hostname: opcional
5. Guardar

### 3.4. Configuración del pool de aplicación

- El pool debe usar el runtime .NET CLR: Sin código administrado
- El modo de canalización: Integrado
- El pool debe apuntar a la identidad adecuada del servidor

Si el sitio usa ASP.NET Core, también es importante revisar que el pool sea compatible con ASP.NET Core y que el Hosting Bundle esté instalado.

### 3.5. Verificar la app

Probablemente la app MVC se arrancará con el puerto configurado. Verificar con:

```powershell
Invoke-WebRequest http://localhost
```

O desde el navegador:

```text
http://localhost
```

Si el sitio se configuró en un puerto específico, usar ese puerto en su lugar.

### 3.6. Reiniciar IIS después del despliegue

Después de publicar la aplicación en C:\inetpub\rich, es obligatorio reiniciar el servicio o reciclar el sitio para que IIS cargue la nueva versión.

Opción 1: reciclar el sitio en IIS

```powershell
Restart-WebAppPool -Name "DefaultAppPool"
```

O, si el sitio tiene un pool específico:

```powershell
Restart-WebAppPool -Name "rich"
```

Opción 2: reiniciar el servicio IIS

```powershell
Restart-Service W3SVC
```

Opción 3: recargar el sitio desde IIS Manager

1. IIS Manager
2. Sitios
3. Seleccionar el sitio `rich`
4. Detener y luego Iniciar

Importante:

- Siempre que se publique una nueva versión de la MVC, se debe reiniciar IIS o reciclar el pool para que tome el cambio.
- Si la app se publica en un sitio activo, se recomienda detener/start o reciclar antes de confirmar que quedó disponible.

---

## 4. Configuración de la app MVC para IIS

### 4.1. Archivo appsettings.json

La app toma la URL base de la API desde:

```json
"Api": {
  "BaseUrl": "https://api-ventas-prod-ffh4dmdhgpcsapda.westus3-01.azurewebsites.net/"
}
```

Esto debe dejarse así para el despliegue del entorno productivo.

La referencia base del proyecto actual es:

- [Proyectos/Administrador/appsettings.json](../Proyectos/Administrador/appsettings.json)

En este despliegue concreto, no se debe dejar apuntando a localhost ni a un servidor interno por defecto.

### 4.2. Configuración obligatoria para producción

En el servidor IIS, la app MVC debe apuntar a la API productiva real:

```json
{
  "Api": {
    "BaseUrl": "https://api-ventas-prod-ffh4dmdhgpcsapda.westus3-01.azurewebsites.net/"
  }
}
```

La API pública queda documentada como:

```text
https://api-ventas-prod-ffh4dmdhgpcsapda.westus3-01.azurewebsites.net/swagger/index.html
```

Esto debe ser el endpoint base que use el MVC para todas sus llamadas al backend.

### 4.3. Variables de entorno

Se recomienda definir `ASPNETCORE_ENVIRONMENT=Production` en IIS para asegurar que la app no use valores de desarrollo.

Ejemplo:

```powershell
[System.Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", "Machine")
```

### 4.3. Variables de entorno

Es posible que en IIS haga falta definir ASPNETCORE_ENVIRONMENT como `Production` si la app se comporta distinto según ambiente.

Ejemplo:

```powershell
[System.Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", "Machine")
```

---

## 5. Configuración de los APK para instalación desde el administrador

### 5.1. Qué valida el proyecto actualmente

El administrador ya tiene lógica para localizar el APK del PDA y ejecutarlo por `adb`.

Esto está definido en:

- [Proyectos/Administrador/Services/Pda/OpcionesRegistroPda.cs](../Proyectos/Administrador/Services/Pda/OpcionesRegistroPda.cs)
- [Proyectos/Administrador/Services/Pda/AdbProceso.cs](../Proyectos/Administrador/Services/Pda/AdbProceso.cs)
- [Proyectos/Administrador/appsettings.json](../Proyectos/Administrador/appsettings.json)
- [Proyectos/API/src/NewRich.Constants/ProvisionPda.cs](../Proyectos/API/src/NewRich.Constants/ProvisionPda.cs)

La parte importante aquí es:

- `RutaApk` apunta a la ruta del APK del PDA
- `RutaAdb` usa `adb`
- `Paquete` esperado es `com.newrich.pda`

### 5.2. Configuración actual esperada por el sistema

La app admin está configurada para buscar este APK:

```json
"RegistroPda": {
  "RutaAdb": "adb",
  "RutaApk": "wwwroot/android/com.newrich.pda-Signed.apk",
  "PuertoPuenteUsb": 5295
}
```

Esto significa que, para que el administrador pueda instalarlo desde el equipo con Windows, el APK debe existir en ese sitio y estar correctamente firmado para Android.

### 5.3. Requisitos para que el APK quede instalable

Para que el administrador pueda instalarlos desde la app a un PDA o celular Android, normalmente se requiere lo siguiente:

1. APK válido generado por compilación de Android/Maui
2. Firma correcta del paquete Android
3. Nombre de paquete consistente con el proyecto
4. `adb` instalado y accesible en PATH
5. El dispositivo conectado por USB con depuración USB habilitada
6. El archivo APK ubicado en una ruta accesible por el administrador

### 5.4. Paquete Android esperado

El proyecto define el paquete esperado como:

```csharp
public const string Paquete = "com.newrich.pda";
```

Esto lo define:

- [Proyectos/API/src/NewRich.Constants/ProvisionPda.cs](../Proyectos/API/src/NewRich.Constants/ProvisionPda.cs)

Por tanto, el archivo APK final debe corresponder a ese paquete para que la instalación del administrador sea consistente con la validación del sistema.

### 5.5. Cómo dejarlo preparado

#### Opción recomendada

Copiar el APK firmado a la carpeta del administrador:

```text
Proyectos/Administrador/wwwroot/android/com.newrich.pda-Signed.apk
```

Esa ruta es relativa al proyecto. Se conserva al pasar el repositorio a otra máquina y al publicar el administrador. El archivo supera el límite de GitHub para archivos normales, así que queda marcado con Git LFS.

#### Verificación mínima antes de instalar

- El archivo existe
- El archivo no está vacío
- El APK tiene firma válida
- El nombre del paquete coincide con `com.newrich.pda`
- `adb devices` detecta el equipo conectado
- El puerto o endpoint del puente USB no es un bloqueo para la instalación desde el administrador

### 5.6. Recomendación para Android

Antes de instalar en PDA o celular, en el dispositivo:

1. Habilitar depuración USB
2. Aceptar la conexión del equipo desde el PC
3. Ejecutar `adb devices` y confirmar que el dispositivo aparece
4. Instalar solo el APK generado para Release

---

## 6. Validación final antes de despliegue

### 6.1 Validación de la MVC

- La app compila en Release
- IIS tiene la carpeta C:\inetpub\rich
- El sitio apunta a esa carpeta
- El pool está configurado correctamente
- La app responde en el puerto esperado
- La URL de la API de la app es la correcta

### 6.2 Validación de APKs

- El archivo APK existe en la ruta indicada por `RegistroPda:RutaApk`
- El APK es del paquete correcto (`com.newrich.pda`)
- El administrador puede localizarlo
- El equipo Android está visible con `adb devices`
- `adb install` funciona sin error

---

## 7. Dudas que no se pueden asumir sin validación

Estas son las cosas que sí requieren confirmación real en el entorno final del cliente antes de declarar despliegue completo:

- Si el servidor IIS tiene el Hosting Bundle instalado
- Si el sitio usará HTTP o HTTPS
- Si la API estará en un dominio interno o externo
- Si el administrador debe instalar APKs desde la app o solo dejar los archivos listos para descarga/manual
- Si los dispositivos PDA usan Android Enterprise o depuración USB directa
- Si el APK final es realmente el firmado y no un debug build accidental

No se debe asumir que un APK sirve solo porque existe en disco. Debe verificarse con el paquete esperado y la instalación real en dispositivo Android.

---

## 8. Resumen práctico

Para dejar la MVC desplegada en IIS en C:\inetpub\rich:

1. Instalar .NET 10 Hosting Bundle
2. Publicar la app MVC en Release a C:\inetpub\rich
3. Crear un sitio IIS apuntando a esa carpeta
4. Configurar el pool y el puerto
5. Ajustar la URL de la API en appsettings para entorno real

Para dejar los APK listos para instalación desde el administrador:

1. Generar el APK Release del proyecto Android/Maui
2. Verificar que el paquete sea `com.newrich.pda`
3. Colocar el APK en la ruta configurada por `RegistroPda:RutaApk`
4. Confirmar que `adb` detecta el dispositivo y el archivo es instalable

---

## 9. Propuesta de siguiente paso

Si quieres, en el siguiente paso puedo dejarte una versión todavía más operativa de este documento con:

- comandos exactos para PowerShell de publicación en IIS
- una plantilla de `web.config` para ASP.NET Core en IIS
- y un checklist final de validación para levantar la app en un servidor real.
