<#
Publica el API de producción en Azure.

- API: App Service newrich (Linux con .NET 10), contra la base dbnewrich de srvdb-newrich.
- APK: contenedor produccion de la cuenta stnewrich.

Los datos de los recursos están en documentos\azure\azure-produccion.md (fuera del repositorio).
La contraseña de SQL y la conexión del blob no se guardan en el repositorio: se escriben solo en la copia publicada.

Uso:
  .\publicar-produccion-azure.ps1 -PasswordSql '...' -ConexionBlob 'BlobEndpoint=https://stnewrich.blob.core.windows.net/;SharedAccessSignature=...'
#>
param(
    [Parameter(Mandatory)][string]$PasswordSql,
    [Parameter(Mandatory)][string]$ConexionBlob,
    [string]$PerfilPublicacion = (Join-Path $PSScriptRoot '..\azure\newrich.PublishSettings')
)

$ErrorActionPreference = 'Stop'
$raiz = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$salida = Join-Path $raiz 'tmp\publicar-produccion-azure'

$UrlApi = 'https://newrich-cxhea7a9g6d2c3ce.centralus-01.azurewebsites.net/'
$ServidorSql = 'srvdb-newrich.database.windows.net'
$BaseSql = 'dbnewrich'
$UsuarioSql = 'newrichsql'
$ContenedorBlob = 'produccion'

$destino = Join-Path $salida 'api'
if (Test-Path $destino) { Remove-Item $destino -Recurse -Force }
New-Item -ItemType Directory -Force $salida | Out-Null

# El App Service es Linux con .NET 10: se publica dependiente del runtime del contenedor.
dotnet publish (Join-Path $raiz 'Proyectos\API\src\NewRich.Api\NewRich.Api.csproj') `
    -c Release -o $destino -nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'No compiló el API.' }

$ajustes = Join-Path $destino 'appsettings.json'
$json = Get-Content $ajustes -Raw | ConvertFrom-Json
$json.ConnectionStrings.NewRichDatabase = "Server=tcp:$ServidorSql,1433;Initial Catalog=$BaseSql;Persist Security Info=False;User ID=$UsuarioSql;Password=$PasswordSql;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
# Los navegadores del administrador (5000) y del vendedor (5001) se conectan a los hubs de SignalR del API.
# CorsOrigenes acepta ese puerto también desde equipos de la red local.
$json.Cors.Origenes = @($json.Cors.Origenes) + @('http://localhost:5000', 'http://localhost:5001') | Select-Object -Unique
$json | Add-Member -NotePropertyName AzureBlobConnection -NotePropertyValue $ConexionBlob -Force
$json | Add-Member -NotePropertyName AzureBlobContainerName -NotePropertyValue $ContenedorBlob -Force
$json | ConvertTo-Json -Depth 20 | Set-Content -Path $ajustes -Encoding UTF8
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
        $salud = Invoke-WebRequest -Uri ($UrlApi + 'api/salud') -UseBasicParsing -TimeoutSec 120
        break
    } catch {
        if ((Get-Date) -gt $limite) { throw "El API no respondió: $($_.Exception.Message)" }
        Start-Sleep -Seconds 10
    }
}
Write-Host "API de producción responde HTTP $($salud.StatusCode) en $($UrlApi)api/salud"
