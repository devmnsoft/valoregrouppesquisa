@echo off
setlocal
cd /d %~dp0\..\..\..
echo Publicacao dry-run Valora.Web para IIS
if exist publish\valora-web rmdir /s /q publish\valora-web
dotnet publish backend\Valora.Web\Valora.Web.csproj -c Release -o publish\valora-web
if errorlevel 1 exit /b 1
if not exist publish\valora-web\web.config exit /b 1
if not exist publish\valora-web\wwwroot exit /b 1
if not exist publish\valora-web\Views exit /b 1
findstr /S /I /C:"/web-config.js" backend\Valora.Web\Controllers\WebConfigController.cs backend\Valora.Web\Views\Shared\_Layout.cshtml >nul || exit /b 1
findstr /S /I /C:"private_key" /C:"SMTP password" /C:"ConnectionStrings" backend\Valora.Web\appsettings*.json && exit /b 1
for /R publish\valora-web %%F in (*.jpg *.jpeg *.png *.ico *.webp *.gif *.bmp *.pdf *.zip *.docx *.xlsx *.pptx *.webm) do (echo Binario proibido %%F & exit /b 1)
echo Dry-run concluido. Nada foi publicado em producao.
exit /b 0
