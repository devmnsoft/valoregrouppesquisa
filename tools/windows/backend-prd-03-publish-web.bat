@echo off
setlocal enabledelayedexpansion
cd /d %~dp0\..\..
if "%1"=="" (
  echo Execute o script Linux equivalente em CI ou adapte conforme comandos abaixo.
)
if exist publish\valora-0.9.0-rc1\web rmdir /s /q publish\valora-0.9.0-rc1\web
dotnet publish backend\Valora.Web\Valora.Web.csproj -c Release -o publish\valora-0.9.0-rc1\web /p:UseAppHost=false || exit /b 1
