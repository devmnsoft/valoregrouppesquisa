@echo off
cd /d C:\DBBACK\valoregrouppesquisa\communication-gateway
node tests\run-tests.js
if errorlevel 1 goto erro
curl http://localhost:8097/health
if errorlevel 1 goto erro
echo GATEWAY VALIDADO.
pause
exit /b 0
:erro
echo GATEWAY INVALIDO.
pause
exit /b 1
