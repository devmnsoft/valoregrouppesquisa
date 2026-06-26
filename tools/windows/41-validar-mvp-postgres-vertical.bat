@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/13] Frontend syntax...
call npm run check
if errorlevel 1 goto erro

echo [2/13] Validando provider publico...
node scripts\validate-public-journey-provider.js
if errorlevel 1 goto erro

echo [3/13] Subindo Postgres...
docker compose -f docker-compose.postgres.yml up -d
if errorlevel 1 goto erro

echo [4/13] Validando schema...
node scripts\validate-postgres-schema.js
if errorlevel 1 goto erro

echo [5/13] Build backend...
dotnet build backend\Valora.sln
if errorlevel 1 goto erro

echo [6/13] Testes backend...
dotnet test backend\Valora.sln
if errorlevel 1 goto erro

echo [7/13] Rodando API health...
node scripts\validate-api-backend.js
if errorlevel 1 goto erro

echo [8/13] Validando MVP vertical...
node scripts\validate-vertical-postgres-mvp.js
if errorlevel 1 goto erro

echo [9/13] Validando hybrid...
node scripts\validate-hybrid-provider.js
if errorlevel 1 goto erro

echo [10/13] Validando migration...
node scripts\validate-migration-scripts.js
if errorlevel 1 goto erro

echo [11/13] Build frontend...
call npm run build:prod
if errorlevel 1 goto erro

echo [12/13] Health atual...
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
if errorlevel 1 goto erro

echo [13/13] Release candidate...
node scripts\validate-release-candidate.js
if errorlevel 1 goto erro

echo MVP POSTGRES VERTICAL VALIDADO.
pause
exit /b 0

:erro
echo FALHA NO MVP POSTGRES VERTICAL.
pause
exit /b 1
