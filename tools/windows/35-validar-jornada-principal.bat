@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/9] Validando codigo...
call npm run check
if errorlevel 1 goto erro

echo [2/9] Validando pesquisa destaque...
node scripts\validate-featured-home-survey.js --project gestordepesquisa
if errorlevel 1 goto erro

echo [3/9] Validando fluxo publico...
node scripts\validate-public-survey-flow.js --project gestordepesquisa
if errorlevel 1 goto erro

echo [4/9] Validando menu mobile...
node scripts\validate-mobile-menu.js
if errorlevel 1 goto erro

echo [5/9] Validando certificados...
node scripts\validate-certificates.js
if errorlevel 1 goto erro

echo [6/9] Validando cadastro/login...
node scripts\validate-signup-login.js
if errorlevel 1 goto erro

echo [7/9] Validando envio de resultado...
node scripts\validate-result-email-flow.js
if errorlevel 1 goto erro

echo [8/9] Build producao...
call npm run build:prod
if errorlevel 1 goto erro

echo [9/9] Validando release candidate...
node scripts\validate-release-candidate.js
if errorlevel 1 goto erro

echo VALIDACAO CONCLUIDA.
pause
exit /b 0

:erro
echo VALIDACAO FALHOU.
pause
exit /b 1
