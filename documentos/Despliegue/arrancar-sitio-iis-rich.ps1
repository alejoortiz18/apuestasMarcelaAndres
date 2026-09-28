$ErrorActionPreference = "Stop"
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

Write-Log "Estado antes:"
& $appcmd list site /site.name:"rich"
& $appcmd list apppool /apppool.name:"rich"
Write-Log "Arrancando pool rich"
& $appcmd start apppool /apppool.name:"rich"
Write-Log "Arrancando sitio rich"
& $appcmd start site /site.name:"rich"
Write-Log "Estado despues:"
& $appcmd list site /site.name:"rich"
& $appcmd list apppool /apppool.name:"rich"
Write-Log "OK"
exit 0
