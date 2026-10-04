# Publica el API y el administrador de la rama ProduccionTest en el IIS de este equipo.
#   API:            C:\inetpub\ApiProduccionLocal  (http://localhost:9091/, base local NewRich)
#   Administrador:  C:\inetpub\RichProduccion      (Api:BaseUrl http://localhost:9091/)
# Conserva appsettings.json y web.config de cada sitio y no borra archivos del destino.
# Antes de correrlo: dotnet publish de ambos proyectos a bin\publish-produccion-local.
# Requiere PowerShell elevado.

$ErrorActionPreference = "Stop"

$raiz = "D:\Desarrollo\chances\SWApuestas"
$log = Join-Path $raiz "documentos\Despliegue\publicar-produccion-local-iis.log"
$aapt = "D:\Android\Sdk\build-tools\36.0.0\aapt.exe"

$sitios = @(
    @{ Nombre = "API"; Origen = Join-Path $raiz "Proyectos\API\src\NewRich.Api\bin\publish-produccion-local"; Destino = "C:\inetpub\ApiProduccionLocal"; Salud = "http://localhost:9091/api/salud" },
    @{ Nombre = "Administrador"; Origen = Join-Path $raiz "Proyectos\Administrador\bin\publish-produccion-local"; Destino = "C:\inetpub\RichProduccion"; Salud = "http://localhost:9090/" }
)

function Write-Log([string]$msg) {
    $line = "{0} {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $msg
    Add-Content -Path $log -Value $line
    Write-Host $line
}

Set-Content -Path $log -Value "=== Publicacion ProduccionTest en IIS local ==="

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Log "ERROR: se necesita PowerShell elevado."
    exit 1
}

foreach ($s in $sitios) {
    if (-not (Test-Path (Join-Path $s.Origen "web.config"))) {
        Write-Log "ERROR: falta la publicacion de $($s.Nombre) en $($s.Origen)"
        exit 1
    }
}

$fallo = $false
foreach ($s in $sitios) {
    $offline = Join-Path $s.Destino "app_offline.htm"
    Write-Log "$($s.Nombre): deteniendo con app_offline.htm"
    Set-Content -Path $offline -Value "<html><body>Actualizando, intente en un momento.</body></html>"
    Start-Sleep -Seconds 5

    Write-Log "$($s.Nombre): copiando $($s.Origen) -> $($s.Destino)"
    & robocopy.exe $s.Origen $s.Destino /E /R:5 /W:3 /NFL /NDL /NP /NJH `
        /XF appsettings.json web.config app_offline.htm appsettings.json.bak-azure `
        /XD chat-files logs apks | ForEach-Object { if ($_.Trim()) { Write-Log "  $($_.Trim())" } }
    $codigo = $LASTEXITCODE
    Remove-Item $offline -Force -ErrorAction SilentlyContinue
    if ($codigo -ge 8) {
        Write-Log "ERROR: robocopy fallo en $($s.Nombre) con codigo $codigo"
        $fallo = $true
    }
}

$apk = "C:\inetpub\RichProduccion\wwwroot\android\com.newrich.pda-Signed.apk"
if (Test-Path $aapt) {
    Write-Log ("APK del administrador: " + (& $aapt dump badging $apk 2>$null | Select-Object -First 1))
}

foreach ($s in $sitios) {
    $ok = $false
    for ($intento = 1; $intento -le 10; $intento++) {
        Start-Sleep -Seconds 3
        try {
            $r = Invoke-WebRequest -Uri $s.Salud -UseBasicParsing -TimeoutSec 60 -MaximumRedirection 5
            Write-Log "$($s.Nombre): HTTP $([int]$r.StatusCode) en $($s.Salud) (intento $intento)"
            $ok = $true
            break
        }
        catch {
            $c = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { "sin respuesta" }
            Write-Log "$($s.Nombre): intento $intento sin respuesta valida ($c)"
        }
    }
    if (-not $ok) { $fallo = $true }
}

if ($fallo) {
    Write-Log "TERMINO CON ERRORES"
    exit 1
}

Write-Log "OK"
exit 0
