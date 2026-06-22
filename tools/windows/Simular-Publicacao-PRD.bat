@echo off
cd /d "%~dp0..\.."
node scripts\publish-iis-prd.js --dry-run
pause
