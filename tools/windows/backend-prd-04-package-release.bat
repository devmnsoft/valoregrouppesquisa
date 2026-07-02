@echo off
setlocal enabledelayedexpansion
cd /d %~dp0\..\..
if "%1"=="" (
  echo Execute o script Linux equivalente em CI ou adapte conforme comandos abaixo.
)
powershell -NoProfile -ExecutionPolicy Bypass -Command "$v=(Get-Content VERSION -Raw).Trim(); $pkg='publish/package/valora-'+$v; if(Test-Path $pkg){Remove-Item $pkg -Recurse -Force}; New-Item -ItemType Directory -Force $pkg,$pkg/sql,$pkg/docs | Out-Null; Copy-Item ('publish/valora-'+$v+'/api') $pkg/api -Recurse; Copy-Item ('publish/valora-'+$v+'/web') $pkg/web -Recurse; Copy-Item database/postgresql/*.sql $pkg/sql; Copy-Item VERSION,RELEASE_CANDIDATE_NOTES.md,CUTOVER_PLAN.md,ROLLBACK_PLAN.md,BACKUP_RESTORE_RUNBOOK.md,SECURITY_HARDENING_CHECKLIST.md $pkg/docs; Get-ChildItem $pkg -Recurse -File | Get-FileHash -Algorithm SHA256 | ForEach-Object { $_.Hash + '  ' + $_.Path } | Set-Content ($pkg+'/ARTIFACTS.sha256'); Compress-Archive -Path $pkg -DestinationPath ('publish/valora-'+$v+'.zip') -Force" || exit /b 1
