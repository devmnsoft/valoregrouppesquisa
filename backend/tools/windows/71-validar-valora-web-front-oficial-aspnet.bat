@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/75] Subindo ambiente local live...
call npm run local:live:up
if errorlevel 1 goto erro

echo [2/75] Aplicando migrations...
call npm run local:migrations:apply
if errorlevel 1 goto erro

echo [3/75] Valora.Web ASP.NET only...
call npm run web:aspnet-only
if errorlevel 1 goto erro

echo [4/75] Build API...
call npm run backend:build
if errorlevel 1 goto erro

echo [5/75] Testes API...
call npm run backend:test
if errorlevel 1 goto erro

echo [6/75] Build Valora.Web...
call npm run web:build
if errorlevel 1 goto erro

echo [7/75] Projeto Valora.Web...
call npm run web:validate
if errorlevel 1 goto erro

echo [8/75] Sem binarios...
call npm run web:no-binaries
if errorlevel 1 goto erro

echo [9/75] Sem placeholders...
call npm run web:no-placeholders
if errorlevel 1 goto erro

echo [10/75] Sem admin generico...
call npm run web:no-generic-admin
if errorlevel 1 goto erro

echo [11/75] Gaps controlados...
call npm run web:controlled-gaps
if errorlevel 1 goto erro

echo [12/75] JS especifico por pagina...
call npm run web:page-specific-js
if errorlevel 1 goto erro

echo [13/75] MVC logging...
call npm run web:mvc-logging
if errorlevel 1 goto erro

echo [14/75] Formularios reais...
call npm run web:real-forms
if errorlevel 1 goto erro

echo [15/75] Uso real da API...
call npm run web:real-api-usage
if errorlevel 1 goto erro

echo [16/75] Area autenticada...
call npm run web:authenticated-modules
if errorlevel 1 goto erro

echo [17/75] API gaps CRUD...
call npm run web:crud-api-gaps
if errorlevel 1 goto erro

echo [18/75] Auth guards...
call npm run web:auth-guards
if errorlevel 1 goto erro

echo [19/75] Sem JSON bruto...
call npm run web:no-json-dump
if errorlevel 1 goto erro

echo [20/75] UX Admin...
call npm run web:admin-ux
if errorlevel 1 goto erro

echo [21/75] Endpoints API para Web...
call npm run web:api-endpoints
if errorlevel 1 goto erro

echo [22/75] Segurança Admin Web...
call npm run web:admin-security
if errorlevel 1 goto erro

echo [23/75] ResultToken seguro...
call npm run web:result-token-safety
if errorlevel 1 goto erro

echo [24/75] Certificado binario/fallback...
call npm run web:certificate-binary
if errorlevel 1 goto erro

echo [25/75] JQuery AJAX...
call npm run web:jquery
if errorlevel 1 goto erro

echo [26/75] Contrato API...
call npm run web:api-contract
if errorlevel 1 goto erro

echo [27/75] Telas...
call npm run web:screens
if errorlevel 1 goto erro

echo [28/75] Erros...
call npm run web:errors
if errorlevel 1 goto erro

echo [29/75] Mobile...
call npm run web:mobile
if errorlevel 1 goto erro

echo [30/75] CORS...
call npm run web:cors
if errorlevel 1 goto erro

echo [31/75] Docker Web...
call npm run web:docker
if errorlevel 1 goto erro

echo [32/75] Fluxo funcional Web...
call npm run web:functional-flow
if errorlevel 1 goto erro

echo [33/75] Release Web oficial...
call npm run web:official-release
if errorlevel 1 goto erro

echo [34/75] Health API...
call npm run backend:health
if errorlevel 1 goto erro

echo [35/75] API E2E...
call npm run api:e2e
if errorlevel 1 goto erro

echo [36/75] SaaS E2E...
call npm run prod:saas-e2e
if errorlevel 1 goto erro

echo [37/75] Web E2E...
call npm run web:e2e
if errorlevel 1 goto erro

echo [38/75] Frontend SaaS antigo...
call npm run prod:frontend-saas
if errorlevel 1 goto erro

echo [39/75] Security check...
call npm run security:check
if errorlevel 1 goto erro

