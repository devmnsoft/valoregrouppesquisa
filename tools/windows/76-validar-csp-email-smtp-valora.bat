@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/45] Build backend...
call npm run backend:build
if errorlevel 1 goto erro

echo [2/45] Testes backend...
call npm run backend:test
if errorlevel 1 goto erro

echo [3/45] CSP production...
call npm run security:csp
if errorlevel 1 goto erro

echo [4/45] Contrato comunicação...
call npm run api:communication-contract
if errorlevel 1 goto erro

echo [5/45] SMTP config...
call npm run email:smtp-config
if errorlevel 1 goto erro

echo [6/45] SMTP sender real...
call npm run email:real-sender
if errorlevel 1 goto erro

echo [7/45] Email jobs schema...
call npm run email:jobs-schema
if errorlevel 1 goto erro

echo [8/45] Sem placeholder provider...
call npm run email:no-placeholder-provider
if errorlevel 1 goto erro

echo [9/45] Segurança endpoints email...
call npm run email:endpoints-security
if errorlevel 1 goto erro

echo [10/45] Fluxo web email...
call npm run email:web-flow
if errorlevel 1 goto erro

echo [11/45] Swagger sem conflito...
call npm run api:swagger-health
if errorlevel 1 goto erro

echo [12/45] Sem rotas duplicadas...
call npm run api:no-route-conflicts
if errorlevel 1 goto erro

echo [13/45] Web ASP.NET...
call npm run web:aspnet-only
if errorlevel 1 goto erro

echo [14/45] Web build...
call npm run web:build
if errorlevel 1 goto erro

echo [15/45] Web e2e...
call npm run web:e2e
if errorlevel 1 goto erro

echo [16/45] SaaS e2e...
call npm run prod:saas-e2e
if errorlevel 1 goto erro

echo [17/45] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [18/45] Documento SMTP...
if not exist EMAIL_SMTP_CONFIGURATION.md goto erro

echo [19/45] Documento operação email...
if not exist EMAIL_DELIVERY_OPERATION.md goto erro

echo [20/45] Documento CSP...
if not exist CSP_PRODUCTION_POLICY.md goto erro

echo [21/45] Auditoria sprint 44...
if not exist SPRINT_44_CSP_EMAIL_SMTP_AUDIT.md goto erro

echo CSP + EMAIL SMTP VALORA VALIDADO.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DE CSP/EMAIL SMTP.
pause
exit /b 1
