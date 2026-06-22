@echo off
cd /d "%~dp0..\.."
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa --check-firebase
pause
