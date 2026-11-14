param(
    [string]$Database = 'vigia_local_integration',
    [int]$Port = 5125,
    [string]$ConfigurationFile = '',
    [string]$Dotnet = ''
)
$ErrorActionPreference = 'Stop'
if ($Database -notmatch '^vigia_local_[a-zA-Z0-9_]+$') { throw 'Use a database name prefixed vigia_local_.' }
$repo = Split-Path $PSScriptRoot -Parent
if (!$Dotnet) {
    $userSdk = Join-Path $env:USERPROFILE '.dotnet/dotnet.exe'
    $Dotnet = if (Test-Path -LiteralPath $userSdk) { $userSdk } else { (Get-Command dotnet -ErrorAction Stop).Source }
}
if (!$ConfigurationFile) { $ConfigurationFile = Join-Path $repo 'VigiaPlatform.API/appsettings.Development.json' }
$config = Get-Content -LiteralPath $ConfigurationFile -Raw | ConvertFrom-Json
$connection = $config.ConnectionStrings.DefaultConnection
if (!$connection) { throw 'DefaultConnection is missing from the selected local configuration file.' }
if ($connection -notmatch '(?i)(database|initial catalog)\s*=') { throw 'The connection string must specify a database.' }
$connection = $connection -replace '(?i)((?:database|initial catalog)\s*=\s*)[^;]*', "`${1}$Database"
$localDirectory = Join-Path $repo '.local'
New-Item -ItemType Directory -Force -Path $localDirectory | Out-Null
$accessFile = Join-Path $localDirectory 'access.json'
if (Test-Path -LiteralPath $accessFile) {
    $access = Get-Content -LiteralPath $accessFile -Raw | ConvertFrom-Json
} else {
    $password = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
    $access = @{ usernames = @('admin.local', 'warehouse.local', 'site.local'); password = $password }
    $access | ConvertTo-Json | Set-Content -LiteralPath $accessFile -Encoding utf8
}
$names = @('ConnectionStrings__DefaultConnection', 'Development__SeedDemoData', 'Development__DemoPassword', 'ASPNETCORE_ENVIRONMENT', 'ASPNETCORE_URLS')
$previous = @{}
foreach ($name in $names) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
try {
    $env:ConnectionStrings__DefaultConnection = $connection
    $env:Development__SeedDemoData = 'true'
    $env:Development__DemoPassword = $access.password
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_URLS = "http://localhost:$Port"
    Write-Host "Local API: http://localhost:$Port | Scalar: http://localhost:$Port/scalar"
    Write-Host "Local account details: $accessFile"
    & $Dotnet run --project (Join-Path $repo 'VigiaPlatform.API') --no-launch-profile
    if ($LASTEXITCODE -ne 0) { throw "The API exited with code $LASTEXITCODE." }
} finally {
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
}
