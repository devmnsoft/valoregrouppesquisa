@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/35] Build backend...
call npm run backend:build
if errorlevel 1 goto erro

echo [2/35] Testes backend...
call npm run backend:test
if errorlevel 1 goto erro

echo [3/35] CSP local...
call npm run security:csp
if errorlevel 1 goto erro

echo [4/35] Build produção...
call npm run build:prod
if errorlevel 1 goto erro

echo [5/35] Firebase readiness...
call npm run deploy:firebase-readiness
if errorlevel 1 goto erro

echo [6/35] CSP publicada...
set VALORA_PUBLIC_URL=https://valoragroup.mnsoft.com.br
call npm run security:csp-live
if errorlevel 1 goto erro

echo [7/35] Sem IDs demo...
call npm run email:no-demo-response
if errorlevel 1 goto erro

echo [8/35] Fluxo responseId real...
call npm run email:real-response-flow
if errorlevel 1 goto erro

echo [9/35] Fluxo web de email...
call npm run email:web-flow
if errorlevel 1 goto erro

echo [10/35] Resultado por email obrigatório...
call npm run home:result-email-required
if errorlevel 1 goto erro

echo [11/35] SMTP real...
call npm run email:real-sender
if errorlevel 1 goto erro

echo [12/35] Sem provider placeholder...
call npm run email:no-placeholder-provider
if errorlevel 1 goto erro

echo [13/35] Smoke produção...
set VALORA_API_URL=https://api.valoragroup.mnsoft.com.br
call npm run prod:smoke
if errorlevel 1 goto erro

echo [14/35] E2E CSP e email live...
call npm run e2e:csp-email-live
if errorlevel 1 goto erro

echo [15/35] Web ASP.NET...
call npm run web:aspnet-only
if errorlevel 1 goto erro

echo [16/35] Web build...
call npm run web:build
if errorlevel 1 goto erro

echo [17/35] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [18/35] Documento CSP live...
if not exist CSP_LIVE_DEPLOYMENT_VALIDATION.md goto erro

echo [19/35] Documento Firebase headers...
if not exist FIREBASE_HOSTING_DEPLOYMENT_HEADERS.md goto erro

echo [20/35] Documento no demo response...
if not exist NO_DEMO_RESPONSE_POLICY.md goto erro

echo CSP LIVE + EMAIL REAL VALIDADO.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DE CSP LIVE + EMAIL REAL.
pause
exit /b 1
