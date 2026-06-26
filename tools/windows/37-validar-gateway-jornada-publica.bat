@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/10] Validando código...
call npm run check
if errorlevel 1 goto erro

echo [2/10] Validando provider da jornada pública...
node scripts\validate-public-journey-provider.js
if errorlevel 1 goto erro

echo [3/10] Validando pesquisa destaque...
node scripts\validate-featured-home-survey.js --project gestordepesquisa
if errorlevel 1 goto erro

echo [4/10] Validando fluxo público...
node scripts\validate-public-survey-flow.js --project gestordepesquisa
if errorlevel 1 goto erro

echo [5/10] Validando envio de resultado...
node scripts\validate-result-email-flow.js
if errorlevel 1 goto erro

echo [6/10] Validando certificados...
node scripts\validate-certificates.js
if errorlevel 1 goto erro

echo [7/10] Validando mobile...
node scripts\validate-mobile-menu.js
if errorlevel 1 goto erro

echo [8/10] Validando signup/login...
node scripts\validate-signup-login.js
if errorlevel 1 goto erro

echo [9/10] Build produção...
call npm run build:prod
if errorlevel 1 goto erro

echo [10/10] Release candidate...
node scripts\validate-release-candidate.js
if errorlevel 1 goto erro

echo VALIDAÇÃO CONCLUÍDA.
pause
exit /b 0

:erro
echo VALIDAÇÃO FALHOU.
pause
exit /b 1
