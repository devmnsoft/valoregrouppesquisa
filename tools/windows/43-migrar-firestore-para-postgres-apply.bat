@echo off
cd /d C:\DBBACK\valoregrouppesquisa
node migration\export-firestore.js
if errorlevel 1 goto erro
node migration\transform-firestore-to-postgres.js
if errorlevel 1 goto erro
node migration\import-postgres.js --apply --backup
if errorlevel 1 goto erro
echo MIGRACAO FIRESTORE PARA POSTGRES APLICADA.
pause
exit /b 0
:erro
echo FALHA NA MIGRACAO FIRESTORE PARA POSTGRES.
pause
exit /b 1
