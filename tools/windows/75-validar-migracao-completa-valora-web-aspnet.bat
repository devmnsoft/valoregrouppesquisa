@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/80] Subindo ambiente local live...
call npm run local:live:up
if errorlevel 1 goto erro

echo [2/80] Aplicando migrations...
call npm run local:migrations:apply
if errorlevel 1 goto erro

echo [3/80] Build API...
call npm run backend:build
if errorlevel 1 goto erro

echo [4/80] Testes API...
call npm run backend:test
if errorlevel 1 goto erro

echo [5/80] Sem conflito de rotas...
call npm run api:no-route-conflicts
if errorlevel 1 goto erro

echo [6/80] Swagger health...
call npm run api:swagger-health
if errorlevel 1 goto erro

echo [7/80] Valora.Web ASP.NET only...
call npm run web:aspnet-only
if errorlevel 1 goto erro

echo [8/80] Build Valora.Web...
call npm run web:build
if errorlevel 1 goto erro

echo [9/80] Sem acesso direto a dados...
call npm run web:no-data-access
if errorlevel 1 goto erro

echo [10/80] Sem binarios...
call npm run web:no-binaries
if errorlevel 1 goto erro

echo [11/80] Paridade de permissões...
call npm run web:permission-parity
if errorlevel 1 goto erro

echo [12/80] Paridade de módulos...
call npm run web:module-parity
if errorlevel 1 goto erro

echo [13/80] Paridade de jornada...
call npm run web:journey-parity
if errorlevel 1 goto erro

echo [14/80] Paridade visual...
call npm run web:ui-parity
if errorlevel 1 goto erro

echo [15/80] Sem dados fake admin...
call npm run web:no-fake-admin-data
if errorlevel 1 goto erro

echo [16/80] Regras de negócio...
call npm run web:business-rules
if errorlevel 1 goto erro

echo [17/80] Sem placeholders...
call npm run web:no-placeholders
if errorlevel 1 goto erro

echo [18/80] Sem render oficial generico...
call npm run web:no-generic-official-render
if errorlevel 1 goto erro

echo [19/80] Renderizadores especificos...
call npm run web:specific-renderers
if errorlevel 1 goto erro

echo [20/80] Gaps controlados...
call npm run web:controlled-gaps
if errorlevel 1 goto erro

echo [21/80] Gaps de producao...
call npm run web:production-gaps
if errorlevel 1 goto erro

echo [22/80] MVC logging...
call npm run web:mvc-logging
if errorlevel 1 goto erro

echo [23/80] Formularios reais...
call npm run web:real-forms
if errorlevel 1 goto erro

echo [24/80] Uso real da API...
call npm run web:real-api-usage
if errorlevel 1 goto erro

echo [25/80] Area autenticada...
call npm run web:authenticated-modules
if errorlevel 1 goto erro

echo [26/80] Auth guards...
call npm run web:auth-guards
if errorlevel 1 goto erro

echo [27/80] Sem JSON bruto...
call npm run web:no-json-dump
if errorlevel 1 goto erro

echo [28/80] Segurança Admin Web...
call npm run web:admin-security
if errorlevel 1 goto erro

echo [29/80] Sem dados sensiveis na UI...
call npm run web:no-sensitive-ui
if errorlevel 1 goto erro

echo [30/80] ResultToken seguro...
call npm run web:result-token-safety
if errorlevel 1 goto erro

echo [31/80] Certificado binario/fallback...
call npm run web:certificate-binary
if errorlevel 1 goto erro

echo [32/80] JQuery AJAX...
call npm run web:jquery
if errorlevel 1 goto erro

echo [33/80] Mobile...
call npm run web:mobile
if errorlevel 1 goto erro

echo [34/80] CORS...
call npm run web:cors
if errorlevel 1 goto erro

echo [35/80] Config dinamica...
call npm run web:dynamic-config
if errorlevel 1 goto erro

echo [36/80] Health Web...
call npm run web:health
if errorlevel 1 goto erro

echo [37/80] IIS readiness...
call npm run web:iis-readiness
if errorlevel 1 goto erro

echo [38/80] Docker runtime...
call npm run web:docker-runtime
if errorlevel 1 goto erro

echo [39/80] E2E API...
call npm run api:e2e
if errorlevel 1 goto erro

echo [40/80] E2E SaaS...
call npm run prod:saas-e2e
if errorlevel 1 goto erro

echo [41/80] E2E Web...
call npm run web:e2e
if errorlevel 1 goto erro

echo [42/80] Smoke produção...
call npm run prod:smoke
if errorlevel 1 goto erro

echo [43/80] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [44/80] Documento auditoria sprint 43...
if not exist SPRINT_43_VALORA_WEB_MIGRATION_PARITY_AUDIT.md goto erro

echo [45/80] Documento paridade UI...
if not exist ASPNET_WEB_UI_PARITY_WITH_ROOT_INDEX.md goto erro

echo [46/80] Documento gaps...
if not exist ASPNET_WEB_API_GAPS.md goto erro

echo [47/80] Sem conflito Swagger manual...
curl -k https://localhost:51844/swagger/v1/swagger.json > nul
if errorlevel 1 goto erro

echo [48/80] Encerrando ambiente...
call npm run local:live:down
if errorlevel 1 goto erro

echo MIGRACAO COMPLETA VALORA.WEB ASP.NET VALIDADA.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DA MIGRACAO COMPLETA VALORA.WEB ASP.NET.
call npm run local:live:down
pause
exit /b 1
