@echo off
setlocal
cd /d %~dp0\..\..\..
echo Valora Insight™ - PostgreSQL transition helper
node migration\validate-migration.js
