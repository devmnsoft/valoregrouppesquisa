@echo off
setlocal enabledelayedexpansion
cd /d %~dp0\..\..
if "%1"=="" (
  echo Execute o script Linux equivalente em CI ou adapte conforme comandos abaixo.
)
if exist publish\valora-0.9.0-rc1\api rmdir /s /q publish\valora-0.9.0-rc1\api
dotnet publish backend\Valora.Api\Valora.Api.csproj -c Release -o publish\valora-0.9.0-rc1\api /p:UseAppHost=false || exit /b 1
