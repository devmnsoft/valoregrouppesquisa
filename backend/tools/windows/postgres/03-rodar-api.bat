@echo off
setlocal
cd /d %~dp0\..\..\..
echo Valora Pulse - PostgreSQL transition helper
dotnet run --project backend\Valora.Api\Valora.Api.csproj
