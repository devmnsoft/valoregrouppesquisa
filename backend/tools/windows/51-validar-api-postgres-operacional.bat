@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/19] Frontend syntax...
call npm run check
if errorlevel 1 goto erro

echo [2/19] API provider...
node scripts\validate-api-provider.js
if errorlevel 1 goto erro

echo [3/19] Public journey provider...
node scripts\validate-public-journey-provider.js
if errorlevel 1 goto erro

echo [4/19] Architecture warnings...
node scripts\validate-architecture-warnings.js
if errorlevel 1 goto erro

echo [5/19] Cutover readiness...
node scripts\validate-cutover-readiness.js
if errorlevel 1 goto erro

echo [6/19] Subindo PostgreSQL...
docker compose -f docker-compose.postgres.yml up -d
if errorlevel 1 goto erro

echo [7/19] Validando schema PostgreSQL...
node scripts\validate-postgres-schema.js
if errorlevel 1 goto erro

echo [8/19] Build backend...
dotnet build backend\Valora.sln
if errorlevel 1 goto erro

echo [9/19] Testes backend...
dotnet test backend\Valora.sln
if errorlevel 1 goto erro

echo [10/19] API backend...
node scripts\validate-api-backend.js
if errorlevel 1 goto erro

echo [11/19] MVP PostgreSQL vertical...
node scripts\validate-vertical-postgres-mvp.js
if errorlevel 1 goto erro

echo [12/19] End-to-end API flow...
node scripts\validate-end-to-end-api-flow.js
if errorlevel 1 goto erro

echo [13/19] Migration scripts...
node scripts\validate-migration-scripts.js
if errorlevel 1 goto erro

echo [14/19] Hybrid provider...
node scripts\validate-hybrid-provider.js
if errorlevel 1 goto erro

echo [15/19] Real migration dry-run...
node scripts\validate-real-migration-dry-run.js
if errorlevel 1 goto erro

echo [16/19] Migration validate...
node migration\validate-migration.js --dry-run
if errorlevel 1 goto erro

echo [17/19] Build frontend...
call npm run build:prod
if errorlevel 1 goto erro

echo [18/19] Health PRD atual...
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro

echo [19/19] Release candidate...
node scripts\validate-release-candidate.js
if errorlevel 1 goto erro

echo API/POSTGRES OPERACIONAL VALIDADO.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO API/POSTGRES OPERACIONAL.
pause
exit /b 1
