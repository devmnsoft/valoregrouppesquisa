@echo off
cd /d "%~dp0..\.."
node scripts\publish-iis-prd.js --package-only
pause
