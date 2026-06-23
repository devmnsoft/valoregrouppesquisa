@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/5] Validando codigo...
call npm run check
if errorlevel 1 goto erro

echo [2/5] Validando capacidades...
node scripts\validate-runtime-capabilities.js
if errorlevel 1 goto erro

echo [3/5] Validando ambiente de e-mail...
node scripts\validate-email-environment.js
if errorlevel 1 goto erro

echo [4/5] Gerando build...
call npm run build:prod
if errorlevel 1 goto erro

echo [5/5] Publicando no IIS...
robocopy C:\DBBACK\valoregrouppesquisa\dist C:\inetpub\wwwroot\valoragroup /MIR
if errorlevel 8 goto erro
copy /Y C:\DBBACK\valoregrouppesquisa\templates\iis\web.config C:\inetpub\wwwroot\valoragroup\web.config
iisreset
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro

echo PUBLICACAO CONCLUIDA
pause
exit /b 0
:erro
echo PUBLICACAO INTERROMPIDA
pause
exit /b 1
