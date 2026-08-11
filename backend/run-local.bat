@echo off
setlocal
cd /d "%~dp0"
set ASPNETCORE_ENVIRONMENT=Development
start "Valora.Api" dotnet run --no-launch-profile --project Valora.Api\Valora.Api.csproj --urls http://localhost:5080
set Api__BaseUrl=http://localhost:5080
start "Valora.Web" dotnet run --no-launch-profile --project Valora.Web\Valora.Web.csproj --urls "http://localhost:5088;https://localhost:7088"
echo Valora.Api: http://localhost:5080 ^| Valora.Web: http://localhost:5088 e https://localhost:7088
endlocal
