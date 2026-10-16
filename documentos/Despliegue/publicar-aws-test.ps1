<#
Publica el ambiente ProducciónTest en AWS (rama ProduccionAWSTest).

- API: Elastic Beanstalk newrich-test-api (.NET 10, Amazon Linux 2023, una instancia).
- Base: RDS SQL Server Express newrich-test-sql, base NewRich.
- Archivos (APK, chat y premios): bucket S3 newrich-test-archivos-129911414654.
- Administrador: sitio IIS richTestWeb (puerto 3000), carpeta C:\inetpub\RichProduccionWebTest,
  apuntando al API de AWS.

Mientras AWS no verifique la cuenta para CloudFront, el API solo responde por HTTP en la dirección
de Beanstalk. Cuando exista la distribución, cambiar $UrlApiTest por la dirección https de CloudFront.

La contraseña de SQL no se guarda en el repositorio: se lee de %USERPROFILE%\.aws\newrich-test-sql.txt
y se escribe solo en las variables del ambiente de Beanstalk.

Requisitos: AWS CLI v2 con sesión iniciada (aws login --profile newrich).

Uso:
  .\publicar-aws-test.ps1 [-Parte Api|Admin|Todo] [-Perfil newrich]
La parte Admin abre una consola elevada para escribir en IIS.
#>
param(
    [ValidateSet('Api', 'Admin', 'Todo')][string]$Parte = 'Todo',
    [string]$Perfil = 'newrich'
)

$ErrorActionPreference = 'Stop'
$env:AWS_PROFILE = $Perfil
$env:AWS_REGION = 'us-east-2'
$raiz = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$salida = Join-Path $raiz 'tmp\publicar-aws-test'

$UrlApiTest = 'http://newrich-test-api.us-east-2.elasticbeanstalk.com/'
$Aplicacion = 'newrich-api'
$Ambiente = 'newrich-test-api'
$Bucket = 'newrich-test-archivos-129911414654'
$ServidorSql = 'newrich-test-sql.cdy4k0kg8s4b.us-east-2.rds.amazonaws.com'
$BaseSql = 'NewRich'
$GrupoSeguridad = 'sg-0f9ae4e7aab617c77'
$Vpc = 'vpc-0050c9f742b2520d3'
$Subredes = 'subnet-06afdd2d6e47dc5df,subnet-0903ea6e2658d42f0,subnet-00896dcff922a8d07'
$PerfilInstancia = 'newrich-test-api-ec2'
$RolServicio = 'aws-elasticbeanstalk-service-role'
$SitioIis = 'richTestWeb'
$CarpetaIis = 'C:\inetpub\RichProduccionWebTest'
$PuertoIis = 3000
$CarpetaClaves = 'C:\inetpub\rich-keys-test'

function AwsJson {
    $r = & aws @args --output json
    if ($LASTEXITCODE -ne 0) { throw "Falló: aws $($args -join ' ')" }
    if ($r) { $r | ConvertFrom-Json }
}

function Escribir-Json($ruta, $objeto) {
    $objeto | ConvertTo-Json -Depth 20 | Set-Content -Path $ruta -Encoding UTF8
}

function Ajustes-Ambiente {
    $secreto = Get-Content (Join-Path $env:USERPROFILE '.aws\newrich-test-sql.txt') | ConvertFrom-StringData
    $conexion = "Server=tcp:$ServidorSql,1433;Database=$BaseSql;User ID=$($secreto.usuario);Password=$($secreto.password);Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;"
    $variables = [ordered]@{
        ASPNETCORE_ENVIRONMENT             = 'Production'
        ConnectionStrings__NewRichDatabase = $conexion
        AwsS3__Bucket                      = $Bucket
        AwsS3__Region                      = 'us-east-2'
    }
    $ajustes = @(
        @{ Namespace = 'aws:elasticbeanstalk:environment'; OptionName = 'EnvironmentType'; Value = 'SingleInstance' }
        @{ Namespace = 'aws:elasticbeanstalk:environment'; OptionName = 'ServiceRole'; Value = $RolServicio }
        @{ Namespace = 'aws:autoscaling:launchconfiguration'; OptionName = 'IamInstanceProfile'; Value = $PerfilInstancia }
        @{ Namespace = 'aws:autoscaling:launchconfiguration'; OptionName = 'SecurityGroups'; Value = $GrupoSeguridad }
        @{ Namespace = 'aws:autoscaling:launchconfiguration'; OptionName = 'DisableIMDSv1'; Value = 'true' }
        @{ Namespace = 'aws:autoscaling:launchconfiguration'; OptionName = 'RootVolumeType'; Value = 'gp3' }
        @{ Namespace = 'aws:ec2:instances'; OptionName = 'InstanceTypes'; Value = 't3.micro' }
        @{ Namespace = 'aws:ec2:vpc'; OptionName = 'VPCId'; Value = $Vpc }
        @{ Namespace = 'aws:ec2:vpc'; OptionName = 'Subnets'; Value = $Subredes }
        @{ Namespace = 'aws:ec2:vpc'; OptionName = 'AssociatePublicIpAddress'; Value = 'true' }
        @{ Namespace = 'aws:elasticbeanstalk:healthreporting:system'; OptionName = 'SystemType'; Value = 'enhanced' }
    )
    foreach ($v in $variables.GetEnumerator()) {
        $ajustes += @{ Namespace = 'aws:elasticbeanstalk:application:environment'; OptionName = $v.Key; Value = $v.Value }
    }
    $archivo = Join-Path $salida 'ajustes.json'
    ConvertTo-Json @($ajustes) -Depth 5 | Set-Content $archivo -Encoding ascii
    "file://$archivo"
}

