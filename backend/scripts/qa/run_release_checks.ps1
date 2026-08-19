[CmdletBinding()]
param([switch]$SkipExternalIntegration)
$ErrorActionPreference = 'Stop'
$backend = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $backend
try {
    $commands = @(
        { dotnet clean Valora.sln --nologo },
        { dotnet restore Valora.sln --nologo },
        { dotnet build Valora.sln -c Release --no-restore --nologo },
        { dotnet test Valora.sln -c Release --no-build --nologo },
        { dotnet publish Valora.Api/Valora.Api.csproj -c Release --no-restore --nologo },
        { dotnet publish Valora.Web/Valora.Web.csproj -c Release --no-restore --nologo },
        { & (Join-Path $PSScriptRoot 'run_sql_static_checks.ps1') }
    )
    foreach ($command in $commands) {
        & $command
        if ($LASTEXITCODE -ne 0) { throw "Release check falhou com exit code $LASTEXITCODE." }
    }
    Write-Host 'Release checks: PASS' -ForegroundColor Green
} finally { Pop-Location }
