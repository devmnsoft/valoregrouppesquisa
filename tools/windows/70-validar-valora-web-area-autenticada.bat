@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/65] Subindo ambiente local live...
call npm run local:live:up
if errorlevel 1 goto erro

echo [2/65] Aplicando migrations...
call npm run local:migrations:apply
if errorlevel 1 goto erro

echo [3/65] Build API...
call npm run backend:build
if errorlevel 1 goto erro

echo [4/65] Testes API...
call npm run backend:test
if errorlevel 1 goto erro

echo [5/65] Build Valora.Web...
call npm run web:build
if errorlevel 1 goto erro

echo [6/65] Projeto Valora.Web...
call npm run web:validate
if errorlevel 1 goto erro

echo [7/65] Sem binarios...
call npm run web:no-binaries
if errorlevel 1 goto erro

echo [8/65] Sem placeholders...
call npm run web:no-placeholders
if errorlevel 1 goto erro

echo [9/65] Formularios reais...
call npm run web:real-forms
if errorlevel 1 goto erro

echo [10/65] Uso real da API...
call npm run web:real-api-usage
if errorlevel 1 goto erro

echo [11/65] Area autenticada...
call npm run web:authenticated-modules
if errorlevel 1 goto erro

echo [12/65] API gaps CRUD...
call npm run web:crud-api-gaps
if errorlevel 1 goto erro

echo [13/65] Auth guards...
call npm run web:auth-guards
if errorlevel 1 goto erro

echo [14/65] Sem JSON bruto...
call npm run web:no-json-dump
if errorlevel 1 goto erro

echo [15/65] UX Admin...
call npm run web:admin-ux
if errorlevel 1 goto erro

echo [16/65] Endpoints API para Web...
call npm run web:api-endpoints
if errorlevel 1 goto erro

echo [17/65] Segurança Admin Web...
call npm run web:admin-security
if errorlevel 1 goto erro

echo [18/65] ResultToken seguro...
call npm run web:result-token-safety
if errorlevel 1 goto erro

echo [19/65] Certificado binario/fallback...
call npm run web:certificate-binary
if errorlevel 1 goto erro

echo [20/65] JQuery AJAX...
call npm run web:jquery
if errorlevel 1 goto erro

echo [21/65] Contrato API...
call npm run web:api-contract
if errorlevel 1 goto erro

echo [22/65] Telas...
call npm run web:screens
if errorlevel 1 goto erro

echo [23/65] Erros...
call npm run web:errors
if errorlevel 1 goto erro

echo [24/65] Mobile...
call npm run web:mobile
if errorlevel 1 goto erro

echo [25/65] CORS...
call npm run web:cors
if errorlevel 1 goto erro

echo [26/65] Docker Web...
call npm run web:docker
if errorlevel 1 goto erro

echo [27/65] Fluxo funcional Web...
call npm run web:functional-flow
if errorlevel 1 goto erro

echo [28/65] Health API...
call npm run backend:health
if errorlevel 1 goto erro

echo [29/65] API E2E...
call npm run api:e2e
if errorlevel 1 goto erro

echo [30/65] SaaS E2E...
call npm run prod:saas-e2e
if errorlevel 1 goto erro

echo [31/65] Web E2E...
call npm run web:e2e
if errorlevel 1 goto erro

echo [32/65] Frontend SaaS antigo...
call npm run prod:frontend-saas
if errorlevel 1 goto erro

echo [33/65] Security check...
call npm run security:check
if errorlevel 1 goto erro

echo [34/65] Logs sensiveis...
call npm run api:no-sensitive-logs
if errorlevel 1 goto erro

echo [35/65] Readiness...
call npm run prod:saas-readiness
if errorlevel 1 goto erro

echo [36/65] No legacy...
call npm run prod:no-legacy
if errorlevel 1 goto erro

echo [37/65] No pending...
call npm run prod:no-pending
if errorlevel 1 goto erro

echo [38/65] Billing...
call npm run prod:billing
if errorlevel 1 goto erro

echo [39/65] Email...
call npm run prod:email-flow
if errorlevel 1 goto erro

echo [40/65] Certificado...
call npm run prod:certificate-flow
if errorlevel 1 goto erro

echo [41/65] Performance...
call npm run prod:performance
if errorlevel 1 goto erro

echo [42/65] Security live...
call npm run prod:security-live
if errorlevel 1 goto erro

echo [43/65] Cutover dry run...
call npm run prod:cutover-dry-run
if errorlevel 1 goto erro

echo [44/65] Rollback ready...
call npm run prod:rollback-ready
if errorlevel 1 goto erro

echo [45/65] Docker build...
docker compose build
if errorlevel 1 goto erro

echo [46/65] Build producao...
call npm run build:prod
if errorlevel 1 goto erro

echo [47/65] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [48/65] View Dashboard existe...
if not exist backend\Valora.Web\Views\Dashboard\Index.cshtml goto erro

echo [49/65] View Users existe...
if not exist backend\Valora.Web\Views\Users\Index.cshtml goto erro

echo [50/65] View Surveys existe...
if not exist backend\Valora.Web\Views\Surveys\Index.cshtml goto erro

echo [51/65] View Responses existe...
if not exist backend\Valora.Web\Views\Responses\Index.cshtml goto erro

echo [52/65] Users API JS existe...
if not exist backend\Valora.Web\wwwroot\js\api\users-api.js goto erro

echo [53/65] Organization API JS existe...
if not exist backend\Valora.Web\wwwroot\js\api\organization-api.js goto erro

echo [54/65] Public Links API JS existe...
if not exist backend\Valora.Web\wwwroot\js\api\public-links-api.js goto erro

echo [55/65] API gaps existe...
if not exist ASPNET_WEB_API_GAPS.md goto erro

echo [56/65] Auditoria Sprint 37 existe...
if not exist SPRINT_37_VALORA_WEB_AUTHENTICATED_AREA_AUDIT.md goto erro

echo [57/65] Validation report existe...
if not exist ASPNET_WEB_VALIDATION_REPORT.md goto erro

echo [58/65] Sem JSON bruto no Valora.Web...
findstr /S /I /C:"JSON.stringify(result,null,2)" backend\Valora.Web\*.cshtml backend\Valora.Web\wwwroot\js\*.js && goto erro

echo [59/65] Sem texto generico...
findstr /S /I /C:"Executar ação" backend\Valora.Web\*.cshtml && goto erro

echo [60/65] Sem modulo em ativacao solto...
findstr /S /I /C:"módulo em ativação" backend\Valora.Web\*.cshtml backend\Valora.Web\wwwroot\js\*.js && goto erro

echo [61/65] Documentação area autenticada...
if not exist ASPNET_WEB_AUTHENTICATED_MODULES.md goto erro

echo [62/65] Checklist UX Admin...
if not exist ASPNET_WEB_ADMIN_UX_CHECKLIST.md goto erro

echo [63/65] No binary policy...
if not exist ASPNET_WEB_NO_BINARY_ASSETS_POLICY.md goto erro

echo [64/65] Encerrando ambiente...
call npm run local:live:down
if errorlevel 1 goto erro

echo [65/65] Area autenticada validada.
echo VALORA.WEB AREA AUTENTICADA SAAS VALIDADA.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DA AREA AUTENTICADA VALORA.WEB.
call npm run local:live:down
pause
exit /b 1
