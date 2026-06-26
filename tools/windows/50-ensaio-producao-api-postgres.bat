@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/18] Frontend syntax...
call npm run check
if errorlevel 1 goto erro

echo [2/18] API provider...
node scripts\validate-api-provider.js
if errorlevel 1 goto erro

echo [3/18] Public journey provider...
node scripts\validate-public-journey-provider.js
if errorlevel 1 goto erro

echo [4/18] Architecture warnings...
node scripts\validate-architecture-warnings.js
if errorlevel 1 goto erro

echo [5/18] Cutover readiness...
node scripts\validate-cutover-readiness.js
if errorlevel 1 goto erro

echo [6/18] Subindo PostgreSQL...
docker compose -f docker-compose.postgres.yml up -d
if errorlevel 1 goto erro

echo [7/18] Validando schema PostgreSQL...
node scripts\validate-postgres-schema.js
if errorlevel 1 goto erro

echo [8/18] Build backend...
dotnet build backend\Valora.sln
if errorlevel 1 goto erro

echo [9/18] Testes backend...
dotnet test backend\Valora.sln
if errorlevel 1 goto erro

echo [10/18] API backend...
node scripts\validate-api-backend.js
if errorlevel 1 goto erro

echo [11/18] MVP PostgreSQL vertical...
node scripts\validate-vertical-postgres-mvp.js
if errorlevel 1 goto erro

echo [12/18] End-to-end API flow...
node scripts\validate-end-to-end-api-flow.js
if errorlevel 1 goto erro

echo [13/18] Migration scripts...
node scripts\validate-migration-scripts.js
if errorlevel 1 goto erro

echo [14/18] Hybrid provider...
node scripts\validate-hybrid-provider.js
if errorlevel 1 goto erro

echo [15/18] Migration dry-run...
node migration\validate-migration.js --dry-run
if errorlevel 1 goto erro

echo [16/18] Build frontend...
call npm run build:prod
if errorlevel 1 goto erro

echo [17/18] Health PRD atual...
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro

echo [18/18] Release candidate...
node scripts\validate-release-candidate.js
if errorlevel 1 goto erro

echo ENSAIO DE PRODUCAO API/POSTGRES VALIDADO.
pause
exit /b 0

:erro
echo FALHA NO ENSAIO DE PRODUCAO API/POSTGRES.
pause
exit /b 1
