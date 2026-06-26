@echo off
cd /d C:\DBBACK\valoregrouppesquisa
node scripts\validate-real-migration-dry-run.js
if errorlevel 1 goto erro
node migration\validate-migration.js --dry-run
if errorlevel 1 goto erro
node migration\compare-firebase-postgres.js --dry-run
if errorlevel 1 goto erro
echo ENSAIO DE MIGRACAO COM DADOS REAIS/DRY-RUN VALIDADO.
pause
exit /b 0
:erro
echo FALHA NO ENSAIO DE MIGRACAO.
pause
exit /b 1
