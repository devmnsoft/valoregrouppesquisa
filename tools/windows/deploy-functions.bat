@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/4] Instalando dependencias das Functions...
call npm run functions:install
if errorlevel 1 goto erro

echo [2/4] Validando Node 22...
call npm run functions:node22-readiness
if errorlevel 1 goto erro

echo [3/4] Lint Functions...
call npm run functions:lint
if errorlevel 1 goto erro

echo [4/4] Deploy Functions...
call firebase deploy --only functions --project gestordepesquisa
if errorlevel 1 goto erro

echo Functions publicadas com sucesso.
pause
exit /b 0

:erro
echo FALHA NO DEPLOY DAS FUNCTIONS.
pause
exit /b 1
