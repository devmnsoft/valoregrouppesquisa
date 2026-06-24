@echo off
cd /d C:\DBBACK\valoregrouppesquisa
call npm run check
if errorlevel 1 goto erro
node scripts\validate-result-email-flow.js
if errorlevel 1 goto erro
call npm run build:prod
if errorlevel 1 goto erro
echo Validacao concluida.
pause
exit /b 0
:erro
echo Falha na validacao do envio de resultados.
pause
exit /b 1
