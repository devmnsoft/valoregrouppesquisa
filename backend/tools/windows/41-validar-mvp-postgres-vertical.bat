@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/14] Frontend syntax...
call npm run check
if errorlevel 1 goto erro

echo [2/14] Validando API provider...
node scripts\validate-api-provider.js
if errorlevel 1 goto erro

echo [3/14] Validando provider publico...
node scripts\validate-public-journey-provider.js
if errorlevel 1 goto erro

echo [4/14] Subindo Postgres...
docker compose -f docker-compose.postgres.yml up -d
if errorlevel 1 goto erro

echo [5/14] Validando schema...
node scripts\validate-postgres-schema.js
if errorlevel 1 goto erro

echo [6/14] Build backend...
dotnet build backend\Valora.sln
if errorlevel 1 goto erro

echo [7/14] Testes backend...
dotnet test backend\Valora.sln
if errorlevel 1 goto erro

echo [8/14] Validando API backend...
node scripts\validate-api-backend.js
if errorlevel 1 goto erro

echo [9/14] Validando MVP vertical...
node scripts\validate-vertical-postgres-mvp.js
if errorlevel 1 goto erro

echo [10/14] Validando migration scripts...
node scripts\validate-migration-scripts.js
if errorlevel 1 goto erro

echo [11/14] Validando hybrid...
node scripts\validate-hybrid-provider.js
if errorlevel 1 goto erro

echo [12/14] Build frontend...
call npm run build:prod
if errorlevel 1 goto erro

echo [13/14] Health PRD atual...
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro

echo [14/14] Release candidate...
node scripts\validate-release-candidate.js
if errorlevel 1 goto erro

echo MVP POSTGRES VERTICAL VALIDADO.
pause
exit /b 0

:erro
echo FALHA NO MVP POSTGRES VERTICAL.
pause
exit /b 1
