@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/30] Segurança sem segredos...
call npm run security:no-secrets
if errorlevel 1 goto erro

echo [2/30] SMTP Google config...
call npm run email:google-smtp-config
if errorlevel 1 goto erro

echo [3/30] Functions e-mail resultado...
call npm run email:functions-result
if errorlevel 1 goto erro

echo [4/30] Fluxo e-mail legado...
call npm run legacy:result-email-flow
if errorlevel 1 goto erro

echo [5/30] Certificado legado...
call npm run legacy:certificate-flow
if errorlevel 1 goto erro

echo [6/30] Token gratuito sem expirar...
call npm run home:free-token-runtime
if errorlevel 1 goto erro

echo [7/30] Menu mobile final estrutural...
call npm run admin:mobile-final
if errorlevel 1 goto erro

echo [8/30] Menu mobile final runtime...
call npm run admin:mobile-final-runtime
if errorlevel 1 goto erro

echo [9/30] Cache busting final...
call npm run prod:cache-final
if errorlevel 1 goto erro

echo [10/30] Sem resposta demonstrativa...
call npm run email:no-demo-response
if errorlevel 1 goto erro

echo [11/30] Build backend...
call npm run backend:build
if errorlevel 1 goto erro

echo [12/30] Testes backend...
call npm run backend:test
if errorlevel 1 goto erro

echo [13/30] Web ASP.NET...
call npm run web:aspnet-only
if errorlevel 1 goto erro

echo [14/30] Build Web...
call npm run web:build
if errorlevel 1 goto erro

echo [15/30] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [16/30] Documento auditoria...
if not exist SPRINT_63_LEGACY_FULL_RUNTIME_FIX_AUDIT.md goto erro

echo EMAIL + CERTIFICADO + MENU + TOKEN VALIDADO.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO EMAIL + CERTIFICADO + MENU + TOKEN.
pause
exit /b 1
