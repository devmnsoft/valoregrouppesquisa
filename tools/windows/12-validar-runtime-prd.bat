@echo off
cd /d C:\DBBACK\valoregrouppesquisa
node scripts\validate-runtime-capabilities.js
node scripts\validate-email-environment.js
pause
