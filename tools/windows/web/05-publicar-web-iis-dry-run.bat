@echo off
cd /d C:\DBBACK\valoregrouppesquisa
echo Publicacao IIS dry-run Valora.Web
call dotnet publish backend\Valora.Web\Valora.Web.csproj -c Release -o publish\valora-web-iis-dry-run
if errorlevel 1 exit /b 1
if not exist backend\Valora.Web\web.config exit /b 1
if not exist backend\Valora.Web\wwwroot exit /b 1
findstr /S /I /C:"Password=" backend\Valora.Web\appsettings*.json && exit /b 1
findstr /S /I /C:"private_key" backend\Valora.Web\appsettings*.json && exit /b 1
findstr /I /C:"Api" backend\Valora.Web\appsettings.json >nul || exit /b 1
if not exist publish\valora-web-iis-dry-run exit /b 1
node scripts\validate-no-binary-assets.js
if errorlevel 1 exit /b 1
echo Dry-run IIS Valora.Web OK.
exit /b 0
