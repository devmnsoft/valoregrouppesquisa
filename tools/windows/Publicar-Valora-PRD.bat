@echo off
cd /d "%~dp0..\.."
powershell -ExecutionPolicy Bypass -File "tools\windows\Publicar-Valora-PRD.ps1"
pause
