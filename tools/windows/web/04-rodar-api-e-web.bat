@echo off
cd /d C:\DBBACK\valoregrouppesquisa
start cmd /k dotnet run --project backend\Valora.Api\Valora.Api.csproj --urls http://localhost:5080
start cmd /k npm run web:run
pause
