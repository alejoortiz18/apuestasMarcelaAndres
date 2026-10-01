# Publica solo la API de pruebas en IIS.
# Destino: C:\inetpub\apilocal
# URL: http://www.apirichtest.com:8085/
# Base de datos: localhost / NewRich (Windows auth)
# Requiere PowerShell elevado.

$ErrorActionPreference = "Stop"

$dotnet = "C:\Program Files\dotnet\dotnet.exe"
$csproj = "D:\Desarrollo\chances\SWApuestas\Proyectos\API\src\NewRich.Api\NewRich.Api.csproj"
$staging = "D:\Desarrollo\chances\SWApuestas\Proyectos\API\src\NewRich.Api\bin\publish-iis"
$dest = "C:\inetpub\apilocal"
$siteName = "apilocal"
$poolName = "apilocal"
$port = 8085
$hostName = "www.apirichtest.com"
$appcmd = "C:\Windows\System32\inetsrv\appcmd.exe"
$log = "D:\Desarrollo\chances\SWApuestas\documentos\Despliegue\publicar-api-pruebas-iis.log"
$hostsPath = "$env:SystemRoot\System32\drivers\etc\hosts"

function Write-Log([string]$msg) {
    $line = "{0} {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $msg
    Add-Content -Path $log -Value $line
    Write-Host $line
}

Set-Content -Path $log -Value "=== Publicacion API de pruebas ==="

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Log "ERROR: se necesita PowerShell elevado."
    exit 1
}

if (-not (Test-Path $appcmd)) {
    Write-Log "ERROR: no esta IIS (falta appcmd)."
    exit 1
}

Write-Log "Publicando Release a $staging"
& $dotnet publish $csproj -c Release -o $staging
if ($LASTEXITCODE -ne 0) {
    Write-Log "ERROR: dotnet publish fallo con codigo $LASTEXITCODE"
    exit $LASTEXITCODE
}

$appsettingsPath = Join-Path $staging "appsettings.json"
$appsettings = Get-Content $appsettingsPath -Raw
if ($appsettings -notmatch 'Server=localhost;Database=NewRich;') {
    Write-Log "ERROR: la publicacion no apunta a la base local NewRich."
    exit 1
}

if (-not (Test-Path (Join-Path $staging "web.config"))) {
    Write-Log "ERROR: no se genero web.config"
    exit 1
}

Write-Log "Marcando el entorno Development para servir HTTP sin redireccion a HTTPS"
$webConfigPath = Join-Path $staging "web.config"
[xml]$webConfig = Get-Content $webConfigPath
$aspNetCore = $webConfig.configuration.location.'system.webServer'.aspNetCore
if ($null -eq $aspNetCore) {
    Write-Log "ERROR: web.config no tiene el nodo aspNetCore"
    exit 1
}
$aspNetCore.SetAttribute("stdoutLogEnabled", "true")
$aspNetCore.SetAttribute("stdoutLogFile", ".\logs\stdout")
$envVars = $webConfig.CreateElement("environmentVariables")
$envVar = $webConfig.CreateElement("environmentVariable")
$envVar.SetAttribute("name", "ASPNETCORE_ENVIRONMENT")
$envVar.SetAttribute("value", "Development")
$envVars.AppendChild($envVar) | Out-Null
if ($aspNetCore.environmentVariables) {
    $aspNetCore.RemoveChild($aspNetCore.environmentVariables) | Out-Null
}
$aspNetCore.AppendChild($envVars) | Out-Null
$webConfig.Save($webConfigPath)

$pools = & $appcmd list apppool
$sites = & $appcmd list site
Write-Log "Sitios antes:"
$sites | ForEach-Object { Write-Log $_ }

