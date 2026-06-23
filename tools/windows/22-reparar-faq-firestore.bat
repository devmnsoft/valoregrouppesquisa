@echo off
cd /d C:\DBBACK\valoregrouppesquisa

set GOOGLE_APPLICATION_CREDENTIALS=C:\FirebaseKeys\gestordepesquisa-firebase-adminsdk-fbsvc-fe4f2380fd.json

echo [1/3] Dry-run do reparo de FAQ...
node scripts\repair-faq-settings.js --project gestordepesquisa --backup --dry-run
if errorlevel 1 goto erro

echo [2/3] Aplicando reparo de FAQ...
node scripts\repair-faq-settings.js --project gestordepesquisa --backup --apply
if errorlevel 1 goto erro

echo [3/3] Validando dados PRD...
node scripts\validate-prd-data.js --project gestordepesquisa
if errorlevel 1 goto erro

echo FAQ REPARADO.
pause
exit /b 0

:erro
echo FALHA NO REPARO DO FAQ.
pause
exit /b 1
