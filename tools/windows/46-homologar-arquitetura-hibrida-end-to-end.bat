@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/17] Frontend syntax...
call npm run check
if errorlevel 1 goto erro

echo [2/17] API provider...
node scripts\validate-api-provider.js
if errorlevel 1 goto erro

echo [3/17] Public journey provider...
node scripts\validate-public-journey-provider.js
if errorlevel 1 goto erro

echo [4/17] Architecture warnings...
node scripts\validate-architecture-warnings.js
if errorlevel 1 goto erro

echo [5/17] Subindo PostgreSQL...
docker compose -f docker-compose.postgres.yml up -d
if errorlevel 1 goto erro

echo [6/17] Validando schema PostgreSQL...
node scripts\validate-postgres-schema.js
if errorlevel 1 goto erro

echo [7/17] Build backend...
dotnet build backend\Valora.sln
if errorlevel 1 goto erro

echo [8/17] Testes backend...
dotnet test backend\Valora.sln
if errorlevel 1 goto erro

echo [9/17] API backend...
node scripts\validate-api-backend.js
if errorlevel 1 goto erro

echo [10/17] MVP PostgreSQL vertical...
node scripts\validate-vertical-postgres-mvp.js
if errorlevel 1 goto erro

echo [11/17] End-to-end API flow...
node scripts\validate-end-to-end-api-flow.js
if errorlevel 1 goto erro

echo [12/17] Migration scripts...
node scripts\validate-migration-scripts.js
if errorlevel 1 goto erro

echo [13/17] Hybrid provider...
node scripts\validate-hybrid-provider.js
if errorlevel 1 goto erro

echo [14/17] Migration dry-run...
node migration\validate-migration.js --dry-run
if errorlevel 1 goto erro

echo [15/17] Build frontend...
call npm run build:prod
if errorlevel 1 goto erro

echo [16/17] Health PRD atual...
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro

echo [17/17] Release candidate...
node scripts\validate-release-candidate.js
if errorlevel 1 goto erro

echo HOMOLOGACAO HIBRIDA END-TO-END VALIDADA.
pause
exit /b 0

:erro
echo FALHA NA HOMOLOGACAO HIBRIDA END-TO-END.
pause
exit /b 1
