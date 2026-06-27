@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/65] Syntax...
call npm run check
if errorlevel 1 goto erro

echo [2/65] Security check...
call npm run security:check
if errorlevel 1 goto erro

echo [3/65] Data contracts...
call npm run validate:data-contracts
if errorlevel 1 goto erro

echo [4/65] Render resilience...
call npm run validate:render-resilience
if errorlevel 1 goto erro

echo [5/65] Data shape audit...
call npm run audit:data-shapes
if errorlevel 1 goto erro

echo [6/65] Frontend Bootstrap...
call npm run frontend:bootstrap
if errorlevel 1 goto erro

echo [7/65] Frontend API errors...
call npm run frontend:api-errors
if errorlevel 1 goto erro

echo [8/65] Frontend SaaS coverage...
call npm run prod:frontend-saas
if errorlevel 1 goto erro

echo [9/65] Error handling...
call npm run api:error-handling
if errorlevel 1 goto erro

echo [10/65] Logging...
call npm run api:logging
if errorlevel 1 goto erro

echo [11/65] Repository logging...
call npm run api:repository-logging
if errorlevel 1 goto erro

echo [12/65] Migration logging...
call npm run api:migration-logging
if errorlevel 1 goto erro

echo [13/65] Sensitive logs...
call npm run api:no-sensitive-logs
if errorlevel 1 goto erro

echo [14/65] Correlation...
call npm run api:correlation
if errorlevel 1 goto erro

echo [15/65] Transaction logging...
call npm run api:transaction-logging
if errorlevel 1 goto erro

echo [16/65] Email errors...
call npm run api:email-errors
if errorlevel 1 goto erro

echo [17/65] Health observability...
call npm run api:health-observability
if errorlevel 1 goto erro

echo [18/65] Provider API...
call npm run api:provider
if errorlevel 1 goto erro

echo [19/65] Public journey provider...
call npm run journey:provider
if errorlevel 1 goto erro

echo [20/65] Public API routes...
call npm run api:routes
if errorlevel 1 goto erro

echo [21/65] Public API real...
call npm run api:public-real
if errorlevel 1 goto erro

echo [22/65] Typed services...
call npm run api:typed-services
if errorlevel 1 goto erro

echo [23/65] Service size...
call npm run api:service-size
if errorlevel 1 goto erro

echo [24/65] Result token...
call npm run api:result-token
if errorlevel 1 goto erro

echo [25/65] Transactional submit...
call npm run api:transactional-submit
if errorlevel 1 goto erro

echo [26/65] Repository transaction...
call npm run api:repository-transaction
if errorlevel 1 goto erro

echo [27/65] Architecture warnings...
call npm run architecture:warnings
if errorlevel 1 goto erro

echo [28/65] Cutover readiness...
call npm run cutover:ready
if errorlevel 1 goto erro

echo [29/65] Backend implementation...
call npm run backend:implementation
if errorlevel 1 goto erro

echo [30/65] Backend clean...
call npm run backend:clean
if errorlevel 1 goto erro

echo [31/65] PostgreSQL schema...
call npm run postgres:schema
if errorlevel 1 goto erro

echo [32/65] Docker Windows runtime...
call npm run runtime:docker-windows
if errorlevel 1 goto erro

echo [33/65] SaaS readiness...
call npm run prod:saas-readiness
if errorlevel 1 goto erro

echo [34/65] No legacy...
call npm run prod:no-legacy
if errorlevel 1 goto erro

echo [35/65] No pending...
call npm run prod:no-pending
if errorlevel 1 goto erro

echo [36/65] SaaS features...
call npm run prod:saas-features
if errorlevel 1 goto erro

echo [37/65] Auth flow...
call npm run prod:auth-flow
if errorlevel 1 goto erro

echo [38/65] Certificate flow...
call npm run prod:certificate-flow
if errorlevel 1 goto erro

echo [39/65] Email flow...
call npm run prod:email-flow
if errorlevel 1 goto erro

echo [40/65] Billing...
call npm run prod:billing
if errorlevel 1 goto erro

echo [41/65] Security gate...
call npm run prod:security-gate
if errorlevel 1 goto erro

echo [42/65] PostgreSQL up...
call npm run postgres:up
if errorlevel 1 goto erro

echo [43/65] Backend build...
call npm run backend:build
if errorlevel 1 goto erro

echo [44/65] Backend tests...
call npm run backend:test
if errorlevel 1 goto erro

echo [45/65] Backend health...
call npm run backend:health
if errorlevel 1 goto erro

echo [46/65] PostgreSQL MVP...
call npm run postgres:mvp
if errorlevel 1 goto erro

echo [47/65] API E2E...
call npm run api:e2e
if errorlevel 1 goto erro

echo [48/65] SaaS E2E...
call npm run prod:saas-e2e
if errorlevel 1 goto erro

echo [49/65] Hybrid check...
call npm run hybrid:check
if errorlevel 1 goto erro

echo [50/65] Migration dry run...
call npm run migration:dry-run-real
if errorlevel 1 goto erro

echo [51/65] Migration validate...
call npm run migration:validate
if errorlevel 1 goto erro

echo [52/65] Migration compare...
call npm run migration:compare
if errorlevel 1 goto erro

echo [53/65] Cutover dry run...
call npm run prod:cutover-dry-run
if errorlevel 1 goto erro

echo [54/65] Rollback ready...
call npm run prod:rollback-ready
if errorlevel 1 goto erro

echo [55/65] Docker build...
docker compose build
if errorlevel 1 goto erro

echo [56/65] Build production...
call npm run build:prod
if errorlevel 1 goto erro

echo [57/65] Production health...
call npm run prod:health
if errorlevel 1 goto erro

echo [58/65] Final release gate...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [59/65] Release report exists...
if not exist RELEASE_CANDIDATE_REPORT.md goto erro

echo [60/65] Production checklist exists...
if not exist PRODUCTION_READINESS_CHECKLIST.md goto erro

echo [61/65] Cutover checklist exists...
if not exist PRODUCTION_CUTOVER_CHECKLIST.md goto erro

echo [62/65] Rollback checklist exists...
if not exist PRODUCTION_ROLLBACK_CHECKLIST.md goto erro

echo [63/65] Risk register exists...
if not exist BUG_RISK_REGISTER.md goto erro

echo [64/65] Known limitations exists...
if not exist KNOWN_LIMITATIONS_BEFORE_PRODUCTION.md goto erro

echo [65/65] Produto SaaS validado.
echo PRODUTO SAAS FINAL HOMOLOGAVEL.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO FINAL DO PRODUTO SAAS.
pause
exit /b 1
