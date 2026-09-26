# Deploy-NewRich.ps1

# Publicación de NewRich MVC en IIS

# .NET 10 - Producción

$ErrorActionPreference = "Stop"

# ============================================================

# CONFIGURACIÓN

# ============================================================

$ApiUrl = "https://api-ventas-prod-ffh4dmdhgpcsapda.westus3-01.azurewebsites.net/"

$ProjectPath = "D:\Proyectos\NewRich\apuestasMarcelaAndres\Proyectos\Administrador\NewRich.Admin.csproj"

$PublishDir = "C:\inetpub\rich"

$SiteName = "newrich"

$AppPoolName = "newrich"

$Port = 8081

$DotnetPath = "C:\Program Files\dotnet\dotnet.exe"

$AspNetCoreModulePath = "C:\Program Files\IIS\Asp.Net Core Module\V2"

$BackupRoot = "C:\inetpub\backups"

$PlatformToolsUrl = "https://dl.google.com/android/repository/platform-tools-latest-windows.zip"

$PlatformToolsSourcePath = Join-Path $env:TEMP "newrich-platform-tools\platform-tools"

# ============================================================

# VALIDAR ADMINISTRADOR

# ============================================================

function Ensure-Administrator {

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()

$principal = New-Object Security.Principal.WindowsPrincipal($identity)

$adminRole = [Security.Principal.WindowsBuiltInRole]::Administrator

if (-not $principal.IsInRole($adminRole)) {

    throw "Ejecuta PowerShell como Administrador."

}

function Ensure-PlatformToolsSource {

    $adbSourcePath = Join-Path $PlatformToolsSourcePath "adb.exe"
    if (Test-Path $adbSourcePath) {
        return
    }

    $platformToolsRoot = Split-Path $PlatformToolsSourcePath -Parent
    $archivePath = Join-Path $env:TEMP "platform-tools-latest-windows.zip"
    New-Item -ItemType Directory -Path $platformToolsRoot -Force | Out-Null

    Write-Host "Descargando Android Platform Tools oficiales..." -ForegroundColor Cyan
    Invoke-WebRequest -Uri $PlatformToolsUrl -OutFile $archivePath
    Expand-Archive -LiteralPath $archivePath -DestinationPath $platformToolsRoot -Force

    if (-not (Test-Path $adbSourcePath)) {
        throw "No se encontró adb.exe después de descargar Android Platform Tools."
    }

}

function Copy-PlatformTools {

    $destination = Join-Path $PublishDir "tools\platform-tools"
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Copy-Item -Path (Join-Path $PlatformToolsSourcePath "*") -Destination $destination -Recurse -Force

    $poolIdentity = "IIS AppPool\$AppPoolName"
    & icacls.exe $destination /grant "${poolIdentity}:(OI)(CI)(RX)" /T /C | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "No se pudieron conceder permisos de lectura y ejecución de ADB a $poolIdentity."
    }

    return (Join-Path $destination "adb.exe")

}

}

# ============================================================

# VALIDAR IIS Y ASP.NET CORE

# ============================================================

function Ensure-IIS {

if (-not (Test-Path $DotnetPath)) {

    throw "No se encontró dotnet.exe en: $DotnetPath"

}

if (-not (Test-Path $ProjectPath)) {

    throw "No se encontró el proyecto: $ProjectPath"

}

if (-not (Test-Path $AspNetCoreModulePath)) {

    throw "No se encontró AspNetCoreModuleV2. Verifica la instalación del .NET Hosting Bundle."

}

Ensure-PlatformToolsSource

try {

    Import-Module WebAdministration -ErrorAction Stop

}
catch {

    throw "No se pudo cargar el módulo WebAdministration de IIS."

}

$was = Get-Service WAS

$w3svc = Get-Service W3SVC

if ($was.Status -ne "Running") {

    Start-Service WAS

}

if ($w3svc.Status -ne "Running") {

    Start-Service W3SVC

}

}

# ============================================================

# ACTUALIZAR APPSETTINGS

# ============================================================

function Update-PublishedAppSettings {

param(
    [string]$Path,
    [string]$ApiBaseUrl,
    [string]$AdbExecutablePath
)

$appSettingsPath = Join-Path $Path "appsettings.json"

if (-not (Test-Path $appSettingsPath)) {

    throw "No se encontró appsettings.json en $Path"

}

$json = Get-Content -Path $appSettingsPath -Raw | ConvertFrom-Json

if ($null -eq $json.Api) {

    $json | Add-Member -MemberType NoteProperty -Name Api -Value ([PSCustomObject]@{})

}

$json.Api.BaseUrl = $ApiBaseUrl
$json.RegistroPda.RutaAdb = $AdbExecutablePath

$json |
    ConvertTo-Json -Depth 100 |
    Set-Content -Path $appSettingsPath -Encoding UTF8

}

# ============================================================

# CONFIGURAR APPLICATION POOL

# ============================================================

function Ensure-AppPool {

param(
    [string]$PoolName
)

if (-not (Test-Path "IIS:\AppPools\$PoolName")) {

    New-Item "IIS:\AppPools\$PoolName" | Out-Null

}

Set-ItemProperty `
    "IIS:\AppPools\$PoolName" `
    -Name managedRuntimeVersion `
    -Value ""

Set-ItemProperty `
    "IIS:\AppPools\$PoolName" `
    -Name managedPipelineMode `
    -Value "Integrated"

Set-ItemProperty `
    "IIS:\AppPools\$PoolName" `
    -Name enable32BitAppOnWin64 `
    -Value $false

Set-ItemProperty `
    "IIS:\AppPools\$PoolName" `
    -Name processModel.loadUserProfile `
    -Value $true

}