function Armar-Paquete {
    $destino = Join-Path $salida 'api'
    if (Test-Path $destino) { Remove-Item $destino -Recurse -Force }

    dotnet publish (Join-Path $raiz 'Proyectos\API\src\NewRich.Api\NewRich.Api.csproj') -c Release -o $destino -nologo -v q | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'No compiló el API.' }

    $ajustes = Join-Path $destino 'appsettings.json'
    $json = Get-Content $ajustes -Raw | ConvertFrom-Json
    $json.Cors.Origenes = @($json.Cors.Origenes) + @("http://localhost:$PuertoIis", "http://127.0.0.1:$PuertoIis") | Select-Object -Unique
    Escribir-Json $ajustes $json
    Remove-Item (Join-Path $destino 'appsettings.Development.json') -ErrorAction SilentlyContinue

    Copy-Item (Join-Path $PSScriptRoot 'aws\.platform') $destino -Recurse
    [IO.File]::WriteAllText((Join-Path $destino 'Procfile'), "web: dotnet NewRich.Api.dll`n")

    $zip = Join-Path $salida "api-$(Get-Date -Format yyyyMMddHHmmss).zip"
    # Compress-Archive de Windows PowerShell guarda rutas con '\', que Linux no reconoce.
    Push-Location $destino
    $elementos = Get-ChildItem -Name -Force
    tar -a -c -f $zip @elementos | Out-Host
    Pop-Location
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo armar el zip del API.' }
    $zip
}

function Publicar-Api {
    $zip = Armar-Paquete
    $etiqueta = [IO.Path]::GetFileNameWithoutExtension($zip)

    $almacen = (AwsJson elasticbeanstalk create-storage-location).S3Bucket
    Write-Host "Subiendo $etiqueta a s3://$almacen ..."
    aws s3 cp $zip "s3://$almacen/$Aplicacion/$etiqueta.zip" --only-show-errors
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo subir el paquete.' }

    $apps = AwsJson elasticbeanstalk describe-applications --application-names $Aplicacion
    if (-not $apps.Applications) { AwsJson elasticbeanstalk create-application --application-name $Aplicacion | Out-Null }
    AwsJson elasticbeanstalk create-application-version --application-name $Aplicacion --version-label $etiqueta `
        --source-bundle "S3Bucket=$almacen,S3Key=$Aplicacion/$etiqueta.zip" --process | Out-Null

    $ajustes = Ajustes-Ambiente
    try {
        $existente = (AwsJson elasticbeanstalk describe-environments --application-name $Aplicacion --environment-names $Ambiente --no-include-deleted).Environments |
            Where-Object Status -ne 'Terminated'
        if ($existente) {
            Write-Host "Actualizando el ambiente $Ambiente ..."
            AwsJson elasticbeanstalk update-environment --environment-name $Ambiente --version-label $etiqueta --option-settings $ajustes | Out-Null
        } else {
            $plataforma = (AwsJson elasticbeanstalk list-available-solution-stacks).SolutionStacks |
                Where-Object { $_ -like '64bit Amazon Linux 2023 * running .NET 10' } | Select-Object -First 1
            Write-Host "Creando el ambiente $Ambiente sobre '$plataforma' (tarda unos minutos) ..."
            AwsJson elasticbeanstalk create-environment --application-name $Aplicacion --environment-name $Ambiente `
                --cname-prefix $Ambiente --solution-stack-name $plataforma --version-label $etiqueta `
                --option-settings $ajustes --tags 'Key=Proyecto,Value=NewRich' 'Key=Ambiente,Value=Test' | Out-Null
        }
    } finally {
        Remove-Item (Join-Path $salida 'ajustes.json') -Force -ErrorAction SilentlyContinue
    }

    $limite = (Get-Date).AddMinutes(25)
    do {
        Start-Sleep -Seconds 15
        $estado = (AwsJson elasticbeanstalk describe-environments --environment-names $Ambiente).Environments[0]
        Write-Host "  $($estado.Status) / $($estado.Health) / versión $($estado.VersionLabel)"
    } while ($estado.Status -ne 'Ready' -and (Get-Date) -lt $limite)
    if ($estado.Status -ne 'Ready') { throw 'El ambiente no quedó listo a tiempo.' }
    if ($estado.VersionLabel -ne $etiqueta) { throw "El ambiente quedó con la versión $($estado.VersionLabel), no con $etiqueta. Revise los eventos en la consola." }

    $salud = Invoke-WebRequest -Uri ($UrlApiTest + 'api/salud') -UseBasicParsing -TimeoutSec 120
    Write-Host "API de test en AWS responde HTTP $($salud.StatusCode) en $($UrlApiTest)api/salud"
}

function Publicar-Admin {
    $destino = Join-Path $salida 'admin'
    if (Test-Path $destino) { Remove-Item $destino -Recurse -Force }

    dotnet publish (Join-Path $raiz 'Proyectos\Administrador\NewRich.Admin.csproj') -c Release -o $destino -nologo -v q | Out-Host
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
    Write-Host "Administrador de test responde HTTP $($pagina.StatusCode) en http://localhost:$PuertoIis/ (API: $UrlApiTest)"
}

New-Item -ItemType Directory -Force $salida | Out-Null
if ($Parte -in 'Api', 'Todo') { Publicar-Api }
if ($Parte -in 'Admin', 'Todo') { Publicar-Admin }
