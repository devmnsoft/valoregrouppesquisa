@echo off
cd /d C:\DBBACK\valoregrouppesquisa
call npm run check
if errorlevel 1 goto erro
node scripts\validate-cutover-readiness.js
if errorlevel 1 goto erro
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro
echo PRD validada sem trocar DATA_PROVIDER=firebase.
pause
exit /b 0
:erro
echo Falha na validacao segura da PRD.
pause
exit /b 1
