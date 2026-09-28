$ErrorActionPreference = "Stop"
$staging = "D:\Proyectos\NewRich\apuestasMarcelaAndres\Proyectos\Administrador\bin\publish-iis"
$dest = "C:\inetpub\rich"
$appcmd = "C:\Windows\System32\inetsrv\appcmd.exe"
$log = "D:\Proyectos\NewRich\apuestasMarcelaAndres\documentos\Despliegue\publicar-admin-iis-local.log"

function Write-Log([string]$msg) {
    $line = "{0} {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $msg
    Add-Content -Path $log -Value $line
    Write-Host $line
}

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Log "ERROR: se necesita PowerShell elevado."
    exit 1
}

if (-not (Test-Path (Join-Path $staging "NewRich.Admin.dll"))) {
    Write-Log "ERROR: no hay publicacion en $staging"
    exit 1
}

Write-Log "Deteniendo pool y sitio para copiar"
& $appcmd stop apppool /apppool.name:"rich" | Out-Null
& $appcmd stop site /site.name:"rich" | Out-Null
Start-Sleep -Seconds 2

Write-Log "Copiando con robocopy (sin borrar el destino de golpe)"
$rc = Start-Process -FilePath "robocopy.exe" -ArgumentList @(
    $staging, $dest, "/E", "/IS", "/IT", "/R:2", "/W:1", "/NFL", "/NDL", "/NJH"
) -Wait -PassThru
Write-Log ("robocopy exit " + $rc.ExitCode)
if ($rc.ExitCode -ge 8) {
    Write-Log "ERROR: robocopy no pudo copiar"
    exit $rc.ExitCode
}

Write-Log "Arrancando pool y sitio"
& $appcmd start apppool /apppool.name:"rich"
& $appcmd start site /site.name:"rich"
$dll = Get-Item (Join-Path $dest "NewRich.Admin.dll")
Write-Log ("IIS NewRich.Admin.dll " + $dll.Length + " " + $dll.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"))
Write-Log "OK"
exit 0
