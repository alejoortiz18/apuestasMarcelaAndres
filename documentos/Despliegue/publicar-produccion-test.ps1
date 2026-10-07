<#
Publica el ambiente ProducciónTest.

- API: App Service apirichtest (Azure), contra la base testRich de sql-api-ventas-prod.
- Administrador: sitio IIS richTestWeb (puerto 3000), carpeta C:\inetpub\RichProduccionWebTest,
  apuntando al API de test.

La contraseña de SQL y las credenciales del perfil de publicación no se guardan en el repositorio:
se escriben solo en la copia publicada.

Uso:
  .\publicar-produccion-test.ps1 -PasswordSql '...' [-Parte Api|Admin|Todo]
La parte Admin abre una consola elevada para escribir en IIS.
#>
param(
    [Parameter(Mandatory)][string]$PasswordSql,
    [ValidateSet('Api', 'Admin', 'Todo')][string]$Parte = 'Todo',
    [string]$PerfilPublicacion = 'F:\apirichtest.PublishSettings'
)

$ErrorActionPreference = 'Stop'
$raiz = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$salida = Join-Path $raiz 'tmp\publicar-test'

$UrlApiTest = 'https://apirichtest-gkgkguhyc6hdbtdb.westus3-01.azurewebsites.net/'
$ServidorSql = 'sql-api-ventas-prod.database.windows.net'
$BaseSql = 'testRich'
$UsuarioSql = 'sqladmin'
$SitioIis = 'richTestWeb'
$CarpetaIis = 'C:\inetpub\RichProduccionWebTest'
$PuertoIis = 3000
$CarpetaClaves = 'C:\inetpub\rich-keys-test'

function Escribir-Json($ruta, $objeto) {
    $objeto | ConvertTo-Json -Depth 20 | Set-Content -Path $ruta -Encoding UTF8
}

function Publicar-Api {
    $destino = Join-Path $salida 'api'
    if (Test-Path $destino) { Remove-Item $destino -Recurse -Force }

    # El App Service es Linux con .NET 10: se publica dependiente del runtime del contenedor.
    dotnet publish (Join-Path $raiz 'Proyectos\API\src\NewRich.Api\NewRich.Api.csproj') `
        -c Release -o $destino -nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'No compiló el API.' }

    $ajustes = Join-Path $destino 'appsettings.json'
    $json = Get-Content $ajustes -Raw | ConvertFrom-Json
    $json.ConnectionStrings.NewRichDatabase = "Server=tcp:$ServidorSql,1433;Initial Catalog=$BaseSql;User ID=$UsuarioSql;Password=$PasswordSql;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
    $json.Cors.Origenes = @($json.Cors.Origenes) + @("http://localhost:$PuertoIis", "http://127.0.0.1:$PuertoIis") | Select-Object -Unique
    Escribir-Json $ajustes $json
    Remove-Item (Join-Path $destino 'appsettings.Development.json') -ErrorAction SilentlyContinue

    [xml]$perfil = Get-Content $PerfilPublicacion
    $zipPerfil = $perfil.publishData.publishProfile | Where-Object publishMethod -eq 'ZipDeploy'
    $scm = $zipPerfil.publishUrl.Split(':')[0]
    $auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($zipPerfil.userName):$($zipPerfil.userPWD)"))
    $cabeceras = @{ Authorization = "Basic $auth" }

    $zip = Join-Path $salida 'api.zip'
    if (Test-Path $zip) { Remove-Item $zip -Force }
    # Compress-Archive de Windows PowerShell guarda rutas con '\', que el contenedor Linux no reconoce.
    Push-Location $destino
    $elementos = Get-ChildItem -Name
    tar -a -c -f $zip @elementos
    Pop-Location
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo armar el zip del API.' }

    Write-Host 'Subiendo API al App Service...'
    Invoke-WebRequest -Method Post -Uri "https://$scm/api/publish?type=zip&clean=true" -Headers $cabeceras `
        -InFile $zip -ContentType 'application/octet-stream' -UseBasicParsing -TimeoutSec 1800 | Out-Null
    do {
        Start-Sleep -Seconds 5
        $d = Invoke-RestMethod -Uri "https://$scm/api/deployments/latest" -Headers $cabeceras
    } while (-not $d.complete)
    if ($d.status -ne 4) { throw "El despliegue del API falló (estado $($d.status)): $($d.status_text)" }

    $limite = (Get-Date).AddMinutes(5)
    while ($true) {
        try {
            $salud = Invoke-WebRequest -Uri ($UrlApiTest + 'api/salud') -UseBasicParsing -TimeoutSec 120
            break
        } catch {
            if ((Get-Date) -gt $limite) { throw "El API de test no respondió: $($_.Exception.Message)" }
            Start-Sleep -Seconds 10
        }
    }
    Write-Host "API de test responde HTTP $($salud.StatusCode) en $($UrlApiTest)api/salud"
}

