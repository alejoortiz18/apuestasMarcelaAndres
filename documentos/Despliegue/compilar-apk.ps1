<#
Compila el APK de New Rich firmado con la llave del almacén (apkstore) y, si se pide, lo publica.

1. Entra al API con el usuario superusuario.
2. Descarga la llave de firma (api/apkstore/llave/descarga) a un archivo temporal.
3. Compila en Release con esa llave y con el número de compilación indicado.
4. Comprueba que la huella del APK sea la de la llave.
5. Con -Publicar, sube el APK a api/apkstore/versiones.
6. Borra la llave del equipo, pase lo que pase.

La contraseña no se guarda en ningún archivo.

Uso:
  .\compilar-apk.ps1 -NumeroCompilacion 2 [-NombreVersion 1.0] [-Publicar] [-UrlApi https://...] [-Usuario super]
Pide la contraseña del superusuario si no se pasa -Password.
#>
param(
    [Parameter(Mandatory)][int]$NumeroCompilacion,
    [string]$NombreVersion = '1.0',
    [switch]$Publicar,
    [string]$UrlApi = 'https://newrich-cxhea7a9g6d2c3ce.centralus-01.azurewebsites.net/',
    [string]$Usuario = 'super',
    [string]$Password,
    [string]$AndroidSdk = 'C:\Program Files (x86)\Android\android-sdk',
    [string]$Jdk = 'C:\Program Files\Microsoft\jdk-17.0.20.101-hotspot'
)

$ErrorActionPreference = 'Stop'
$raiz = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$proyecto = Join-Path $raiz 'Proyectos\Android\src\NewRich.Maui\NewRich.Maui.csproj'
$salida = Join-Path $raiz "tmp\compilar-apk\$NumeroCompilacion"
$UrlApi = $UrlApi.TrimEnd('/') + '/'

if (-not $Password) {
    $seguro = Read-Host "Contraseña de $Usuario" -AsSecureString
    $Password = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($seguro))
}

function Llamar($metodo, $ruta, $cuerpo, $token) {
    $cabeceras = @{}
    if ($token) { $cabeceras.Authorization = "Bearer $token" }
    try {
        $json = if ($cuerpo) { $cuerpo | ConvertTo-Json } else { $null }
        return Invoke-RestMethod -Method $metodo -Uri ($UrlApi + $ruta) -Headers $cabeceras -Body $json -ContentType 'application/json' -TimeoutSec 120
    } catch {
        $detalle = $_.ErrorDetails.Message
        throw "El API respondió con error en $ruta. $detalle"
    }
}

$login = Llamar 'Post' 'api/Auth/login' @{ usuario = $Usuario; password = $Password } $null
$token = $login.data.token
$llave = (Llamar 'Post' 'api/apkstore/llave/descarga' @{ password = $Password } $token).data

$temporal = Join-Path ([IO.Path]::GetTempPath()) ("newrich-firma-" + [Guid]::NewGuid().ToString('N') + '.p12')
try {
    [IO.File]::WriteAllBytes($temporal, [Convert]::FromBase64String($llave.almacen))

    if (Test-Path $salida) { Remove-Item $salida -Recurse -Force }
    # La compilación incremental conserva el manifiesto generado con el versionCode anterior.
    foreach ($carpeta in 'obj\Release\net10.0-android', 'bin\Release\net10.0-android') {
        $ruta = Join-Path (Split-Path $proyecto) $carpeta
        if (Test-Path $ruta) { Remove-Item $ruta -Recurse -Force }
    }
    dotnet publish $proyecto -f net10.0-android -c Release -o $salida -nologo -v q `
        "-p:AndroidSdkDirectory=$AndroidSdk" "-p:JavaSdkDirectory=$Jdk" `
        "-p:ApplicationVersion=$NumeroCompilacion" "-p:ApplicationDisplayVersion=$NombreVersion" `
        "-p:AndroidPackageFormats=apk" "-p:AndroidKeyStore=true" `
        "-p:AndroidSigningKeyStore=$temporal" "-p:AndroidSigningKeyAlias=$($llave.alias)" `
        "-p:AndroidSigningStorePass=$($llave.clave)" "-p:AndroidSigningKeyPass=$($llave.clave)"
    if ($LASTEXITCODE -ne 0) { throw 'No compiló la aplicación.' }
} finally {
    Remove-Item $temporal -Force -ErrorAction SilentlyContinue
}

$apk = Get-ChildItem $salida -Filter '*-Signed.apk' | Select-Object -First 1
if (-not $apk) { throw "No se encontró el APK firmado en $salida" }

$certificado = & (Join-Path $Jdk 'bin\keytool.exe') -printcert -jarfile $apk.FullName 2>&1 |
    Select-String 'SHA256:' | Select-Object -First 1
$huella = ($certificado.Line -replace '.*SHA256:\s*', '' -replace ':', '').Trim()
if ($huella -ne $llave.huella) { throw "El APK no quedó firmado con la llave del almacén (huella $huella)." }

$aapt = Get-ChildItem 'C:\Program Files\dotnet\packs\Microsoft.Android.Sdk.Windows' -Recurse -Filter 'aapt2.exe' |
    Sort-Object FullName -Descending | Select-Object -First 1
$paquete = (& $aapt.FullName dump badging $apk.FullName 2>&1 | Select-Object -First 1) -as [string]
if ($paquete -notmatch "versionCode='$NumeroCompilacion' versionName='$([regex]::Escape($NombreVersion))'") {
    throw "El APK no quedó con la compilación $NumeroCompilacion y la versión $NombreVersion. aapt2: $paquete"
}

$final = Join-Path $salida "newrich-$NombreVersion-$NumeroCompilacion.apk"
Copy-Item $apk.FullName $final -Force
Write-Host "APK firmado con la llave del almacén: $final"

if ($Publicar) {
    Add-Type -AssemblyName System.Net.Http
    $cliente = New-Object System.Net.Http.HttpClient
    $cliente.Timeout = [TimeSpan]::FromMinutes(30)
    $cliente.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue('Bearer', $token)
    $formulario = New-Object System.Net.Http.MultipartFormDataContent
    $flujo = [IO.File]::OpenRead($final)
    try {
        $archivo = New-Object System.Net.Http.StreamContent($flujo)
        $archivo.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue('application/vnd.android.package-archive')
        $formulario.Add($archivo, 'archivo', [IO.Path]::GetFileName($final))
        $formulario.Add((New-Object System.Net.Http.StringContent($NombreVersion)), 'nombreVersion')
        $formulario.Add((New-Object System.Net.Http.StringContent("$NumeroCompilacion")), 'numeroCompilacion')
        $respuesta = $cliente.PostAsync($UrlApi + 'api/apkstore/versiones', $formulario).GetAwaiter().GetResult()
        $texto = $respuesta.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    } finally {
        $flujo.Dispose()
        $cliente.Dispose()
    }
    if (-not $respuesta.IsSuccessStatusCode) { throw "No se publicó la versión (HTTP $([int]$respuesta.StatusCode)). $texto" }
    $r = $texto | ConvertFrom-Json
    Write-Host "Versión publicada: compilación $($r.data.numeroCompilacion), SHA-256 $($r.data.sha256)"
}
