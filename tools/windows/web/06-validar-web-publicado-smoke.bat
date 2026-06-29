@echo off
setlocal
cd /d %~dp0\..\..\..
if "%VALORA_WEB_URL%"=="" set VALORA_WEB_URL=http://localhost:5088
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; $u=$env:VALORA_WEB_URL.TrimEnd('/'); '/health/web','/health/web/version','/Account/Login','/Dashboard' | %% { $r=Invoke-WebRequest -UseBasicParsing ($u+$_); if($r.StatusCode -ge 500){ throw $_+' retornou '+$r.StatusCode }; if($r.Content -match 'StackTrace|password_hash|token_hash|private_key'){ throw $_+' expos conteudo sensivel' } }"
if errorlevel 1 exit /b 1
echo Smoke publicado Valora.Web OK
exit /b 0
