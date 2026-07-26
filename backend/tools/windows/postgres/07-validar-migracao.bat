@echo off
setlocal
cd /d %~dp0\..\..\..
echo Valora Pulse - PostgreSQL transition helper
node migration\validate-migration.js
