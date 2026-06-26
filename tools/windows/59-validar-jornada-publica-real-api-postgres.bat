@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/30] Frontend syntax...
call npm run check
if errorlevel 1 goto erro

echo [2/30] Bootstrap frontend...
node scripts\validate-bootstrap-frontend.js
if errorlevel 1 goto erro

echo [3/30] API provider...
node scripts\validate-api-provider.js
if errorlevel 1 goto erro

echo [4/30] Public journey provider...
node scripts\validate-public-journey-provider.js
if errorlevel 1 goto erro

echo [5/30] Public API routes...
node scripts\validate-public-api-routes.js
if errorlevel 1 goto erro

echo [6/30] Public API real flow...
node scripts\validate-public-api-real-flow.js
if errorlevel 1 goto erro

echo [7/30] Result token security...
node scripts\validate-result-token-security.js
if errorlevel 1 goto erro

echo [8/30] Transactional public submit...
node scripts\validate-transactional-public-submit.js
if errorlevel 1 goto erro

echo [9/30] Architecture warnings...
node scripts\validate-architecture-warnings.js
if errorlevel 1 goto erro

echo [10/30] Cutover readiness...
node scripts\validate-cutover-readiness.js
if errorlevel 1 goto erro

echo [11/30] Backend implementation...
node scripts\validate-backend-implementation.js
if errorlevel 1 goto erro

echo [12/30] Backend clean architecture...
node scripts\validate-backend-clean-architecture.js
if errorlevel 1 goto erro

echo [13/30] Single PostgreSQL schema...
node scripts\validate-single-postgres-schema.js
if errorlevel 1 goto erro

echo [14/30] Docker/Windows runtime...
node scripts\validate-docker-windows-runtime.js
if errorlevel 1 goto erro

echo [15/30] Subindo PostgreSQL...
docker compose -f docker-compose.postgres.yml up -d
if errorlevel 1 goto erro

echo [16/30] Validando schema PostgreSQL...
node scripts\validate-postgres-schema.js
if errorlevel 1 goto erro

echo [17/30] Build backend...
dotnet build backend\Valora.sln
if errorlevel 1 goto erro

echo [18/30] Testes backend...
dotnet test backend\Valora.sln
if errorlevel 1 goto erro

echo [19/30] API backend...
node scripts\validate-api-backend.js
if errorlevel 1 goto erro

echo [20/30] MVP PostgreSQL vertical...
node scripts\validate-vertical-postgres-mvp.js
if errorlevel 1 goto erro

echo [21/30] End-to-end API flow...
node scripts\validate-end-to-end-api-flow.js
if errorlevel 1 goto erro

echo [22/30] Migration scripts...
node scripts\validate-migration-scripts.js
if errorlevel 1 goto erro

echo [23/30] Hybrid provider...
node scripts\validate-hybrid-provider.js
if errorlevel 1 goto erro

echo [24/30] Real migration dry-run...
node scripts\validate-real-migration-dry-run.js
if errorlevel 1 goto erro

echo [25/30] Migration validate...
node migration\validate-migration.js --dry-run
if errorlevel 1 goto erro

echo [26/30] Docker compose build...
docker compose build
if errorlevel 1 goto erro

echo [27/30] Build frontend...
call npm run build:prod
if errorlevel 1 goto erro

echo [28/30] Health PRD atual...
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro

echo [29/30] Release candidate...
node scripts\validate-release-candidate.js
if errorlevel 1 goto erro

echo [30/30] Validação final concluída.
echo JORNADA PUBLICA REAL API/POSTGRES VALIDADA.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DA JORNADA PUBLICA REAL API/POSTGRES.
pause
exit /b 1
