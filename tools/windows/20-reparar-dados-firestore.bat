@echo off
cd /d C:\DBBACK\valoregrouppesquisa

set GOOGLE_APPLICATION_CREDENTIALS=C:\FirebaseKeys\gestordepesquisa-firebase-adminsdk-fbsvc-fe4f2380fd.json

echo [1/3] Backup e dry-run...
node scripts\repair-data-shapes.js --project gestordepesquisa --backup --dry-run
if errorlevel 1 goto erro

echo [2/3] Aplicando reparo...
node scripts\repair-data-shapes.js --project gestordepesquisa --backup --apply
if errorlevel 1 goto erro

echo [3/3] Validando dados PRD...
node scripts\validate-prd-data.js --project gestordepesquisa
if errorlevel 1 goto erro

echo DADOS REPARADOS.
pause
exit /b 0

:erro
echo FALHA NO REPARO.
pause
exit /b 1
