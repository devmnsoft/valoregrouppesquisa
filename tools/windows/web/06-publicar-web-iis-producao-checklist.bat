@echo off
cd /d C:\DBBACK\valoregrouppesquisa
call npm run web:build
exit /b %errorlevel%
