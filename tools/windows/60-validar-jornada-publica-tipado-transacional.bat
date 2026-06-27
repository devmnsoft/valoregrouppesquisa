@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/34] Frontend syntax...
call npm run check
if errorlevel 1 goto erro

echo [2/34] Bootstrap frontend...
node scripts\validate-bootstrap-frontend.js
if errorlevel 1 goto erro

echo [3/34] API provider...
node scripts\validate-api-provider.js
if errorlevel 1 goto erro

echo [4/34] Public journey provider...
node scripts\validate-public-journey-provider.js
if errorlevel 1 goto erro

echo [5/34] Public API routes...
node scripts\validate-public-api-routes.js
if errorlevel 1 goto erro

echo [6/34] Public API real flow...
node scripts\validate-public-api-real-flow.js
if errorlevel 1 goto erro

echo [7/34] Typed public services...
node scripts\validate-typed-public-services.js
if errorlevel 1 goto erro

echo [8/34] Service method size...
node scripts\validate-service-method-size.js
if errorlevel 1 goto erro

echo [9/34] Result token security...
node scripts\validate-result-token-security.js
if errorlevel 1 goto erro

echo [10/34] Transactional public submit...
node scripts\validate-transactional-public-submit.js
if errorlevel 1 goto erro

echo [11/34] Repository transaction boundary...
node scripts\validate-repository-transaction-boundary.js
if errorlevel 1 goto erro

echo [12/34] Architecture warnings...
node scripts\validate-architecture-warnings.js
if errorlevel 1 goto erro

echo [13/34] Cutover readiness...
node scripts\validate-cutover-readiness.js
if errorlevel 1 goto erro

echo [14/34] Backend implementation...
node scripts\validate-backend-implementation.js
if errorlevel 1 goto erro

echo [15/34] Backend clean architecture...
node scripts\validate-backend-clean-architecture.js
if errorlevel 1 goto erro

echo [16/34] Single PostgreSQL schema...
node scripts\validate-single-postgres-schema.js
if errorlevel 1 goto erro

echo [17/34] Docker/Windows runtime...
node scripts\validate-docker-windows-runtime.js
if errorlevel 1 goto erro

echo [18/34] Subindo PostgreSQL...
docker compose -f docker-compose.postgres.yml up -d
if errorlevel 1 goto erro

echo [19/34] Validando schema PostgreSQL...
node scripts\validate-postgres-schema.js
if errorlevel 1 goto erro

echo [20/34] Build backend...
dotnet build backend\Valora.sln
if errorlevel 1 goto erro

echo [21/34] Testes backend...
dotnet test backend\Valora.sln
if errorlevel 1 goto erro

echo [22/34] API backend...
node scripts\validate-api-backend.js
if errorlevel 1 goto erro

echo [23/34] MVP PostgreSQL vertical...
node scripts\validate-vertical-postgres-mvp.js
if errorlevel 1 goto erro

echo [24/34] End-to-end API flow...
node scripts\validate-end-to-end-api-flow.js
if errorlevel 1 goto erro

echo [25/34] Migration scripts...
node scripts\validate-migration-scripts.js
if errorlevel 1 goto erro

echo [26/34] Hybrid provider...
node scripts\validate-hybrid-provider.js
if errorlevel 1 goto erro

echo [27/34] Real migration dry-run...
node scripts\validate-real-migration-dry-run.js
if errorlevel 1 goto erro

echo [28/34] Migration validate...
node migration\validate-migration.js --dry-run
if errorlevel 1 goto erro

echo [29/34] Docker compose build...
docker compose build
if errorlevel 1 goto erro

echo [30/34] Build frontend...
call npm run build:prod
if errorlevel 1 goto erro

echo [31/34] Health PRD atual...
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro

echo [32/34] Release candidate...
node scripts\validate-release-candidate.js
if errorlevel 1 goto erro

echo [33/34] Teste provider API local documentado...
echo DATA_PROVIDER=api deve ser testado com config local-api antes de qualquer cutover.

echo [34/34] Validacao final concluida.
echo JORNADA PUBLICA TIPADA E TRANSACIONAL VALIDADA.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DA JORNADA PUBLICA TIPADA E TRANSACIONAL.
pause
exit /b 1
