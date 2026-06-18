@echo off
setlocal
cd /d "%~dp0"
title Valora Group - Servidor local

echo ===============================================
echo VALORA GROUP - SERVIDOR LOCAL
echo ===============================================
echo O sistema escolhera automaticamente uma porta local disponivel.
echo Mantenha esta janela aberta enquanto estiver usando o sistema.
echo.

where python >nul 2>nul
if %errorlevel%==0 (
  python server.py
  goto end
)

where py >nul 2>nul
if %errorlevel%==0 (
  py server.py
  goto end
)

echo Python nao foi encontrado no Windows.
echo Instale o Python em https://www.python.org/downloads/
echo Depois execute este arquivo novamente.

:end
pause
