@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/26] Frontend syntax...
call npm run check
if errorlevel 1 goto erro

echo [2/26] Bootstrap frontend...
node scripts\validate-bootstrap-frontend.js
if errorlevel 1 goto erro

echo [3/26] API provider...
node scripts\validate-api-provider.js
if errorlevel 1 goto erro

echo [4/26] Public journey provider...
node scripts\validate-public-journey-provider.js
if errorlevel 1 goto erro

echo [5/26] Public API routes...
node scripts\validate-public-api-routes.js
if errorlevel 1 goto erro

echo [6/26] Architecture warnings...
node scripts\validate-architecture-warnings.js
if errorlevel 1 goto erro

echo [7/26] Cutover readiness...
node scripts\validate-cutover-readiness.js
if errorlevel 1 goto erro

echo [8/26] Backend implementation...
node scripts\validate-backend-implementation.js
if errorlevel 1 goto erro

echo [9/26] Backend clean architecture...
node scripts\validate-backend-clean-architecture.js
if errorlevel 1 goto erro

echo [10/26] Single PostgreSQL schema...
node scripts\validate-single-postgres-schema.js
if errorlevel 1 goto erro

echo [11/26] Docker/Windows runtime...
node scripts\validate-docker-windows-runtime.js
if errorlevel 1 goto erro

echo [12/26] Subindo PostgreSQL...
docker compose -f docker-compose.postgres.yml up -d
if errorlevel 1 goto erro

echo [13/26] Validando schema PostgreSQL...
node scripts\validate-postgres-schema.js
if errorlevel 1 goto erro

echo [14/26] Build backend...
dotnet build backend\Valora.sln
if errorlevel 1 goto erro

echo [15/26] Testes backend...
dotnet test backend\Valora.sln
if errorlevel 1 goto erro

echo [16/26] API backend...
node scripts\validate-api-backend.js
if errorlevel 1 goto erro

echo [17/26] MVP PostgreSQL vertical...
node scripts\validate-vertical-postgres-mvp.js
if errorlevel 1 goto erro

echo [18/26] End-to-end API flow...
node scripts\validate-end-to-end-api-flow.js
if errorlevel 1 goto erro

echo [19/26] Migration scripts...
node scripts\validate-migration-scripts.js
if errorlevel 1 goto erro

echo [20/26] Hybrid provider...
node scripts\validate-hybrid-provider.js
if errorlevel 1 goto erro

echo [21/26] Real migration dry-run...
node scripts\validate-real-migration-dry-run.js
if errorlevel 1 goto erro

echo [22/26] Migration validate...
node migration\validate-migration.js --dry-run
if errorlevel 1 goto erro

echo [23/26] Docker compose build...
docker compose build
if errorlevel 1 goto erro

echo [24/26] Build frontend...
call npm run build:prod
if errorlevel 1 goto erro

echo [25/26] Health PRD atual...
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro

echo [26/26] Release candidate...
node scripts\validate-release-candidate.js
if errorlevel 1 goto erro

echo API/POSTGRES LIMPO VALIDADO.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO API/POSTGRES LIMPO.
pause
exit /b 1
