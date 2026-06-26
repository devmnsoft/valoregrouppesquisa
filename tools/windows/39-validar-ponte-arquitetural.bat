@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/12] Validando frontend...
call npm run check
if errorlevel 1 goto erro

echo [2/12] Validando provider da jornada publica...
node scripts\validate-public-journey-provider.js
if errorlevel 1 goto erro

echo [3/12] Validando arquitetura de transicao...
node scripts\validate-architecture-transition.js
if errorlevel 1 goto erro

echo [4/12] Validando pesquisa destaque...
node scripts\validate-featured-home-survey.js --project gestordepesquisa
if errorlevel 1 goto erro

echo [5/12] Validando fluxo publico...
node scripts\validate-public-survey-flow.js --project gestordepesquisa
if errorlevel 1 goto erro

echo [6/12] Validando envio de resultado...
node scripts\validate-result-email-flow.js
if errorlevel 1 goto erro

echo [7/12] Subindo PostgreSQL...
docker compose -f docker-compose.postgres.yml up -d
if errorlevel 1 goto erro

echo [8/12] Validando schema PostgreSQL...
node scripts\validate-postgres-schema.js
if errorlevel 1 goto erro

echo [9/12] Build backend...
dotnet build backend\Valora.sln
if errorlevel 1 goto erro

echo [10/12] Testes backend...
dotnet test backend\Valora.sln
if errorlevel 1 goto erro

echo [11/12] Build frontend...
call npm run build:prod
if errorlevel 1 goto erro

echo [12/12] Release candidate...
node scripts\validate-release-candidate.js
if errorlevel 1 goto erro

echo PONTE ARQUITETURAL VALIDADA.
pause
exit /b 0

:erro
echo FALHA NA PONTE ARQUITETURAL.
pause
exit /b 1
