@echo off
cd /d C:\DBBACK\valoregrouppesquisa
firebase deploy --only firestore:rules --project gestordepesquisa
if errorlevel 1 (
  echo.
  echo Firebase CLI falhou. Alternativa manual:
  echo Firebase Console ^> Firestore Database ^> Rules
  echo Cole o conteudo de firestore.rules e clique em Publish.
)
pause
