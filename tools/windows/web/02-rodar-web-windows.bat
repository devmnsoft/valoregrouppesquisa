@echo off
cd /d C:\DBBACK\valoregrouppesquisa\backend\Valora.Web
set ASPNETCORE_ENVIRONMENT=Development
dotnet run --urls http://localhost:5088
pause