echo [40/75] Logs sensiveis...
call npm run api:no-sensitive-logs
if errorlevel 1 goto erro

echo [41/75] Readiness...
call npm run prod:saas-readiness
if errorlevel 1 goto erro

echo [42/75] No legacy...
call npm run prod:no-legacy
if errorlevel 1 goto erro

echo [43/75] No pending...
call npm run prod:no-pending
if errorlevel 1 goto erro

echo [44/75] Billing...
call npm run prod:billing
if errorlevel 1 goto erro

echo [45/75] Email...
call npm run prod:email-flow
if errorlevel 1 goto erro

echo [46/75] Certificado...
call npm run prod:certificate-flow
if errorlevel 1 goto erro

echo [47/75] Performance...
call npm run prod:performance
if errorlevel 1 goto erro

echo [48/75] Security live...
call npm run prod:security-live
if errorlevel 1 goto erro

echo [49/75] Cutover dry run...
call npm run prod:cutover-dry-run
if errorlevel 1 goto erro

echo [50/75] Rollback ready...
call npm run prod:rollback-ready
if errorlevel 1 goto erro

echo [51/75] Docker build...
docker compose build
if errorlevel 1 goto erro

echo [52/75] Build producao...
call npm run build:prod
if errorlevel 1 goto erro

echo [53/75] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [54/75] Valora.Web csproj...
if not exist backend\Valora.Web\Valora.Web.csproj goto erro

echo [55/75] Valora.Web Layout...
if not exist backend\Valora.Web\Views\Shared\_Layout.cshtml goto erro

echo [56/75] Dashboard...
if not exist backend\Valora.Web\Views\Dashboard\Index.cshtml goto erro

echo [57/75] Organization...
if not exist backend\Valora.Web\Views\Organization\Index.cshtml goto erro

echo [58/75] Users...
if not exist backend\Valora.Web\Views\Users\Index.cshtml goto erro

echo [59/75] Forms...
if not exist backend\Valora.Web\Views\Forms\Index.cshtml goto erro

echo [60/75] Surveys...
if not exist backend\Valora.Web\Views\Surveys\Index.cshtml goto erro

echo [61/75] Responses...
if not exist backend\Valora.Web\Views\Responses\Index.cshtml goto erro

echo [62/75] Communications...
if not exist backend\Valora.Web\Views\Communications\Index.cshtml goto erro

echo [63/75] Audit...
if not exist backend\Valora.Web\Views\Audit\Index.cshtml goto erro

echo [64/75] Migration...
if not exist backend\Valora.Web\Views\Migration\Index.cshtml goto erro

echo [65/75] Settings...
if not exist backend\Valora.Web\Views\Settings\Index.cshtml goto erro

echo [66/75] API gaps...
if not exist ASPNET_WEB_API_GAPS.md goto erro

echo [67/75] Auditoria Sprint 38...
if not exist SPRINT_38_VALORA_WEB_OFFICIAL_FRONT_AUDIT.md goto erro

echo [68/75] Official Front doc...
if not exist ASPNET_WEB_OFFICIAL_FRONTEND.md goto erro

echo [69/75] Sem JSON bruto...
findstr /S /I /C:"JSON.stringify(result,null,2)" backend\Valora.Web\*.cshtml backend\Valora.Web\wwwroot\js\*.js && goto erro

echo [70/75] Sem texto generico...
findstr /S /I /C:"Executar ação" backend\Valora.Web\*.cshtml && goto erro

echo [71/75] Sem front Node oficial...
if exist frontend-web\server.js goto erro

echo [72/75] No binary policy...
if not exist ASPNET_WEB_NO_BINARY_ASSETS_POLICY.md goto erro

echo [73/75] Validation report...
if not exist ASPNET_WEB_VALIDATION_REPORT.md goto erro

echo [74/75] Encerrando ambiente...
call npm run local:live:down
if errorlevel 1 goto erro

echo [75/75] Valora.Web oficial ASP.NET validado.
echo VALORA.WEB ASP.NET FRONT OFICIAL DO SAAS VALIDADO.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DO VALORA.WEB FRONT OFICIAL ASP.NET.
call npm run local:live:down
pause
exit /b 1
