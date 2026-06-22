@echo off
cd /d C:\DBBACK\valoregrouppesquisa
set GOOGLE_APPLICATION_CREDENTIALS=C:\FirebaseKeys\gestordepesquisa-firebase-adminsdk-fbsvc-fe4f2380fd.json
node scripts\validate-prd-data.js --project gestordepesquisa
pause
