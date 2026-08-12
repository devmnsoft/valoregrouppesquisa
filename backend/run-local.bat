@echo off
setlocal
cd /d "%~dp0"
set ASPNETCORE_ENVIRONMENT=Development
start "Valora.Api" dotnet run --no-launch-profile --project Valora.Api\Valora.Api.csproj --urls http://localhost:5080
powershell -NoProfile -Command "$ready=$false; 1..60 | ForEach-Object { try { $response=Invoke-WebRequest -UseBasicParsing -TimeoutSec 2 http://localhost:5080/health; if ($response.StatusCode -eq 200) { $ready=$true; break } } catch {}; Start-Sleep -Seconds 1 }; if (-not $ready) { Write-Error 'Tempo esgotado aguardando Valora.Api em http://localhost:5080/health.'; exit 1 }"
if errorlevel 1 exit /b 1
set Api__BaseUrl=http://localhost:5080
start "Valora.Web" dotnet run --no-launch-profile --project Valora.Web\Valora.Web.csproj --urls "http://localhost:5088;https://localhost:7088"
echo Valora.Api: http://localhost:5080 ^| Valora.Web: http://localhost:5088 e https://localhost:7088
endlocal
