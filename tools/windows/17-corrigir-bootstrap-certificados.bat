@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/6] Validando sintaxe...
call npm run check
if errorlevel 1 goto erro

echo [2/6] Validando Firebase bundle...
node scripts\validate-firebase-bundle.js
if errorlevel 1 goto erro

echo [3/6] Validando handlers...
node scripts\validate-action-handlers.js
if errorlevel 1 goto erro

echo [4/6] Validando certificados...
node scripts\validate-certificates.js
if errorlevel 1 goto erro

echo [5/6] Gerando build...
call npm run build:prod
if errorlevel 1 goto erro

echo [6/6] Publicando IIS...
robocopy C:\DBBACK\valoregrouppesquisa\dist C:\inetpub\wwwroot\valoragroup /MIR
if errorlevel 8 goto erro

copy /Y C:\DBBACK\valoregrouppesquisa\templates\iis\web.config C:\inetpub\wwwroot\valoragroup\web.config

iisreset

node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro

echo CORRECAO CONCLUIDA
pause
exit /b 0

:erro
echo ERRO NA CORRECAO
pause
exit /b 1
