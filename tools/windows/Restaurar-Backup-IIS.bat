@echo off
cd /d "%~dp0..\.."
node scripts\restore-iis-backup.js --latest
pause
