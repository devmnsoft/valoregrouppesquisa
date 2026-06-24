@echo off
cd /d C:\DBBACK\valoregrouppesquisa
echo [1/8] Aplicando config de producao...
node scripts\apply-config.js --env production
if errorlevel 1 goto erro
echo [2/8] Validando config...
node scripts\validate-config-profile.js --file config.js
if errorlevel 1 goto erro
echo [3/8] Validando codigo...
call npm run check
if errorlevel 1 goto erro
echo [4/8] Validando envio de resultados...
node scripts\validate-result-email-flow.js
if errorlevel 1 goto erro
echo [5/8] Build producao...
call npm run build:prod
if errorlevel 1 goto erro
echo [6/8] Publicando IIS...
robocopy C:\DBBACK\valoregrouppesquisa\dist C:\inetpub\wwwroot\valoragroup /MIR
if errorlevel 8 goto erro
copy /Y C:\DBBACK\valoregrouppesquisa\templates\iis\web.config C:\inetpub\wwwroot\valoragroup\web.config
echo [7/8] Reiniciando IIS...
iisreset
echo [8/8] Health check...
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro
echo PUBLICACAO CONCLUIDA.
pause
exit /b 0
:erro
echo PUBLICACAO FALHOU.
pause
exit /b 1
