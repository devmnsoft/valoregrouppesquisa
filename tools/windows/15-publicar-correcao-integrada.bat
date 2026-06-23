@echo off
setlocal enabledelayedexpansion
cd /d C:\DBBACK\valoregrouppesquisa
set REPORT=reports\publicacao-correcao-integrada-%DATE:/=-%-%TIME::=-%.txt
set REPORT=%REPORT: =0%

echo Iniciando publicacao integrada > "%REPORT%"
call tools\windows\14-validar-email-certificados-chatbot.bat
if errorlevel 1 goto erro

call npm run build:prod >> "%REPORT%" 2>&1
if errorlevel 1 goto erro

if not exist dist\index.html goto erro
if not exist web.config if not exist dist\web.config goto erro

set IIS_DIR=C:\inetpub\wwwroot\valora
set BACKUP=C:\DBBACK\backups\valora-%DATE:/=-%-%TIME::=-%
set BACKUP=%BACKUP: =0%
mkdir "%BACKUP%" >> "%REPORT%" 2>&1
if exist "%IIS_DIR%" xcopy "%IIS_DIR%" "%BACKUP%" /E /I /Y >> "%REPORT%" 2>&1
if errorlevel 1 goto erro

xcopy dist "%IIS_DIR%" /E /I /Y >> "%REPORT%" 2>&1
if errorlevel 1 goto erro
if exist web.config copy /Y web.config "%IIS_DIR%\web.config" >> "%REPORT%" 2>&1
if errorlevel 1 goto erro

iisreset >> "%REPORT%" 2>&1
if errorlevel 1 goto erro

node scripts\healthcheck-prd.js >> "%REPORT%" 2>&1
if errorlevel 1 goto erro

echo Publicacao concluida. Relatorio: %REPORT%
pause
exit /b 0

:erro
echo A publicacao encontrou erros. Consulte %REPORT%
pause
exit /b 1
