@echo off
cd /d C:\DBBACK\valoregrouppesquisa
set GOOGLE_APPLICATION_CREDENTIALS=C:\FirebaseKeys\gestordepesquisa-firebase-adminsdk-fbsvc-fe4f2380fd.json

echo ========================================
echo CORRIGIR LOGINS PRD - VALORA PULSE
echo ========================================

echo [1/3] Diagnosticando usuarios Auth...
node scripts\diagnose-auth-users.js --project gestordepesquisa

echo [2/3] Redefinindo senhas de homologacao...
node scripts\reset-prd-test-passwords.js --project gestordepesquisa --apply

echo [3/3] Validando novamente...
node scripts\diagnose-auth-users.js --project gestordepesquisa

pause
