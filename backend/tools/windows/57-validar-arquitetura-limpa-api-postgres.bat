@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/25] Frontend syntax...
call npm run check
if errorlevel 1 goto erro

echo [2/25] Bootstrap frontend...
node scripts\validate-bootstrap-frontend.js
if errorlevel 1 goto erro

echo [3/25] API provider...
node scripts\validate-api-provider.js
if errorlevel 1 goto erro

echo [4/25] Public journey provider...
node scripts\validate-public-journey-provider.js
if errorlevel 1 goto erro

echo [5/25] Architecture warnings...
node scripts\validate-architecture-warnings.js
if errorlevel 1 goto erro

echo [6/25] Cutover readiness...
node scripts\validate-cutover-readiness.js
if errorlevel 1 goto erro

echo [7/25] Backend implementation...
node scripts\validate-backend-implementation.js
if errorlevel 1 goto erro

echo [8/25] Backend clean architecture...
node scripts\validate-backend-clean-architecture.js
if errorlevel 1 goto erro

echo [9/25] Single PostgreSQL schema...
node scripts\validate-single-postgres-schema.js
if errorlevel 1 goto erro

echo [10/25] Docker/Windows runtime...
node scripts\validate-docker-windows-runtime.js
if errorlevel 1 goto erro

echo [11/25] Subindo PostgreSQL...
docker compose -f docker-compose.postgres.yml up -d
if errorlevel 1 goto erro

echo [12/25] Validando schema PostgreSQL...
node scripts\validate-postgres-schema.js
if errorlevel 1 goto erro

echo [13/25] Build backend...
dotnet build backend\Valora.sln
if errorlevel 1 goto erro

echo [14/25] Testes backend...
dotnet test backend\Valora.sln
if errorlevel 1 goto erro

echo [15/25] API backend...
node scripts\validate-api-backend.js
if errorlevel 1 goto erro

echo [16/25] MVP PostgreSQL vertical...
node scripts\validate-vertical-postgres-mvp.js
if errorlevel 1 goto erro

echo [17/25] End-to-end API flow...
node scripts\validate-end-to-end-api-flow.js
if errorlevel 1 goto erro

echo [18/25] Migration scripts...
node scripts\validate-migration-scripts.js
if errorlevel 1 goto erro

echo [19/25] Hybrid provider...
node scripts\validate-hybrid-provider.js
if errorlevel 1 goto erro

echo [20/25] Real migration dry-run...
node scripts\validate-real-migration-dry-run.js
if errorlevel 1 goto erro

echo [21/25] Migration validate...
node migration\validate-migration.js --dry-run
if errorlevel 1 goto erro

echo [22/25] Docker compose build...
docker compose build
if errorlevel 1 goto erro

echo [23/25] Build frontend...
call npm run build:prod
if errorlevel 1 goto erro

echo [24/25] Health PRD atual...
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro

echo [25/25] Release candidate...
node scripts\validate-release-candidate.js
if errorlevel 1 goto erro

echo ARQUITETURA LIMPA API/POSTGRES VALIDADA.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DA ARQUITETURA LIMPA API/POSTGRES.
pause
exit /b 1
