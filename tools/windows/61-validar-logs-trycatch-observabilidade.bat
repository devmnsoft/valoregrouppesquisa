@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/38] Frontend syntax...
call npm run check
if errorlevel 1 goto erro

echo [2/38] Bootstrap frontend...
node scripts\validate-bootstrap-frontend.js
if errorlevel 1 goto erro

echo [3/38] Error handling middleware...
node scripts\validate-error-handling-middleware.js
if errorlevel 1 goto erro

echo [4/38] Logging coverage...
node scripts\validate-logging-coverage.js
if errorlevel 1 goto erro

echo [5/38] Sensitive logs...
node scripts\validate-no-sensitive-logs.js
if errorlevel 1 goto erro

echo [6/38] Correlation ID...
node scripts\validate-correlation-id.js
if errorlevel 1 goto erro

echo [7/38] Transaction logging...
node scripts\validate-transaction-logging.js
if errorlevel 1 goto erro

echo [8/38] Email error handling...
node scripts\validate-email-error-handling.js
if errorlevel 1 goto erro

echo [9/38] API provider...
node scripts\validate-api-provider.js
if errorlevel 1 goto erro

echo [10/38] Public journey provider...
node scripts\validate-public-journey-provider.js
if errorlevel 1 goto erro

echo [11/38] Public API routes...
node scripts\validate-public-api-routes.js
if errorlevel 1 goto erro

echo [12/38] Public API real flow...
node scripts\validate-public-api-real-flow.js
if errorlevel 1 goto erro

echo [13/38] Typed public services...
node scripts\validate-typed-public-services.js
if errorlevel 1 goto erro

echo [14/38] Service method size...
node scripts\validate-service-method-size.js
if errorlevel 1 goto erro

echo [15/38] Result token security...
node scripts\validate-result-token-security.js
if errorlevel 1 goto erro

echo [16/38] Transactional public submit...
node scripts\validate-transactional-public-submit.js
if errorlevel 1 goto erro

echo [17/38] Repository transaction boundary...
node scripts\validate-repository-transaction-boundary.js
if errorlevel 1 goto erro

echo [18/38] Architecture warnings...
node scripts\validate-architecture-warnings.js
if errorlevel 1 goto erro

echo [19/38] Cutover readiness...
node scripts\validate-cutover-readiness.js
if errorlevel 1 goto erro

echo [20/38] Backend implementation...
node scripts\validate-backend-implementation.js
if errorlevel 1 goto erro

echo [21/38] Backend clean architecture...
node scripts\validate-backend-clean-architecture.js
if errorlevel 1 goto erro

echo [22/38] Single PostgreSQL schema...
node scripts\validate-single-postgres-schema.js
if errorlevel 1 goto erro

echo [23/38] Docker/Windows runtime...
node scripts\validate-docker-windows-runtime.js
if errorlevel 1 goto erro

echo [24/38] Subindo PostgreSQL...
docker compose -f docker-compose.postgres.yml up -d
if errorlevel 1 goto erro

echo [25/38] Validando schema PostgreSQL...
node scripts\validate-postgres-schema.js
if errorlevel 1 goto erro

echo [26/38] Build backend...
dotnet build backend\Valora.sln
if errorlevel 1 goto erro

echo [27/38] Testes backend...
dotnet test backend\Valora.sln
if errorlevel 1 goto erro

echo [28/38] API backend...
node scripts\validate-api-backend.js
if errorlevel 1 goto erro

echo [29/38] MVP PostgreSQL vertical...
node scripts\validate-vertical-postgres-mvp.js
if errorlevel 1 goto erro

echo [30/38] End-to-end API flow...
node scripts\validate-end-to-end-api-flow.js
if errorlevel 1 goto erro

echo [31/38] Migration scripts...
node scripts\validate-migration-scripts.js
if errorlevel 1 goto erro

echo [32/38] Hybrid provider...
node scripts\validate-hybrid-provider.js
if errorlevel 1 goto erro

echo [33/38] Real migration dry-run...
node scripts\validate-real-migration-dry-run.js
if errorlevel 1 goto erro

echo [34/38] Migration validate...
node migration\validate-migration.js --dry-run
if errorlevel 1 goto erro

echo [35/38] Docker compose build...
docker compose build
if errorlevel 1 goto erro

echo [36/38] Build frontend...
call npm run build:prod
if errorlevel 1 goto erro

echo [37/38] Health PRD atual...
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro

echo [38/38] Release candidate...
node scripts\validate-release-candidate.js
if errorlevel 1 goto erro

echo LOGS, TRYCATCH E OBSERVABILIDADE VALIDADOS.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DE LOGS, TRYCATCH E OBSERVABILIDADE.
pause
exit /b 1
