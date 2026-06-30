@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/5] Instalando dependencias das Functions...
call npm run functions:install
if errorlevel 1 goto erro

echo [2/5] Validando runtime Node 22...
call npm run functions:node22-readiness
if errorlevel 1 goto erro

echo [3/5] Validando sintaxe das Functions...
call npm run functions:lint
if errorlevel 1 goto erro

echo [4/5] Publicando Functions...
call firebase deploy --only functions --project gestordepesquisa
if errorlevel 1 goto erro

echo [5/5] Functions publicadas.
pause
exit /b 0

:erro
echo FALHA NO DEPLOY DAS FUNCTIONS.
pause
exit /b 1
