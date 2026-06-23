@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/8] Validando sintaxe...
call npm run check
if errorlevel 1 goto erro

echo [2/8] Auditando formatos dos dados...
node scripts\audit-data-shapes.js --project gestordepesquisa
if errorlevel 1 goto erro

echo [3/8] Reparando formatos em modo dry-run...
node scripts\repair-data-shapes.js --project gestordepesquisa --dry-run
if errorlevel 1 goto erro

echo [4/8] Validando contratos de dados...
node scripts\validate-data-contracts.js
if errorlevel 1 goto erro

echo [5/8] Validando resiliencia de render...
node scripts\validate-render-resilience.js
if errorlevel 1 goto erro

echo [6/8] Gerando build...
call npm run build:prod
if errorlevel 1 goto erro

echo [7/8] Publicando IIS...
robocopy C:\DBBACK\valoregrouppesquisa\dist C:\inetpub\wwwroot\valoragroup /MIR
if errorlevel 8 goto erro

copy /Y C:\DBBACK\valoregrouppesquisa\templates\iis\web.config C:\inetpub\wwwroot\valoragroup\web.config

echo [8/8] Reiniciando IIS e validando...
iisreset
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro

echo CORRECAO CONCLUIDA.
pause
exit /b 0

:erro
echo ERRO NA CORRECAO.
pause
exit /b 1
