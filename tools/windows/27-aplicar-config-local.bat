@echo off
cd /d C:\DBBACK\valoregrouppesquisa
node scripts\apply-config.js --env local
node scripts\validate-config-profile.js --file config.js
pause
