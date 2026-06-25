@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/6] Validando código...
call npm run check
if errorlevel 1 goto erro

echo [2/6] Validando template de e-mail...
node scripts\validate-email-template.js
if errorlevel 1 goto erro

echo [3/6] Validando integração com gateway...
node scripts\validate-gateway-integration.js
if errorlevel 1 goto erro

echo [4/6] Validando fluxo de resultado...
node scripts\validate-result-email-flow.js
if errorlevel 1 goto erro

echo [5/6] Build produção...
call npm run build:prod
if errorlevel 1 goto erro

echo [6/6] Health check...
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro

echo ENVIO DE EMAIL VALIDADO.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DO EMAIL.
pause
exit /b 1
