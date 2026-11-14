#requires -Version 7.0
param(
    [string]$ConfigurationFile = '',
    [int]$Port = 5191,
    [string]$Dotnet = ''
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
if (!$ConfigurationFile) { $ConfigurationFile = Join-Path $repo 'VigiaPlatform.API/appsettings.Development.json' }
if (!$Dotnet) {
    $userSdk = Join-Path $env:USERPROFILE '.dotnet/dotnet.exe'
    $Dotnet = if (Test-Path -LiteralPath $userSdk) { $userSdk } else { (Get-Command dotnet -ErrorAction Stop).Source }
}
$configuration = Get-Content -LiteralPath $ConfigurationFile -Raw | ConvertFrom-Json
$connection = [System.Data.Common.DbConnectionStringBuilder]::new()
# Use CLR accessors because PowerShell treats this builder as a dictionary.
$connection.set_ConnectionString($configuration.ConnectionStrings.DefaultConnection)
foreach ($key in @($connection.get_Keys())) {
    if ($key -match '^(database|initial catalog)$') { [void]$connection.Remove($key) }
}
$suffix = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss') + '_' + [Guid]::NewGuid().ToString('N').Substring(0, 6)
$database = "vigia_local_production_check_$suffix"
$connection['Database'] = $database
$verifiedConnection = [System.Data.Common.DbConnectionStringBuilder]::new()
$verifiedConnection.set_ConnectionString($connection.get_ConnectionString())
if ($verifiedConnection['Database'] -ne $database -or $verifiedConnection.ContainsKey('ConnectionString') -or
    $database -notmatch '^vigia_local_production_check_[a-zA-Z0-9_]+$') {
    throw 'The isolated validation database could not be verified. No database commands were run.'
}
$outputDirectory = Join-Path $repo ".local/production-check-$suffix"
$publishDirectory = Join-Path $outputDirectory 'publish'
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$password = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
$names = @('ASPNETCORE_ENVIRONMENT', 'ConnectionStrings__DefaultConnection', 'BootstrapAdmin__Username',
    'BootstrapAdmin__Password', 'BootstrapAdmin__FullName', 'DataProtection__KeysPath', 'OpenApi__Enabled',
    'Logging__LogLevel__Default')
$previous = @{}
foreach ($name in $names) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
$process = $null
$baseUrl = "http://127.0.0.1:$Port"
$checks = 0

function Send-Check([string]$Method, [string]$Path, [object]$Body = $null, [string]$Token = '', [int]$Expected = 200) {
    $headers = @{}
    if ($Token) { $headers.Authorization = "Bearer $Token" }
    $request = @{ Uri = "$baseUrl$Path"; Method = $Method; Headers = $headers; SkipHttpErrorCheck = $true; TimeoutSec = 10 }
    if ($null -ne $Body) { $request.Body = $Body | ConvertTo-Json -Depth 5; $request.ContentType = 'application/json' }
    $response = Invoke-WebRequest @request
    if ([int]$response.StatusCode -ne $Expected) { throw "$Method $Path expected $Expected, got $($response.StatusCode)." }
    $script:checks++
    if ($response.Content -and $response.Headers.'Content-Type' -match 'application/json') {
        return $response.Content | ConvertFrom-Json -NoEnumerate
    }
}

function Start-CheckedApi {
    $arguments = @(('"' + (Join-Path $publishDirectory 'VigiaPlatform.API.dll') + '"'), '--urls', $baseUrl)
    $started = Start-Process -FilePath $Dotnet -ArgumentList $arguments -WorkingDirectory $publishDirectory -PassThru -WindowStyle Hidden `
        -RedirectStandardOutput (Join-Path $outputDirectory 'api.log') -RedirectStandardError (Join-Path $outputDirectory 'api-error.log')
    try {
        for ($attempt = 0; $attempt -lt 40; $attempt++) {
            if ($started.HasExited) { throw "Production API exited. See $outputDirectory." }
            try {
                if ((Invoke-WebRequest "$baseUrl/health" -SkipHttpErrorCheck -TimeoutSec 2).StatusCode -eq 200) { return $started }
            } catch { }
            Start-Sleep -Milliseconds 250
        }
        throw 'Production API did not become healthy.'
    } catch {
        if (!$started.HasExited) { Stop-Process -Id $started.Id; $started.WaitForExit() }
        throw
    }
}

try {
    $env:ASPNETCORE_ENVIRONMENT = 'Production'
    $env:ConnectionStrings__DefaultConnection = $connection.get_ConnectionString()
    $env:BootstrapAdmin__Username = 'production.check'
    $env:BootstrapAdmin__Password = $password
    $env:BootstrapAdmin__FullName = 'Production Check'
    $env:DataProtection__KeysPath = Join-Path $outputDirectory 'keys'
    $env:OpenApi__Enabled = 'true'
    $env:Logging__LogLevel__Default = 'Warning'
    & $Dotnet publish (Join-Path $repo 'VigiaPlatform.API/VigiaPlatform.API.csproj') -c Release -o $publishDirectory /p:UseAppHost=false --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    if (Test-Path (Join-Path $publishDirectory 'appsettings.Development.json')) { throw 'Private development settings were published.' }
    $assembly = Join-Path $publishDirectory 'VigiaPlatform.API.dll'
    & $Dotnet $assembly --migrate *> (Join-Path $outputDirectory 'migration.log')
    if ($LASTEXITCODE -ne 0) { throw "Migration failed. See $outputDirectory." }
    & $Dotnet $assembly --migrate *> (Join-Path $outputDirectory 'migration-repeat.log')
    if ($LASTEXITCODE -ne 0) { throw 'Repeated migration failed on a database with the complete migration history.' }
    & $Dotnet $assembly --bootstrap-admin *> (Join-Path $outputDirectory 'bootstrap.log')
    if ($LASTEXITCODE -ne 0) { throw "Administrator provisioning failed. See $outputDirectory." }
    $env:BootstrapAdmin__Password = 'A-different-password-that-must-not-reset-the-account'
    & $Dotnet $assembly --bootstrap-admin *> (Join-Path $outputDirectory 'bootstrap-repeat.log')
    if ($LASTEXITCODE -ne 0) { throw 'Repeated provisioning failed.' }
    $process = Start-CheckedApi
    Send-Check GET '/health'
    Send-Check GET '/api/v1/projects' -Expected 401
    Send-Check GET '/openapi/v1.json' | Out-Null
    Send-Check GET '/scalar/v1'
    Send-Check POST '/api/v1/authentication/sign-in' -Body @{username = 'production.check'; password = 'wrong-password'} -Expected 401
    $session = Send-Check POST '/api/v1/authentication/sign-in' -Body @{username = 'production.check'; password = $password}
    $profile = Send-Check GET '/api/v1/users/me' -Token $session.accessToken
    if ($profile.role -ne 'ADMIN') { throw 'Provisioned account does not have the administrator role.' }
    $users = Send-Check GET '/api/v1/users' -Token $session.accessToken
    if ($users.Count -ne 1) { throw 'Unexpected accounts or demo seeding in Production.' }
    foreach ($path in @('/projects', '/materials', '/orders', '/dispatches', '/receptions')) {
        $items = Send-Check GET "/api/v1$path" -Token $session.accessToken
        if ($items.Count -ne 0) { throw "Unexpected demo records at $path." }
    }
    $cors = Invoke-WebRequest "$baseUrl/api/v1/projects" -Method Options -Headers @{
        Origin = 'https://vigia-frontend-8084.web.app'; 'Access-Control-Request-Method' = 'GET'; 'Access-Control-Request-Headers' = 'authorization'
    } -SkipHttpErrorCheck
    if ($cors.StatusCode -ne 204 -or $cors.Headers.'Access-Control-Allow-Origin' -ne 'https://vigia-frontend-8084.web.app') { throw 'Firebase CORS check failed.' }
    Stop-Process -Id $process.Id
    $process.WaitForExit()
    $process = Start-CheckedApi
    Send-Check GET '/api/v1/users/me' -Token $session.accessToken | Out-Null
    $env:OpenApi__Enabled = 'false'
    Stop-Process -Id $process.Id
    $process.WaitForExit()
    $process = Start-CheckedApi
    Send-Check GET '/openapi/v1.json' -Expected 404
    Send-Check GET '/scalar/v1' -Expected 404
    Write-Host "PASS: $checks Production HTTP checks, separate migrations, idempotent administrator provisioning, no demo records, Firebase CORS and bearer token retained across restart. Database: $database"
} finally {
    if ($process -and !$process.HasExited) { Stop-Process -Id $process.Id; $process.WaitForExit() }
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
}
