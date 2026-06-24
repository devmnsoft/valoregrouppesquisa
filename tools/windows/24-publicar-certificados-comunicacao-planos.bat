@echo off
cd /d C:\DBBACK\valoregrouppesquisa

call tools\windows\23-validar-certificados-comunicacao-planos.bat
if errorlevel 1 goto erro

echo Publicando no IIS...
robocopy C:\DBBACK\valoregrouppesquisa\dist C:\inetpub\wwwroot\valoragroup /MIR
if errorlevel 8 goto erro

copy /Y C:\DBBACK\valoregrouppesquisa\templates\iis\web.config C:\inetpub\wwwroot\valoragroup\web.config

iisreset

node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro

echo PUBLICACAO CONCLUIDA.
pause
exit /b 0

:erro
echo PUBLICACAO FALHOU.
pause
exit /b 1
