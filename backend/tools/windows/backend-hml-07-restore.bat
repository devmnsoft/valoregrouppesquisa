@echo off
if not "%CONFIRM_RESTORE%"=="RESTORE_LOCAL_HML" echo Defina CONFIRM_RESTORE=RESTORE_LOCAL_HML para confirmar restore destrutivo local/hml. && exit /b 2
if "%VALORA_ENVIRONMENT%"=="Production" if not "%CONFIRM_PRODUCTION_RESTORE%"=="true" echo Restore em producao bloqueado sem CONFIRM_PRODUCTION_RESTORE=true && exit /b 3
if "%VALORA_RESTORE_FILE%"=="" echo Informe VALORA_RESTORE_FILE && exit /b 4
if "%VALORA_POSTGRES_DB%"=="" set VALORA_POSTGRES_DB=valora_hml
if "%VALORA_POSTGRES_USER%"=="" set VALORA_POSTGRES_USER=valora_hml
if "%VALORA_POSTGRES_HOST%"=="" set VALORA_POSTGRES_HOST=localhost
if "%VALORA_POSTGRES_PORT%"=="" set VALORA_POSTGRES_PORT=5432
pg_restore -h %VALORA_POSTGRES_HOST% -p %VALORA_POSTGRES_PORT% -U %VALORA_POSTGRES_USER% -d %VALORA_POSTGRES_DB% --clean --if-exists "%VALORA_RESTORE_FILE%"
