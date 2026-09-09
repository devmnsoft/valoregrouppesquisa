param(
    [string]$ConnectionString = $env:ConnectionStrings__DefaultConnection
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw "Defina ConnectionStrings__DefaultConnection antes de preparar o banco."
}
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw "psql não foi encontrado no PATH. Instale as ferramentas cliente do PostgreSQL."
}

$root = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSCommandPath))
$bootstrap = Join-Path $root "database/postgresql/script_completo.sql"
& psql $ConnectionString -v ON_ERROR_STOP=1 -f $bootstrap
if ($LASTEXITCODE -ne 0) { throw "Falha ao aplicar script_completo.sql." }
