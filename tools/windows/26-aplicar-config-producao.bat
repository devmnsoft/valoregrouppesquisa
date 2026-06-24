@echo off
cd /d C:\DBBACK\valoregrouppesquisa
node scripts\apply-config.js --env production
node scripts\validate-config-profile.js --file config.js
pause
