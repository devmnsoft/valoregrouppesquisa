@echo off
setlocal enabledelayedexpansion
cd /d %~dp0\..\..
if "%1"=="" (
  echo Execute o script Linux equivalente em CI ou adapte conforme comandos abaixo.
)
dotnet restore backend\Valora.sln || exit /b 1
dotnet build backend\Valora.sln -c Release --no-restore || exit /b 1
