[CmdletBinding()]
param([switch]$SkipExternalIntegration)
$ErrorActionPreference = 'Stop'
$backend = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $backend
try {
    dotnet test Valora.sln --configuration Release --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet test falhou com exit code $LASTEXITCODE." }
    Write-Host 'QA tests: PASS' -ForegroundColor Green
} finally { Pop-Location }
