$ErrorActionPreference='Stop'
$migrate = Get-Content database/migrate.sql -Raw
1..29 | ForEach-Object { $id = '{0:000}' -f $_; if ($migrate -notmatch "database/migrations/$id") { throw "Missing migration $id in database/migrate.sql" } }
if ($migrate -match '\\i\s+migrations/') { throw 'Broken migration path detected' }
foreach ($token in 'client_onboarding','client_communications','job_execution_logs','client_invoices','client_subscriptions','SuperAdmin') {
  if ((Get-Content database/script_completo.sql,database/validate_schema_habitflow.sql,database/migrations/029_operational_completeness_v61.sql -Raw) -notmatch $token) { throw "Missing $token" }
}
Write-Host 'Database script checks passed.'
