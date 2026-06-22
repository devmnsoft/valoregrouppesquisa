@echo off
cd /d C:\DBBACK\valoregrouppesquisa
set GOOGLE_APPLICATION_CREDENTIALS=C:\FirebaseKeys\gestordepesquisa-firebase-adminsdk-fbsvc-fe4f2380fd.json
if not exist exports mkdir exports
copy /Y C:\DBBACK\valora-local-export-20260622-0037.json exports\valora-prd-export-20260622-0037.json
node scripts\import-local-export-to-firebase.js --file .\exports\valora-prd-export-20260622-0037.json --project gestordepesquisa --apply --backup --create-auth-users --include-responses
node scripts\validate-prd-data.js --project gestordepesquisa
pause
