@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/7] Validando sintaxe...
call npm run check
if errorlevel 1 goto erro

echo [2/7] Validando FAQ...
node scripts\validate-faq-rendering.js
if errorlevel 1 goto erro

echo [3/7] Validando acesso admin...
node scripts\validate-admin-access.js
if errorlevel 1 goto erro

echo [4/7] Gerando build...
call npm run build:prod
if errorlevel 1 goto erro

echo [5/7] Publicando IIS...
robocopy C:\DBBACK\valoregrouppesquisa\dist C:\inetpub\wwwroot\valoragroup /MIR
if errorlevel 8 goto erro

copy /Y C:\DBBACK\valoregrouppesquisa\templates\iis\web.config C:\inetpub\wwwroot\valoragroup\web.config

echo [6/7] Reiniciando IIS...
iisreset

echo [7/7] Health check...
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro

echo CORRECAO CONCLUIDA.
pause
exit /b 0

:erro
echo ERRO NA CORRECAO.
pause
exit /b 1
