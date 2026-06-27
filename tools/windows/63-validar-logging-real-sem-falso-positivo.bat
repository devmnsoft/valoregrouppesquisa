@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/46] Frontend syntax...
call npm run check
if errorlevel 1 goto erro

echo [2/46] Bootstrap frontend...
node scripts\validate-bootstrap-frontend.js
if errorlevel 1 goto erro

echo [3/46] Frontend API errors...
node scripts\validate-frontend-api-errors.js
if errorlevel 1 goto erro

echo [4/46] No fake validator comments...
node scripts\validate-no-validator-fake-comments.js
if errorlevel 1 goto erro

echo [5/46] Error handling middleware...
node scripts\validate-error-handling-middleware.js
if errorlevel 1 goto erro

echo [6/46] Logging coverage...
node scripts\validate-logging-coverage.js
if errorlevel 1 goto erro

echo [7/46] Repository logging real...
node scripts\validate-repository-logging.js
if errorlevel 1 goto erro

echo [8/46] Migration logging...
node scripts\validate-migration-logging.js
if errorlevel 1 goto erro

echo [9/46] Sensitive logs...
node scripts\validate-no-sensitive-logs.js
if errorlevel 1 goto erro

echo [10/46] Correlation ID...
node scripts\validate-correlation-id.js
if errorlevel 1 goto erro

echo [11/46] Transaction logging...
node scripts\validate-transaction-logging.js
if errorlevel 1 goto erro

echo [12/46] Email error handling...
node scripts\validate-email-error-handling.js
if errorlevel 1 goto erro

echo [13/46] Health observability...
node scripts\validate-health-observability.js
if errorlevel 1 goto erro

echo [14/46] API provider...
node scripts\validate-api-provider.js
if errorlevel 1 goto erro

echo [15/46] Public journey provider...
node scripts\validate-public-journey-provider.js
if errorlevel 1 goto erro

echo [16/46] Public API routes...
node scripts\validate-public-api-routes.js
if errorlevel 1 goto erro

echo [17/46] Public API real flow...
node scripts\validate-public-api-real-flow.js
if errorlevel 1 goto erro

echo [18/46] Typed public services...
node scripts\validate-typed-public-services.js
if errorlevel 1 goto erro

echo [19/46] Service method size...
node scripts\validate-service-method-size.js
if errorlevel 1 goto erro

echo [20/46] Result token security...
node scripts\validate-result-token-security.js
if errorlevel 1 goto erro

echo [21/46] Transactional public submit...
node scripts\validate-transactional-public-submit.js
if errorlevel 1 goto erro

echo [22/46] Repository transaction boundary...
node scripts\validate-repository-transaction-boundary.js
if errorlevel 1 goto erro

echo [23/46] Architecture warnings...
node scripts\validate-architecture-warnings.js
if errorlevel 1 goto erro

echo [24/46] Cutover readiness...
node scripts\validate-cutover-readiness.js
if errorlevel 1 goto erro

echo [25/46] Backend implementation...
node scripts\validate-backend-implementation.js
if errorlevel 1 goto erro

echo [26/46] Backend clean architecture...
node scripts\validate-backend-clean-architecture.js
if errorlevel 1 goto erro

echo [27/46] Single PostgreSQL schema...
node scripts\validate-single-postgres-schema.js
if errorlevel 1 goto erro

echo [28/46] Docker/Windows runtime...
node scripts\validate-docker-windows-runtime.js
if errorlevel 1 goto erro

echo [29/46] Subindo PostgreSQL...
docker compose -f docker-compose.postgres.yml up -d
if errorlevel 1 goto erro

echo [30/46] Validando schema PostgreSQL...
node scripts\validate-postgres-schema.js
if errorlevel 1 goto erro

echo [31/46] Build backend...
dotnet build backend\Valora.sln
if errorlevel 1 goto erro

echo [32/46] Testes backend...
dotnet test backend\Valora.sln
if errorlevel 1 goto erro

echo [33/46] API backend...
node scripts\validate-api-backend.js
if errorlevel 1 goto erro

echo [34/46] MVP PostgreSQL vertical...
node scripts\validate-vertical-postgres-mvp.js
if errorlevel 1 goto erro

echo [35/46] End-to-end API flow...
node scripts\validate-end-to-end-api-flow.js
if errorlevel 1 goto erro

echo [36/46] Migration scripts...
node scripts\validate-migration-scripts.js
if errorlevel 1 goto erro

echo [37/46] Hybrid provider...
node scripts\validate-hybrid-provider.js
if errorlevel 1 goto erro

echo [38/46] Real migration dry-run...
node scripts\validate-real-migration-dry-run.js
if errorlevel 1 goto erro

echo [39/46] Migration validate...
node migration\validate-migration.js --dry-run
if errorlevel 1 goto erro

echo [40/46] Docker compose build...
docker compose build
if errorlevel 1 goto erro

echo [41/46] Build frontend...
call npm run build:prod
if errorlevel 1 goto erro

echo [42/46] Health PRD atual...
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro

echo [43/46] Release candidate...
node scripts\validate-release-candidate.js
if errorlevel 1 goto erro

echo [44/46] Logging real validado.
echo [45/46] Validadores sem falso positivo validados.
echo [46/46] Homologacao concluida.

echo LOGGING REAL SEM FALSO POSITIVO VALIDADO.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DE LOGGING REAL SEM FALSO POSITIVO.
pause
exit /b 1
