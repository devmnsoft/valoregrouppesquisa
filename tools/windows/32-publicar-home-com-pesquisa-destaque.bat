@echo off
cd /d C:\DBBACK\valoregrouppesquisa
echo [1/6] Validando codigo...
call npm run check
if errorlevel 1 goto erro
echo [2/6] Validando pesquisa destaque...
node scripts\validate-featured-home-survey.js --project gestordepesquisa
if errorlevel 1 goto erro
echo [3/6] Build produção...
call npm run build:prod
if errorlevel 1 goto erro
echo [4/6] Publicando IIS...
robocopy C:\DBBACK\valoregrouppesquisa\dist C:\inetpub\wwwroot\valoragroup /MIR
if errorlevel 8 goto erro
copy /Y C:\DBBACK\valoregrouppesquisa\templates\iis\web.config C:\inetpub\wwwroot\valoragroup\web.config
echo [5/6] Reiniciando IIS...
iisreset
echo [6/6] Health check...
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro
echo PUBLICACAO CONCLUIDA.
pause
exit /b 0
:erro
echo FALHA NA PUBLICACAO.
pause
exit /b 1
