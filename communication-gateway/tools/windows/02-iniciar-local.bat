@echo off
cd /d C:\DBBACK\valoregrouppesquisa\communication-gateway
copy /Y .env.local.example .env
node server.js
pause
