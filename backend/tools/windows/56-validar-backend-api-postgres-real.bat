@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/20] Frontend syntax...
call npm run check
if errorlevel 1 goto erro

echo [2/20] API provider...
node scripts\validate-api-provider.js
if errorlevel 1 goto erro

echo [3/20] Public journey provider...
node scripts\validate-public-journey-provider.js
if errorlevel 1 goto erro

echo [4/20] Architecture warnings...
node scripts\validate-architecture-warnings.js
if errorlevel 1 goto erro

echo [5/20] Cutover readiness...
node scripts\validate-cutover-readiness.js
if errorlevel 1 goto erro

echo [6/20] Backend implementation...
node scripts\validate-backend-implementation.js
if errorlevel 1 goto erro

echo [7/20] Subindo PostgreSQL...
docker compose -f docker-compose.postgres.yml up -d
if errorlevel 1 goto erro

echo [8/20] Validando schema PostgreSQL...
node scripts\validate-postgres-schema.js
if errorlevel 1 goto erro

echo [9/20] Build backend...
dotnet build backend\Valora.sln
if errorlevel 1 goto erro

echo [10/20] Testes backend...
dotnet test backend\Valora.sln
if errorlevel 1 goto erro

echo [11/20] API backend...
node scripts\validate-api-backend.js
if errorlevel 1 goto erro

echo [12/20] MVP PostgreSQL vertical...
node scripts\validate-vertical-postgres-mvp.js
if errorlevel 1 goto erro

echo [13/20] End-to-end API flow...
node scripts\validate-end-to-end-api-flow.js
if errorlevel 1 goto erro

echo [14/20] Migration scripts...
node scripts\validate-migration-scripts.js
if errorlevel 1 goto erro

echo [15/20] Hybrid provider...
node scripts\validate-hybrid-provider.js
if errorlevel 1 goto erro

echo [16/20] Real migration dry-run...
node scripts\validate-real-migration-dry-run.js
if errorlevel 1 goto erro

echo [17/20] Migration validate...
node migration\validate-migration.js --dry-run
if errorlevel 1 goto erro

echo [18/20] Build frontend...
call npm run build:prod
if errorlevel 1 goto erro

echo [19/20] Health PRD atual...
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro

echo [20/20] Release candidate...
node scripts\validate-release-candidate.js
if errorlevel 1 goto erro

echo BACKEND/API/POSTGRES REAL VALIDADO.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO BACKEND/API/POSTGRES REAL.
pause
exit /b 1
