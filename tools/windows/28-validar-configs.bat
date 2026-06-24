@echo off
cd /d C:\DBBACK\valoregrouppesquisa
node scripts\validate-config-profile.js --env production
if errorlevel 1 goto erro
node scripts\validate-config-profile.js --env local
if errorlevel 1 goto erro
node scripts\validate-config-profile.js --env local-firebase
if errorlevel 1 goto erro
echo CONFIGS VALIDAS.
pause
exit /b 0
:erro
echo CONFIG INVALIDA.
pause
exit /b 1
