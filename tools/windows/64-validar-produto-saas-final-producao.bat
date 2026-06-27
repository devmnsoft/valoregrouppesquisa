@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/55] Syntax...
call npm run check
if errorlevel 1 goto erro

echo [2/55] Frontend Bootstrap...
call npm run frontend:bootstrap
if errorlevel 1 goto erro

echo [3/55] Frontend API errors...
call npm run frontend:api-errors
if errorlevel 1 goto erro

echo [4/55] Security check...
call npm run security:check
if errorlevel 1 goto erro

echo [5/55] Data contracts...
call npm run validate:data-contracts
if errorlevel 1 goto erro

echo [6/55] Render resilience...
call npm run validate:render-resilience
if errorlevel 1 goto erro

echo [7/55] Data shape audit...
call npm run audit:data-shapes
if errorlevel 1 goto erro

echo [8/55] No fake comments...
call npm run api:no-fake-validator-comments
if errorlevel 1 goto erro

echo [9/55] Error handling...
call npm run api:error-handling
if errorlevel 1 goto erro

echo [10/55] Logging coverage...
call npm run api:logging
if errorlevel 1 goto erro

echo [11/55] Repository logging...
call npm run api:repository-logging
if errorlevel 1 goto erro

echo [12/55] Migration logging...
call npm run api:migration-logging
if errorlevel 1 goto erro

echo [13/55] Sensitive logs...
call npm run api:no-sensitive-logs
if errorlevel 1 goto erro

echo [14/55] Correlation...
call npm run api:correlation
if errorlevel 1 goto erro

echo [15/55] Transaction logging...
call npm run api:transaction-logging
if errorlevel 1 goto erro

echo [16/55] Email errors...
call npm run api:email-errors
if errorlevel 1 goto erro

echo [17/55] Health observability...
call npm run api:health-observability
if errorlevel 1 goto erro

echo [18/55] API provider...
call npm run api:provider
if errorlevel 1 goto erro

echo [19/55] Public journey provider...
call npm run journey:provider
if errorlevel 1 goto erro

echo [20/55] Public API routes...
call npm run api:routes
if errorlevel 1 goto erro

echo [21/55] Public API real flow...
call npm run api:public-real
if errorlevel 1 goto erro

echo [22/55] Typed services...
call npm run api:typed-services
if errorlevel 1 goto erro

echo [23/55] Service size...
call npm run api:service-size
if errorlevel 1 goto erro

echo [24/55] Result token...
call npm run api:result-token
if errorlevel 1 goto erro

echo [25/55] Transactional submit...
call npm run api:transactional-submit
if errorlevel 1 goto erro

echo [26/55] Repository transaction...
call npm run api:repository-transaction
if errorlevel 1 goto erro

echo [27/55] Architecture warnings...
call npm run architecture:warnings
if errorlevel 1 goto erro

echo [28/55] Cutover readiness...
call npm run cutover:ready
if errorlevel 1 goto erro

echo [29/55] Backend implementation...
call npm run backend:implementation
if errorlevel 1 goto erro

echo [30/55] Backend clean...
call npm run backend:clean
if errorlevel 1 goto erro

echo [31/55] PostgreSQL schema...
call npm run postgres:schema
if errorlevel 1 goto erro

echo [32/55] Docker Windows runtime...
call npm run runtime:docker-windows
if errorlevel 1 goto erro

echo [33/55] SaaS production readiness...
node scripts\validate-saas-production-readiness.js
if errorlevel 1 goto erro

echo [34/55] No legacy production routes...
node scripts\validate-no-legacy-production-routes.js
if errorlevel 1 goto erro

echo [35/55] No pending production features...
node scripts\validate-no-pending-production-features.js
if errorlevel 1 goto erro

echo [36/55] SaaS feature coverage...
node scripts\validate-saas-feature-coverage.js
if errorlevel 1 goto erro

echo [37/55] Auth production flow...
node scripts\validate-auth-production-flow.js
if errorlevel 1 goto erro

echo [38/55] Certificate production flow...
node scripts\validate-certificate-production-flow.js
if errorlevel 1 goto erro

echo [39/55] Email production flow...
node scripts\validate-email-production-flow.js
if errorlevel 1 goto erro

echo [40/55] Billing entitlements...
node scripts\validate-billing-entitlements.js
if errorlevel 1 goto erro

echo [41/55] Security production gate...
node scripts\validate-security-production-gate.js
if errorlevel 1 goto erro

echo [42/55] PostgreSQL up...
call npm run postgres:up
if errorlevel 1 goto erro

echo [43/55] Backend build...
call npm run backend:build
if errorlevel 1 goto erro

echo [44/55] Backend tests...
call npm run backend:test
if errorlevel 1 goto erro

echo [45/55] Backend health...
call npm run backend:health
if errorlevel 1 goto erro

echo [46/55] PostgreSQL MVP...
call npm run postgres:mvp
if errorlevel 1 goto erro

echo [47/55] API E2E...
call npm run api:e2e
if errorlevel 1 goto erro

echo [48/55] Hybrid check...
call npm run hybrid:check
if errorlevel 1 goto erro

echo [49/55] Migration dry run...
call npm run migration:dry-run-real
if errorlevel 1 goto erro

echo [50/55] Migration validate...
call npm run migration:validate
if errorlevel 1 goto erro

echo [51/55] Migration compare...
call npm run migration:compare
if errorlevel 1 goto erro

echo [52/55] Docker build...
docker compose build
if errorlevel 1 goto erro

echo [53/55] Frontend build production...
call npm run build:prod
if errorlevel 1 goto erro

echo [54/55] Production health...
call npm run prod:health
if errorlevel 1 goto erro

echo [55/55] Release candidate gate...
node scripts\validate-release-candidate.js
if errorlevel 1 goto erro

echo PRODUTO SAAS FINAL VALIDADO PARA HOMOLOGACAO DE PRODUCAO.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DO PRODUTO SAAS FINAL.
pause
exit /b 1
