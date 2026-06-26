@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo Este script publica a ponte arquitetural no IIS sem alterar DATA_PROVIDER automaticamente para api.
set /p CONFIRMA=Publicar frontend atual no IIS em modo configurado? (S/N): 
if /I not "%CONFIRMA%"=="S" exit /b 0
call npm run build:prod
if errorlevel 1 goto erro
node scripts\publish-iis-prd.js --apply --mode firebase
if errorlevel 1 goto erro
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro
echo PUBLICACAO DA PONTE CONCLUIDA.
pause
exit /b 0
:erro
echo FALHA NA PUBLICACAO DA PONTE.
pause
exit /b 1
