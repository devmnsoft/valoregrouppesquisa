[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot '../../database/postgresql/script_completo.sql'
$sql = Get-Content $scriptPath -Raw
$executable = [regex]::Replace($sql, '(?m)--.*$', '')
$failures = @()
if ($executable -match '(?i)\bDROP\s+TABLE\b') { $failures += 'DROP TABLE encontrado' }
if ($executable -match '(?i)\bTRUNCATE(?:\s+TABLE)?\b') { $failures += 'TRUNCATE encontrado' }
if ($sql -notmatch '(?is)api_keys\s+ADD\s+COLUMN\s+IF\s+NOT\s+EXISTS\s+key_hash') { $failures += 'api_keys.key_hash não garantido' }
if ($sql -notmatch '(?is)notifications\s+ADD\s+COLUMN\s+IF\s+NOT\s+EXISTS\s+message') { $failures += 'notifications.message não garantido' }
$unguarded = [regex]::Matches($executable, '(?im)^CREATE\s+TABLE\s+(?!IF\s+NOT\s+EXISTS)')
if ($unguarded.Count -gt 0) { $failures += "$($unguarded.Count) CREATE TABLE top-level sem IF NOT EXISTS" }
if ($failures.Count) { $failures | ForEach-Object { Write-Error $_ }; exit 1 }
Write-Host 'SQL static checks: PASS' -ForegroundColor Green
