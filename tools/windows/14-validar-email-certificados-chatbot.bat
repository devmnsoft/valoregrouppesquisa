@echo off
cd /d C:\DBBACK\valoregrouppesquisa

call npm run check
if errorlevel 1 goto erro

node scripts\validate-email-runtime.js
if errorlevel 1 goto erro

node scripts\validate-certificates.js
if errorlevel 1 goto erro

node scripts\validate-chatbot-natural-language.js
if errorlevel 1 goto erro

call npm run build:prod
if errorlevel 1 goto erro

echo Validacao concluida.
pause
exit /b 0

:erro
echo A validacao encontrou erros.
pause
exit /b 1