$poolExists = ($pools | Select-String -SimpleMatch "APPPOOL `"$poolName`"") -ne $null
$siteExists = ($sites | Select-String -SimpleMatch "SITE `"$siteName`"") -ne $null
$ocupado = @($sites | Where-Object { $_ -match ":${port}:" -and $_ -notmatch "SITE `"$siteName`"" })
if ($ocupado.Count -gt 0) {
    Write-Log "ERROR: el puerto $port ya lo usa otro sitio:"
    $ocupado | ForEach-Object { Write-Log $_ }
    exit 1
}

if ($poolExists) {
    Write-Log "Deteniendo pool $poolName"
    & $appcmd stop apppool /apppool.name:"$poolName" | Out-Null
}
if ($siteExists) {
    Write-Log "Deteniendo sitio $siteName"
    & $appcmd stop site /site.name:"$siteName" | Out-Null
}

Start-Sleep -Seconds 2

Write-Log "Copiando publicacion a $dest"
New-Item -ItemType Directory -Path $dest -Force | Out-Null
Get-ChildItem -Path $dest -Force | Remove-Item -Recurse -Force
Copy-Item -Path (Join-Path $staging "*") -Destination $dest -Recurse -Force
New-Item -ItemType Directory -Path (Join-Path $dest "logs") -Force | Out-Null

if (-not $poolExists) {
    Write-Log "Creando pool $poolName"
    & $appcmd add apppool /name:"$poolName" /managedRuntimeVersion:"" /managedPipelineMode:Integrated
}
else {
    & $appcmd set apppool /apppool.name:"$poolName" /managedRuntimeVersion:""
}

& $appcmd set apppool /apppool.name:"$poolName" /processModel.identityType:ApplicationPoolIdentity
& $appcmd set config /section:applicationPools "/[name='$poolName'].processModel.idleTimeout:00:20:00"

Write-Log "Permisos de la carpeta para el pool"
& icacls.exe $dest /grant "IIS APPPOOL\${poolName}:(OI)(CI)M" /T /C | Out-Null

Write-Log "Acceso de Windows del pool a la base NewRich"
$sqlPath = Join-Path $env:TEMP "apilocal-sql.sql"
$sql = @"
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'IIS APPPOOL\$poolName')
    CREATE LOGIN [IIS APPPOOL\$poolName] FROM WINDOWS;
USE [NewRich];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'IIS APPPOOL\$poolName')
    CREATE USER [IIS APPPOOL\$poolName] FOR LOGIN [IIS APPPOOL\$poolName];
IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members AS miembros
    INNER JOIN sys.database_principals AS rol ON miembros.role_principal_id = rol.principal_id
    INNER JOIN sys.database_principals AS miembro ON miembros.member_principal_id = miembro.principal_id
    WHERE rol.name = N'db_owner' AND miembro.name = N'IIS APPPOOL\$poolName')
    ALTER ROLE db_owner ADD MEMBER [IIS APPPOOL\$poolName];
"@
[System.IO.File]::WriteAllText($sqlPath, $sql.Trim(), (New-Object System.Text.UTF8Encoding $false))
& sqlcmd -S localhost -E -C -b -i $sqlPath
if ($LASTEXITCODE -ne 0) {
    Write-Log "ERROR: no se pudo dar acceso SQL al pool"
    exit $LASTEXITCODE
}

$bindings = "http/*:${port}:${hostName}"
if (-not $siteExists) {
    Write-Log "Creando sitio $siteName"
    & $appcmd add site /name:"$siteName" /physicalPath:"$dest" /bindings:"$bindings"
}
else {
    Write-Log "Actualizando ruta del sitio $siteName"
    & $appcmd set vdir "/vdir.name:${siteName}/" /physicalPath:"$dest"
}

$bindingsActuales = & $appcmd list site "$siteName" /text:bindings
if ($bindingsActuales -notmatch ":${port}:(,|$)") {
    Write-Log "Abriendo el puerto $port en todas las interfaces de la red local"
    & $appcmd set site /site.name:"$siteName" "/+bindings.[protocol='http',bindingInformation='*:${port}:']"
}

& $appcmd set app "/app.name:${siteName}/" /applicationPool:"$poolName"

Write-Log "Regla de firewall para el puerto $port en la red local"
$regla = "NewRich API pruebas $port"
$existente = Get-NetFirewallRule -DisplayName $regla -ErrorAction SilentlyContinue
if ($existente) {
    Set-NetFirewallRule -DisplayName $regla -Enabled True -Profile Any -Action Allow -Direction Inbound
}
else {
    New-NetFirewallRule -DisplayName $regla -Direction Inbound -Protocol TCP -LocalPort $port -Action Allow -Profile Any | Out-Null
}

Write-Log "Nombre $hostName en el archivo hosts de este equipo"
$contenidoHosts = [System.IO.File]::ReadAllText($hostsPath)
if ($contenidoHosts -notmatch [regex]::Escape($hostName)) {
    [System.IO.File]::AppendAllText($hostsPath, "`r`n127.0.0.1 $hostName`r`n")
}
& ipconfig.exe /flushdns | Out-Null

Write-Log "Arrancando pool y sitio"
& $appcmd start apppool /apppool.name:"$poolName"
& $appcmd start site /site.name:"$siteName"

Write-Log "Sitios:"
& $appcmd list site | ForEach-Object { Write-Log $_ }

$saludOk = $false
for ($intento = 1; $intento -le 8; $intento++) {
    Start-Sleep -Seconds 2
    try {
        $respuesta = Invoke-WebRequest -Uri "http://${hostName}:${port}/api/salud" -UseBasicParsing -TimeoutSec 20
        Write-Log "Salud HTTP $([int]$respuesta.StatusCode) en el intento $intento"
        $saludOk = $true
        break
    }
    catch {
        $codigo = $null
        if ($_.Exception.Response) { $codigo = [int]$_.Exception.Response.StatusCode }
        Write-Log "Intento $intento sin salud. Codigo: $codigo. $($_.Exception.Message)"
    }
}

if (-not $saludOk) {
    $stdout = Get-ChildItem (Join-Path $dest "logs") -Filter "stdout*" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if ($stdout) {
        Write-Log "Ultimo log de arranque: $($stdout.FullName)"
        Get-Content $stdout.FullName -Tail 40 | ForEach-Object { Write-Log $_ }
    }
    exit 1
}

Write-Log "OK"
exit 0
