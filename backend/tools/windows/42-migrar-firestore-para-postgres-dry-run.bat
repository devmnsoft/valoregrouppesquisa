@echo off
cd /d C:\DBBACK\valoregrouppesquisa
node migration\export-firestore.js
if errorlevel 1 exit /b 1
node migration\transform-firestore-to-postgres.js
if errorlevel 1 exit /b 1
node migration\import-postgres.js --dry-run --batch-size 100
