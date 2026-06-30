@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/35] Segurança sem segredos...
call npm run security:no-secrets
if errorlevel 1 goto erro

echo [2/35] Submit público legado...
call npm run legacy:public-submit-flow
if errorlevel 1 goto erro

echo [3/35] Functions submit público...
call npm run functions:public-submit
if errorlevel 1 goto erro

echo [4/35] Envio e-mail resultado...
call npm run legacy:result-email-send
if errorlevel 1 goto erro

echo [5/35] Certificado completo...
call npm run legacy:certificate-complete
if errorlevel 1 goto erro

echo [6/35] Aba Planos...
call npm run legacy:plans-tab
if errorlevel 1 goto erro

echo [7/35] Token gratuito sem expirar...
call npm run legacy:free-token-never-expires
if errorlevel 1 goto erro

echo [8/35] Menu mobile estrutural...
call npm run legacy:admin-mobile-functional
if errorlevel 1 goto erro

echo [9/35] Menu mobile runtime...
call npm run legacy:admin-mobile-runtime
if errorlevel 1 goto erro

echo [10/35] Diagnóstico completo...
call npm run legacy:diagnostics-complete
if errorlevel 1 goto erro

echo [11/35] Cache busting...
call npm run legacy:cache-busting
if errorlevel 1 goto erro

echo [12/35] E2E submit público...
call npm run e2e:legacy-public-submit
if errorlevel 1 goto erro

echo [13/35] E2E certificado...
call npm run e2e:legacy-certificate
if errorlevel 1 goto erro

echo [14/35] E2E planos...
call npm run e2e:legacy-plans
if errorlevel 1 goto erro

echo [15/35] E2E menu mobile...
call npm run e2e:legacy-admin-mobile
if errorlevel 1 goto erro

echo [16/35] Sem resposta demonstrativa...
call npm run email:no-demo-response
if errorlevel 1 goto erro

echo [17/35] Build backend...
call npm run backend:build
if errorlevel 1 goto erro

echo [18/35] Testes backend...
call npm run backend:test
if errorlevel 1 goto erro

echo [19/35] Web ASP.NET...
call npm run web:aspnet-only
if errorlevel 1 goto erro

echo [20/35] Build Web...
call npm run web:build
if errorlevel 1 goto erro

echo [21/35] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [22/35] Documento auditoria...
if not exist SPRINT_66_LEGACY_PUBLIC_FLOW_FULL_FIX_AUDIT.md goto erro

echo LEGADO FUNCIONAL VALIDADO.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DO LEGADO.
pause
exit /b 1
