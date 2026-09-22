param(
    [string]$PsqlConnection = $env:VALORA_PSQL_CONNECTION
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($PsqlConnection)) {
    throw "Defina VALORA_PSQL_CONNECTION com uma URI PostgreSQL ou conninfo libpq antes de preparar o banco."
}
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw "psql não foi encontrado no PATH. Instale as ferramentas cliente do PostgreSQL."
}

$root = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSCommandPath))
$bootstrap = Join-Path $root "database/postgresql/script_completo.sql"
& psql $PsqlConnection -X -v ON_ERROR_STOP=1 -f $bootstrap
if ($LASTEXITCODE -ne 0) { throw "Falha ao aplicar script_completo.sql." }
