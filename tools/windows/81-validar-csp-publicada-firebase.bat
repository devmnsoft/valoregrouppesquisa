@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/20] Validando CSP local...
call npm run security:csp
if errorlevel 1 goto erro

echo [2/20] Build produção...
call npm run build:prod
if errorlevel 1 goto erro

echo [3/20] Validando readiness Firebase...
call npm run deploy:firebase-readiness
if errorlevel 1 goto erro

echo [4/20] Validando CSP publicada...
set VALORA_PUBLIC_URL=https://valoragroup.mnsoft.com.br
call npm run security:csp-live
if errorlevel 1 goto erro

echo [5/20] Smoke produção...
set VALORA_API_URL=https://api.valoragroup.mnsoft.com.br
call npm run prod:smoke
if errorlevel 1 goto erro

echo CSP PUBLICADA VALIDADA COM SUCESSO.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DA CSP PUBLICADA.
pause
exit /b 1
