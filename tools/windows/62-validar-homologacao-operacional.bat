@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/44] Frontend syntax...
call npm run check
if errorlevel 1 goto erro

echo [2/44] Bootstrap frontend...
node scripts\validate-bootstrap-frontend.js
if errorlevel 1 goto erro

echo [3/44] Frontend API errors...
node scripts\validate-frontend-api-errors.js
if errorlevel 1 goto erro

echo [4/44] Error handling middleware...
node scripts\validate-error-handling-middleware.js
if errorlevel 1 goto erro

echo [5/44] Logging coverage...
node scripts\validate-logging-coverage.js
if errorlevel 1 goto erro

echo [6/44] Repository logging...
node scripts\validate-repository-logging.js
if errorlevel 1 goto erro

echo [7/44] Migration logging...
node scripts\validate-migration-logging.js
if errorlevel 1 goto erro

echo [8/44] Sensitive logs...
node scripts\validate-no-sensitive-logs.js
if errorlevel 1 goto erro

echo [9/44] Correlation ID...
node scripts\validate-correlation-id.js
if errorlevel 1 goto erro

echo [10/44] Transaction logging...
node scripts\validate-transaction-logging.js
if errorlevel 1 goto erro

echo [11/44] Email error handling...
node scripts\validate-email-error-handling.js
if errorlevel 1 goto erro

echo [12/44] Health observability...
node scripts\validate-health-observability.js
if errorlevel 1 goto erro

echo [13/44] API provider...
node scripts\validate-api-provider.js
if errorlevel 1 goto erro

echo [14/44] Public journey provider...
node scripts\validate-public-journey-provider.js
if errorlevel 1 goto erro

echo [15/44] Public API routes...
node scripts\validate-public-api-routes.js
if errorlevel 1 goto erro

echo [16/44] Public API real flow...
node scripts\validate-public-api-real-flow.js
if errorlevel 1 goto erro

echo [17/44] Typed public services...
node scripts\validate-typed-public-services.js
if errorlevel 1 goto erro

echo [18/44] Service method size...
node scripts\validate-service-method-size.js
if errorlevel 1 goto erro

echo [19/44] Result token security...
node scripts\validate-result-token-security.js
if errorlevel 1 goto erro

echo [20/44] Transactional public submit...
node scripts\validate-transactional-public-submit.js
if errorlevel 1 goto erro

echo [21/44] Repository transaction boundary...
node scripts\validate-repository-transaction-boundary.js
if errorlevel 1 goto erro

echo [22/44] Architecture warnings...
node scripts\validate-architecture-warnings.js
if errorlevel 1 goto erro

echo [23/44] Cutover readiness...
node scripts\validate-cutover-readiness.js
if errorlevel 1 goto erro

echo [24/44] Backend implementation...
node scripts\validate-backend-implementation.js
if errorlevel 1 goto erro

echo [25/44] Backend clean architecture...
node scripts\validate-backend-clean-architecture.js
if errorlevel 1 goto erro

echo [26/44] Single PostgreSQL schema...
node scripts\validate-single-postgres-schema.js
if errorlevel 1 goto erro

echo [27/44] Docker/Windows runtime...
node scripts\validate-docker-windows-runtime.js
if errorlevel 1 goto erro

echo [28/44] Subindo PostgreSQL...
docker compose -f docker-compose.postgres.yml up -d
if errorlevel 1 goto erro

echo [29/44] Validando schema PostgreSQL...
node scripts\validate-postgres-schema.js
if errorlevel 1 goto erro

echo [30/44] Build backend...
dotnet build backend\Valora.sln
if errorlevel 1 goto erro

echo [31/44] Testes backend...
dotnet test backend\Valora.sln
if errorlevel 1 goto erro

echo [32/44] API backend...
node scripts\validate-api-backend.js
if errorlevel 1 goto erro

echo [33/44] MVP PostgreSQL vertical...
node scripts\validate-vertical-postgres-mvp.js
if errorlevel 1 goto erro

echo [34/44] End-to-end API flow...
node scripts\validate-end-to-end-api-flow.js
if errorlevel 1 goto erro

echo [35/44] Migration scripts...
node scripts\validate-migration-scripts.js
if errorlevel 1 goto erro

echo [36/44] Hybrid provider...
node scripts\validate-hybrid-provider.js
if errorlevel 1 goto erro

echo [37/44] Real migration dry-run...
node scripts\validate-real-migration-dry-run.js
if errorlevel 1 goto erro

echo [38/44] Migration validate...
node migration\validate-migration.js --dry-run
if errorlevel 1 goto erro

echo [39/44] Docker compose build...
docker compose build
if errorlevel 1 goto erro

echo [40/44] Build frontend...
call npm run build:prod
if errorlevel 1 goto erro

echo [41/44] Health PRD atual...
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro

echo [42/44] Release candidate...
node scripts\validate-release-candidate.js
if errorlevel 1 goto erro

echo [43/44] Logs operacionais verificados.
echo [44/44] Homologacao operacional concluida.

echo HOMOLOGACAO OPERACIONAL VALIDADA.
pause
exit /b 0

:erro
echo FALHA NA HOMOLOGACAO OPERACIONAL.
pause
exit /b 1
