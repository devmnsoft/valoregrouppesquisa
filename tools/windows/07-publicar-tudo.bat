@echo off
cd /d C:\DBBACK\valoregrouppesquisa
set GOOGLE_APPLICATION_CREDENTIALS=C:\FirebaseKeys\gestordepesquisa-firebase-adminsdk-fbsvc-fe4f2380fd.json

echo ========================================
echo VALORA PULSE - PUBLICACAO COMPLETA PRD
echo ========================================

echo [1/6] Validando codigo...
npm run check
if errorlevel 1 goto erro

echo [2/6] Preparando export...
if not exist exports mkdir exports
copy /Y C:\DBBACK\valora-local-export-20260622-0037.json exports\valora-prd-export-20260622-0037.json

echo [3/6] Importando base de teste/local para Firestore PRD...
node scripts\import-local-export-to-firebase.js --file .\exports\valora-prd-export-20260622-0037.json --project gestordepesquisa --apply --backup --create-auth-users --include-responses
if errorlevel 1 goto erro

echo [4/6] Validando base PRD...
node scripts\validate-prd-data.js --project gestordepesquisa
if errorlevel 1 goto erro

echo [5/6] Gerando build de producao...
npm run build:prod
if errorlevel 1 goto erro

echo [6/6] Publicando no IIS...
robocopy C:\DBBACK\valoregrouppesquisa\dist C:\inetpub\wwwroot\valoragroup /MIR
copy /Y C:\DBBACK\valoregrouppesquisa\templates\iis\web.config C:\inetpub\wwwroot\valoragroup\web.config
iisreset

echo Rodando health check...
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa

echo ========================================
echo PUBLICACAO FINALIZADA
echo ========================================
pause
exit /b 0

:erro
echo ========================================
echo ERRO NA PUBLICACAO
echo Verifique a etapa acima.
echo ========================================
pause
exit /b 1
