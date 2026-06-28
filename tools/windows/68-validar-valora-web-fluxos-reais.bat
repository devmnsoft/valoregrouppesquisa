@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/45] Subindo ambiente local live...
call npm run local:live:up
if errorlevel 1 goto erro

echo [2/45] Aplicando migrations...
call npm run local:migrations:apply
if errorlevel 1 goto erro

echo [3/45] Build API...
call npm run backend:build
if errorlevel 1 goto erro

echo [4/45] Testes API...
call npm run backend:test
if errorlevel 1 goto erro

echo [5/45] Build Valora.Web...
call npm run web:build
if errorlevel 1 goto erro

echo [6/45] Projeto Valora.Web...
call npm run web:validate
if errorlevel 1 goto erro

echo [7/45] Sem binarios...
call npm run web:no-binaries
if errorlevel 1 goto erro

echo [8/45] JQuery AJAX...
call npm run web:jquery
if errorlevel 1 goto erro

echo [9/45] Contrato API...
call npm run web:api-contract
if errorlevel 1 goto erro

echo [10/45] Telas...
call npm run web:screens
if errorlevel 1 goto erro

echo [11/45] Erros...
call npm run web:errors
if errorlevel 1 goto erro

echo [12/45] Mobile...
call npm run web:mobile
if errorlevel 1 goto erro

echo [13/45] CORS...
call npm run web:cors
if errorlevel 1 goto erro

echo [14/45] Docker Web...
call npm run web:docker
if errorlevel 1 goto erro

echo [15/45] Fluxo funcional Web...
call npm run web:functional-flow
if errorlevel 1 goto erro

echo [16/45] Health API...
call npm run backend:health
if errorlevel 1 goto erro

echo [17/45] API E2E...
call npm run api:e2e
if errorlevel 1 goto erro

echo [18/45] SaaS E2E...
call npm run prod:saas-e2e
if errorlevel 1 goto erro

echo [19/45] Web E2E...
call npm run web:e2e
if errorlevel 1 goto erro

echo [20/45] Frontend SaaS antigo...
call npm run prod:frontend-saas
if errorlevel 1 goto erro

echo [21/45] Security check...
call npm run security:check
if errorlevel 1 goto erro

echo [22/45] Logs sensiveis...
call npm run api:no-sensitive-logs
if errorlevel 1 goto erro

echo [23/45] Readiness...
call npm run prod:saas-readiness
if errorlevel 1 goto erro

echo [24/45] No legacy...
call npm run prod:no-legacy
if errorlevel 1 goto erro

echo [25/45] No pending...
call npm run prod:no-pending
if errorlevel 1 goto erro

echo [26/45] Billing...
call npm run prod:billing
if errorlevel 1 goto erro

echo [27/45] Email...
call npm run prod:email-flow
if errorlevel 1 goto erro

echo [28/45] Certificado...
call npm run prod:certificate-flow
if errorlevel 1 goto erro

echo [29/45] Performance...
call npm run prod:performance
if errorlevel 1 goto erro

echo [30/45] Security live...
call npm run prod:security-live
if errorlevel 1 goto erro

echo [31/45] Cutover dry run...
call npm run prod:cutover-dry-run
if errorlevel 1 goto erro

echo [32/45] Rollback ready...
call npm run prod:rollback-ready
if errorlevel 1 goto erro

echo [33/45] Docker build...
docker compose build
if errorlevel 1 goto erro

echo [34/45] Build producao...
call npm run build:prod
if errorlevel 1 goto erro

echo [35/45] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [36/45] Valora.Web csproj existe...
if not exist backend\Valora.Web\Valora.Web.csproj goto erro

echo [37/45] Layout existe...
if not exist backend\Valora.Web\Views\Shared\_Layout.cshtml goto erro

echo [38/45] AJAX client existe...
if not exist backend\Valora.Web\wwwroot\js\api\ajax-client.js goto erro

echo [39/45] App CSS existe...
if not exist backend\Valora.Web\wwwroot\css\app.css goto erro

echo [40/45] Relatorio web existe...
if not exist ASPNET_WEB_VALIDATION_REPORT.md goto erro

echo [41/45] No binary policy existe...
if not exist ASPNET_WEB_NO_BINARY_ASSETS_POLICY.md goto erro

echo [42/45] Web architecture existe...
if not exist ASPNET_WEB_FRONTEND_ARCHITECTURE.md goto erro

echo [43/45] Web API gaps existe...
if not exist ASPNET_WEB_API_GAPS.md goto erro

echo [44/45] Encerrando ambiente...
call npm run local:live:down
if errorlevel 1 goto erro

echo [45/45] Valora.Web validado com fluxos reais.
echo VALORA.WEB ASP.NET API-FIRST HOMOLOGADO.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DO VALORA.WEB.
call npm run local:live:down
pause
exit /b 1
