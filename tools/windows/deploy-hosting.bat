@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/4] Gerando dist...
call npm run build:prod
if errorlevel 1 goto erro

echo [2/4] Validando dist...
call npm run hosting:dist-build
if errorlevel 1 goto erro

echo [3/4] Publicando Hosting...
call firebase deploy --only hosting --project gestordepesquisa
if errorlevel 1 goto erro

echo [4/4] Hosting publicado.
pause
exit /b 0

:erro
echo FALHA NO DEPLOY DO HOSTING.
pause
exit /b 1
