$ErrorActionPreference = "Stop"

$dotnet = "C:\Program Files\dotnet\dotnet.exe"
$csproj = "D:\Proyectos\NewRich\apuestasMarcelaAndres\Proyectos\Administrador\NewRich.Admin.csproj"
$staging = "D:\Proyectos\NewRich\apuestasMarcelaAndres\Proyectos\Administrador\bin\publish-iis"
$dest = "C:\inetpub\rich"
$siteName = "rich"
$poolName = "rich"
$port = 8080
$appcmd = "C:\Windows\System32\inetsrv\appcmd.exe"
$log = "D:\Proyectos\NewRich\apuestasMarcelaAndres\documentos\Despliegue\publicar-admin-iis-local.log"

function Write-Log([string]$msg) {
    $line = "{0} {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $msg
    Add-Content -Path $log -Value $line
    Write-Host $line
}

Set-Content -Path $log -Value "=== Publicacion IIS local ==="

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Log "ERROR: se necesita PowerShell elevado."
    exit 1
}

Write-Log "Publicando Release a $staging"
& $dotnet publish $csproj -c Release -o $staging
if ($LASTEXITCODE -ne 0) {
    Write-Log "ERROR: dotnet publish fallo con codigo $LASTEXITCODE"
    exit $LASTEXITCODE
}

if (-not (Test-Path (Join-Path $staging "web.config"))) {
    Write-Log "ERROR: no se genero web.config en la publicacion"
    exit 1
}

Write-Log "Asegurando carpeta destino $dest"
New-Item -ItemType Directory -Path $dest -Force | Out-Null

$poolExists = $false
$siteExists = $false
if (Test-Path $appcmd) {
    $pools = & $appcmd list apppool
    $sites = & $appcmd list site
    $poolExists = ($pools | Select-String -SimpleMatch "APPPOOL `"$poolName`"" ) -ne $null
    $siteExists = ($sites | Select-String -SimpleMatch "SITE `"$siteName`"" ) -ne $null
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
Get-ChildItem -Path $dest -Force | Remove-Item -Recurse -Force
Copy-Item -Path (Join-Path $staging "*") -Destination $dest -Recurse -Force

if (-not (Test-Path $appcmd)) {
    Write-Log "WARN: appcmd no existe; archivos copiados pero el sitio no se configuro"
    exit 0
}

if (-not $poolExists) {
    Write-Log "Creando pool $poolName (Sin codigo administrado)"
    & $appcmd add apppool /name:"$poolName" /managedRuntimeVersion:"" /managedPipelineMode:Integrated
}
else {
    Write-Log "Ajustando pool $poolName a Sin codigo administrado"
    & $appcmd set apppool /apppool.name:"$poolName" /managedRuntimeVersion:""
}

& $appcmd set config /section:applicationPools /[name="$poolName"].processModel.idleTimeout:"00:20:00"

if (-not $siteExists) {
    Write-Log "Creando sitio $siteName en puerto $port"
    & $appcmd add site /name:"$siteName" /physicalPath:"$dest" /bindings:"http/*:${port}:"
    & $appcmd set app /app.name:"${siteName}/" /applicationPool:"$poolName"
}
else {
    Write-Log "Actualizando ruta fisica del sitio $siteName"
    & $appcmd set site /site.name:"$siteName" /physicalPath:"$dest"
    & $appcmd set app /app.name:"${siteName}/" /applicationPool:"$poolName"
}

Write-Log "Arrancando pool y sitio"
& $appcmd start apppool /apppool.name:"$poolName"
& $appcmd start site /site.name:"$siteName"

Write-Log "Sitios:"
& $appcmd list site
Write-Log "Pools:"
& $appcmd list apppool
Write-Log "OK"
exit 0
