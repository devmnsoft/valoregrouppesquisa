@echo off
cd /d C:\DBBACK\valoregrouppesquisa
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
pause
