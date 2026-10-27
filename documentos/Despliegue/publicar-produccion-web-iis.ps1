<#
Publica en el IIS de este equipo el administrador y el sitio del vendedor de producción, apuntando al API de Azure.

- Administrador: sitio IIS richProduccionWeb (puerto 5000), carpeta C:\inetpub\RichProduccionWeb.
- Vendedor:      sitio IIS richVendedorWeb (puerto 5001),    carpeta C:\inetpub\richVendedorWeb.
- API:           https://newrich-cxhea7a9g6d2c3ce.centralus-01.azurewebsites.net/ (se publica con publicar-produccion-azure.ps1).

Los datos de los recursos están en documentos\azure\azure-produccion.md (fuera del repositorio).

Uso:
  .\publicar-produccion-web-iis.ps1 [-Parte Admin|Vendedor|Todo]
La copia a IIS abre una consola elevada.
#>
param(
    [ValidateSet('Admin', 'Vendedor', 'Todo')][string]$Parte = 'Todo'
)

$ErrorActionPreference = 'Stop'
$raiz = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$salida = Join-Path $raiz 'tmp\publicar-produccion-web'
$UrlApi = 'https://newrich-cxhea7a9g6d2c3ce.centralus-01.azurewebsites.net/'

$sitios = @{
    Admin = @{
        Proyecto = 'Proyectos\Administrador\NewRich.Admin.csproj'
        Sitio = 'richProduccionWeb'
        Carpeta = 'C:\inetpub\RichProduccionWeb'
        Puerto = 5000
        Claves = 'C:\inetpub\rich-keys-produccion-web'
    }
    Vendedor = @{
        Proyecto = 'Proyectos\Vendedor\NewRich.Vendedor.csproj'
        Sitio = 'richVendedorWeb'
        Carpeta = 'C:\inetpub\richVendedorWeb'
        Puerto = 5001
        Claves = $null
    }
}

function Preparar($nombre) {
    $s = $sitios[$nombre]
    $destino = Join-Path $salida $nombre
    if (Test-Path $destino) { Remove-Item $destino -Recurse -Force }

    dotnet publish (Join-Path $raiz $s.Proyecto) -c Release -o $destino -nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "No compiló $nombre." }

    $ajustes = Join-Path $destino 'appsettings.json'
    $json = Get-Content $ajustes -Raw | ConvertFrom-Json
    $json.Api.BaseUrl = $UrlApi
    if ($s.Claves) {
        $json | Add-Member -NotePropertyName DataProtection -NotePropertyValue ([pscustomobject]@{ KeysPath = $s.Claves }) -Force
    }
    $json | ConvertTo-Json -Depth 20 | Set-Content -Path $ajustes -Encoding UTF8
    Remove-Item (Join-Path $destino 'appsettings.Development.json') -ErrorAction SilentlyContinue
    return $destino
}

$partes = if ($Parte -eq 'Todo') { @('Admin', 'Vendedor') } else { @($Parte) }
New-Item -ItemType Directory -Force $salida | Out-Null
$origenes = @{}
foreach ($p in $partes) { $origenes[$p] = Preparar $p }

$elevado = Join-Path $salida 'copiar-iis.ps1'
$bitacora = Join-Path $salida 'copiar-iis.log'
$lineas = @(
    '$ErrorActionPreference = ''Stop'''
    "Start-Transcript -Path '$bitacora' -Force | Out-Null"
    'try {'
    '    $appcmd = Join-Path $env:windir ''system32\inetsrv\appcmd.exe'''
)
foreach ($p in $partes) {
    $s = $sitios[$p]
    $lineas += @(
        "    & `$appcmd stop site /site.name:$($s.Sitio) | Out-Null"
        "    & `$appcmd stop apppool /apppool.name:$($s.Sitio) | Out-Null"
        "    & `$appcmd set apppool /apppool.name:$($s.Sitio) /managedRuntimeVersion: | Out-Null"
        '    Start-Sleep -Seconds 3'
        "    robocopy '$($origenes[$p])' '$($s.Carpeta)' /MIR /R:3 /W:2 /NFL /NDL /NJH /NP | Out-Null"
        "    if (`$LASTEXITCODE -ge 8) { throw 'robocopy falló con código ' + `$LASTEXITCODE + ' en $($s.Sitio)' }"
    )
    if ($s.Claves) {
        $lineas += @(
            "    New-Item -ItemType Directory -Force '$($s.Claves)' | Out-Null"
            "    icacls '$($s.Claves)' /grant 'IIS APPPOOL\$($s.Sitio):(OI)(CI)M' | Out-Null"
        )
    }
    $lineas += @(
        "    & `$appcmd start apppool /apppool.name:$($s.Sitio) | Out-Null"
        "    & `$appcmd start site /site.name:$($s.Sitio) | Out-Null"
    )
}
$lineas += @('    ''OK''', '} finally { Stop-Transcript | Out-Null }')
$lineas | Set-Content -Path $elevado -Encoding UTF8

Write-Host 'Copiando a IIS (consola elevada)...'
Start-Process powershell -Verb RunAs -Wait -WindowStyle Hidden -ArgumentList '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $elevado
if (-not (Select-String -Path $bitacora -Pattern '^OK$' -Quiet)) { throw "La copia a IIS no terminó. Revise $bitacora" }

foreach ($p in $partes) {
    $s = $sitios[$p]
    $pagina = Invoke-WebRequest -Uri "http://localhost:$($s.Puerto)/Cuenta/Ingresar" -UseBasicParsing -TimeoutSec 120
    Write-Host "$p responde HTTP $($pagina.StatusCode) en http://localhost:$($s.Puerto)/"
}