function Publicar-Admin {
    $destino = Join-Path $salida 'admin'
    if (Test-Path $destino) { Remove-Item $destino -Recurse -Force }

    dotnet publish (Join-Path $raiz 'Proyectos\Administrador\NewRich.Admin.csproj') -c Release -o $destino -nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'No compiló el Administrador.' }

    $ajustes = Join-Path $destino 'appsettings.json'
    $json = Get-Content $ajustes -Raw | ConvertFrom-Json
    $json.Api.BaseUrl = $UrlApiTest
    $json | Add-Member -NotePropertyName DataProtection -NotePropertyValue ([pscustomobject]@{ KeysPath = $CarpetaClaves }) -Force
    Escribir-Json $ajustes $json
    Remove-Item (Join-Path $destino 'appsettings.Development.json') -ErrorAction SilentlyContinue

    $elevado = Join-Path $salida 'copiar-admin-iis.ps1'
    $bitacora = Join-Path $salida 'copiar-admin-iis.log'
    @"
`$ErrorActionPreference = 'Stop'
Start-Transcript -Path '$bitacora' -Force | Out-Null
try {
    `$appcmd = Join-Path `$env:windir 'system32\inetsrv\appcmd.exe'
    & `$appcmd stop site /site.name:$SitioIis | Out-Null
    & `$appcmd stop apppool /apppool.name:$SitioIis | Out-Null
    & `$appcmd set apppool /apppool.name:$SitioIis /managedRuntimeVersion: | Out-Null
    Start-Sleep -Seconds 3
    robocopy '$destino' '$CarpetaIis' /MIR /R:3 /W:2 /NFL /NDL /NJH /NP | Out-Null
    if (`$LASTEXITCODE -ge 8) { throw "robocopy falló con código `$LASTEXITCODE" }
    New-Item -ItemType Directory -Force '$CarpetaClaves' | Out-Null
    icacls '$CarpetaClaves' /grant 'IIS APPPOOL\${SitioIis}:(OI)(CI)M' | Out-Null
    & `$appcmd start apppool /apppool.name:$SitioIis | Out-Null
    & `$appcmd start site /site.name:$SitioIis | Out-Null
    'OK'
} finally { Stop-Transcript | Out-Null }
"@ | Set-Content -Path $elevado -Encoding UTF8

    Write-Host 'Copiando Administrador a IIS (consola elevada)...'
    Start-Process powershell -Verb RunAs -Wait -WindowStyle Hidden -ArgumentList '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $elevado
    if (-not (Select-String -Path $bitacora -Pattern '^OK$' -Quiet)) { throw "La copia a IIS no terminó. Revise $bitacora" }

    $pagina = Invoke-WebRequest -Uri "http://localhost:$PuertoIis/Cuenta/Ingresar" -UseBasicParsing -TimeoutSec 120
    Write-Host "Administrador de test responde HTTP $($pagina.StatusCode) en http://localhost:$PuertoIis/"
}

New-Item -ItemType Directory -Force $salida | Out-Null
if ($Parte -in 'Api', 'Todo') { Publicar-Api }
if ($Parte -in 'Admin', 'Todo') { Publicar-Admin }
