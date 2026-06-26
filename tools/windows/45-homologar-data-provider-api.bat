@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo Homologando DATA_PROVIDER=api local...
call npm run api:provider
if errorlevel 1 goto erro
call npm run journey:provider
if errorlevel 1 goto erro
call npm run architecture:warnings
if errorlevel 1 goto erro
call npm run backend:build
if errorlevel 1 goto erro
call npm run backend:test
if errorlevel 1 goto erro
call npm run backend:health
if errorlevel 1 goto erro

echo DATA_PROVIDER=api homologado para ambiente controlado.
pause
exit /b 0
:erro
echo Falha na homologacao DATA_PROVIDER=api.
pause
exit /b 1