# ============================================================

# CONFIGURAR SITIO IIS

# ============================================================

function Ensure-Site {

param(
    [string]$SiteName,
    [string]$PhysicalPath,
    [string]$AppPoolName,
    [int]$Port
)

$site = Get-Website -Name $SiteName -ErrorAction SilentlyContinue

if ($null -eq $site) {

    New-Website `
        -Name $SiteName `
        -PhysicalPath $PhysicalPath `
        -Port $Port `
        -ApplicationPool $AppPoolName | Out-Null

}
else {

    Set-ItemProperty `
        "IIS:\Sites\$SiteName" `
        -Name physicalPath `
        -Value $PhysicalPath

    Set-ItemProperty `
        "IIS:\Sites\$SiteName" `
        -Name applicationPool `
        -Value $AppPoolName

}

}

# ============================================================

# RESPALDAR PUBLICACIÓN ANTERIOR

# ============================================================

function Backup-PublishedApp {

param(
    [string]$SourcePath
)

if (-not (Test-Path $SourcePath)) {

    return

}

$files = Get-ChildItem -Path $SourcePath -Force -ErrorAction SilentlyContinue

if (-not $files) {

    return

}

New-Item -ItemType Directory -Path $BackupRoot -Force | Out-Null

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

$backupPath = Join-Path $BackupRoot "newrich_$timestamp"

Write-Host "Creando respaldo en: $backupPath" -ForegroundColor Cyan

New-Item -ItemType Directory -Path $backupPath -Force | Out-Null

Copy-Item `
    -Path "$SourcePath\*" `
    -Destination $backupPath `
    -Recurse `
    -Force

Write-Host "Respaldo completado." -ForegroundColor Green

}

# ============================================================

# DETENER SITIO

# ============================================================

function Stop-NewRichSite {

param(
    [string]$SiteName
)

$site = Get-Website -Name $SiteName -ErrorAction SilentlyContinue

if ($null -ne $site -and $site.State -eq "Started") {

    Write-Host "Deteniendo sitio IIS: $SiteName"

    Stop-Website -Name $SiteName

}

}

# ============================================================

# PUBLICAR APLICACIÓN

# ============================================================

function Publish-NewRich {

Write-Host "Publicando aplicación MVC..." -ForegroundColor Cyan

& $DotnetPath publish `
    $ProjectPath `
    -c Release `
    -o $PublishDir `
    --nologo

if ($LASTEXITCODE -ne 0) {

    throw "Falló la publicación de la aplicación MVC."

}

}

# ============================================================

# INICIAR SITIO Y APPLICATION POOL

# ============================================================

function Start-NewRichSite {

param(
    [string]$SiteName,
    [string]$AppPoolName
)

$pool = Get-WebAppPoolState -Name $AppPoolName

if ($pool.Value -ne "Started") {

    Start-WebAppPool -Name $AppPoolName

}

$site = Get-Website -Name $SiteName

if ($site.State -ne "Started") {

    Start-Website -Name $SiteName

}

}

# ============================================================

# VALIDAR RESPUESTA HTTP

# ============================================================

function Test-NewRichSite {

param(
    [int]$Port
)

$url = "http://localhost:$Port"

Write-Host "Verificando respuesta HTTP: $url"

Start-Sleep -Seconds 3

try {

    $response = Invoke-WebRequest `
        -Uri $url `
        -UseBasicParsing `
        -TimeoutSec 30

    Write-Host "HTTP Status: $($response.StatusCode)" -ForegroundColor Green

}
catch {

    throw "El sitio fue iniciado, pero no respondió correctamente en $url. Error: $($_.Exception.Message)"

}

}

# ============================================================

# EJECUCIÓN PRINCIPAL

# ============================================================

try {

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " DESPLIEGUE NEW RICH MVC - IIS" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

Ensure-Administrator

Ensure-IIS

Write-Host "[1/7] Preparando directorio de publicación..."

New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null

Write-Host "[2/7] Configurando Application Pool..."

Ensure-AppPool -PoolName $AppPoolName

Write-Host "[3/7] Configurando sitio IIS..."

Ensure-Site `
    -SiteName $SiteName `
    -PhysicalPath $PublishDir `
    -AppPoolName $AppPoolName `
    -Port $Port

Write-Host "[4/7] Deteniendo sitio para publicación..."

Stop-NewRichSite -SiteName $SiteName

Write-Host "[5/7] Respaldando versión anterior..."

Backup-PublishedApp -SourcePath $PublishDir

Write-Host "[6/7] Publicando aplicación..."

Publish-NewRich

Write-Host "Preparando ADB para el registro de PDA..." -ForegroundColor Cyan

$adbExecutablePath = Copy-PlatformTools

Write-Host "Configurando URL de API productiva..."

Update-PublishedAppSettings `
    -Path $PublishDir `
    -ApiBaseUrl $ApiUrl `
    -AdbExecutablePath $adbExecutablePath

Write-Host "[7/7] Iniciando sitio IIS..."

Start-NewRichSite `
    -SiteName $SiteName `
    -AppPoolName $AppPoolName

Test-NewRichSite -Port $Port

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host " DESPLIEGUE COMPLETADO" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green

Write-Host "Sitio: $SiteName"
Write-Host "Ruta: $PublishDir"
Write-Host "Puerto: $Port"
Write-Host "API: $ApiUrl"
Write-Host "URL: http://localhost:$Port"

}
catch {

Write-Host ""
Write-Host "ERROR DURANTE EL DESPLIEGUE" -ForegroundColor Red
Write-Host $_.Exception.Message -ForegroundColor Red

throw

}
