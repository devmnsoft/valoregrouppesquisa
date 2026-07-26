@echo off
if "%VALORA_BACKUP_DIR%"=="" set VALORA_BACKUP_DIR=backups\postgresql
if not exist "%VALORA_BACKUP_DIR%" mkdir "%VALORA_BACKUP_DIR%"
if "%VALORA_POSTGRES_DB%"=="" set VALORA_POSTGRES_DB=valora_hml
if "%VALORA_POSTGRES_USER%"=="" set VALORA_POSTGRES_USER=valora_hml
if "%VALORA_POSTGRES_HOST%"=="" set VALORA_POSTGRES_HOST=localhost
if "%VALORA_POSTGRES_PORT%"=="" set VALORA_POSTGRES_PORT=5432
set OUT=%VALORA_BACKUP_DIR%\valora_%VALORA_POSTGRES_DB%_%DATE:~-4%%DATE:~3,2%%DATE:~0,2%_%TIME:~0,2%%TIME:~3,2%%TIME:~6,2%.dump
set OUT=%OUT: =0%
pg_dump -h %VALORA_POSTGRES_HOST% -p %VALORA_POSTGRES_PORT% -U %VALORA_POSTGRES_USER% -d %VALORA_POSTGRES_DB% -Fc -f "%OUT%"
if not exist "%OUT%" exit /b 1
echo Backup criado: %OUT%>> "%VALORA_BACKUP_DIR%\backup.log"
