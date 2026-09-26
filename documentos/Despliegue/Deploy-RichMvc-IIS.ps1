$ErrorActionPreference = "Stop"

$ApiUrl = "https://api-ventas-prod-ffh4dmdhgpcsapda.westus3-01.azurewebsites.net/"
$ProjectPath = "D:\Proyectos\NewRich\apuestasMarcelaAndres\Proyectos\Administrador\NewRich.Admin.csproj"
$PublishDir = "C:\inetpub\rich"
$SiteName = "rich"
$AppPoolName = "rich"

function Ensure-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    $adminRole = [Security.Principal.WindowsBuiltInRole]::Administrator

    if (-not $principal.IsInRole($adminRole)) {
        throw "Este script debe ejecutarse como Administrador. Abre PowerShell como Administrador y vuelve a intentarlo."
    }
}

function Ensure-IIS {
    if (-not (Get-Command "Import-Module" -ErrorAction SilentlyContinue)) {
        throw "PowerShell no dispone de los comandos necesarios para IIS."
    }

    $iisRoot = 'C:\Program Files\IIS'
    $aspNetCoreModule = 'C:\Program Files\IIS\AspNetCoreModuleV2'

    if (-not (Test-Path $iisRoot)) {
        throw "IIS no está instalado en este equipo. Instala IIS y vuelve a ejecutar el script."
    }

    if (-not (Test-Path $aspNetCoreModule)) {
        throw "Falta el módulo ASP.NET Core para IIS (AspNetCoreModuleV2). Instala el .NET 10 Hosting Bundle y vuelve a ejecutar el script."
    }

    try {
        Import-Module WebAdministration -ErrorAction Stop | Out-Null
    }
    catch {
        throw "No se pudo cargar WebAdministration. Asegúrate de tener las funciones de IIS instaladas."
    }
}

function Update-PublishedAppSettings {
    param(
        [string]$Path,
        [string]$ApiBaseUrl
    )

    $appSettingsPath = Join-Path $Path "appsettings.json"
    if (-not (Test-Path $appSettingsPath)) {
        throw "No se encontró appsettings.json publicado en $Path"
    }

    $json = Get-Content -Path $appSettingsPath -Raw | ConvertFrom-Json
    $json.Api = [pscustomobject]@{
        BaseUrl = $ApiBaseUrl
    }

    $json | ConvertTo-Json -Depth 10 | Set-Content -Path $appSettingsPath -Encoding UTF8
}

function Ensure-AppPool {
    param(
        [string]$PoolName
    )

    if (-not (Get-Item "IIS:\AppPools\$PoolName" -ErrorAction SilentlyContinue)) {
        New-Item "IIS:\AppPools\$PoolName" | Out-Null
    }

    Set-ItemProperty "IIS:\AppPools\$PoolName" -Name managedRuntimeVersion -Value ""
    Set-ItemProperty "IIS:\AppPools\$PoolName" -Name managedPipelineMode -Value "Integrated"
}

function Ensure-Site {
    param(
        [string]$SiteName,
        [string]$PhysicalPath,
        [string]$AppPoolName,
        [int]$Port = 80
    )

    if (-not (Get-Website -Name $SiteName -ErrorAction SilentlyContinue)) {
        New-Website -Name $SiteName -PhysicalPath $PhysicalPath -Port $Port -ApplicationPool $AppPoolName | Out-Null
    }
    else {
        Set-ItemProperty "IIS:\Sites\$SiteName" -Name physicalPath -Value $PhysicalPath
        $site = Get-Website -Name $SiteName
        $site.applicationPool = $AppPoolName
        $site | Set-Item
    }
}

function Restart-IisServices {
    param(
        [string]$AppPoolName
    )

    try {
        Restart-WebAppPool -Name $AppPoolName -ErrorAction Stop | Out-Null
    }
    catch {
        Write-Warning "No fue posible reciclar el pool '$AppPoolName'. Se intentará reiniciar IIS."
    }

    try {
        Restart-Service W3SVC -ErrorAction Stop
    }
    catch {
        Write-Warning "No fue posible reiniciar el servicio W3SVC; verifica manualmente en IIS."
    }
}

Ensure-Administrator
Ensure-IIS

Write-Host "[1/5] Verificando que la ruta de destino exista..."
New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null

Write-Host "[2/5] Limpiando contenido previo de C:\inetpub\rich..."
Get-ChildItem -Path $PublishDir -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "[3/5] Publicando la aplicación MVC en Release..."
& "C:\Program Files\dotnet\dotnet.exe" publish $ProjectPath -c Release -o $PublishDir --nologo
if ($LASTEXITCODE -ne 0) {
    throw "La publicación falló. Revisa el proyecto y la instalación del SDK .NET 10."
}

Write-Host "[4/5] Ajustando la URL de la API productiva..."
Update-PublishedAppSettings -Path $PublishDir -ApiBaseUrl $ApiUrl

Write-Host "[5/5] Configurando IIS y reiniciando el sitio..."
Ensure-AppPool -PoolName $AppPoolName
Ensure-Site -SiteName $SiteName -PhysicalPath $PublishDir -AppPoolName $AppPoolName -Port 80
Restart-IisServices -AppPoolName $AppPoolName

Write-Host ""
Write-Host "Despliegue finalizado."
Write-Host "Sitio: $SiteName"
Write-Host "Ruta: $PublishDir"
Write-Host "API: $ApiUrl"
Write-Host "IIS recargado/reiniciado para aplicar la nueva versión."
