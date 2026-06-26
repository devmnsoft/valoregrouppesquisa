@echo off
cd /d C:\DBBACK\valoregrouppesquisa
call tools\windows\37-validar-gateway-jornada-publica.bat
if errorlevel 1 goto erro
call npm run build:prod
if errorlevel 1 goto erro
xcopy /E /I /Y dist C:\inetpub\wwwroot\valoragroup\
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa --check-public-survey
if errorlevel 1 goto erro
echo PUBLICAÇÃO CONCLUÍDA.
pause
exit /b 0
:erro
echo PUBLICAÇÃO FALHOU.
pause
exit /b 1
