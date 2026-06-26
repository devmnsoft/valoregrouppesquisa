@echo off
cd /d C:\DBBACK\valoregrouppesquisa
node migration\export-firestore.js
if errorlevel 1 goto erro
node migration\transform-firestore-to-postgres.js
if errorlevel 1 goto erro
node migration\import-postgres.js --dry-run
if errorlevel 1 goto erro
echo DRY-RUN FIRESTORE PARA POSTGRES VALIDADO.
pause
exit /b 0
:erro
echo FALHA NO DRY-RUN FIRESTORE PARA POSTGRES.
pause
exit /b 1
