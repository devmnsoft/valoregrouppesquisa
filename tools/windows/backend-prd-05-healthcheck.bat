@echo off
setlocal enabledelayedexpansion
cd /d %~dp0\..\..
if "%1"=="" (
  echo Execute o script Linux equivalente em CI ou adapte conforme comandos abaixo.
)
if "%VALORA_API_URL%"=="" set VALORA_API_URL=http://localhost:5080
for %%P in (/health /health/database /health/migration /health/email /health/storage /health/version) do curl -fsS "%VALORA_API_URL%%%P" || exit /b 1
