@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/80] Subindo ambiente live local...
call npm run local:live:up
if errorlevel 1 goto erro

echo [2/80] Aplicando migrations locais...
call npm run local:migrations:apply
if errorlevel 1 goto erro

echo [3/80] Syntax...
call npm run check
if errorlevel 1 goto erro

echo [4/80] Security check...
call npm run security:check
if errorlevel 1 goto erro

echo [5/80] Data contracts...
call npm run validate:data-contracts
if errorlevel 1 goto erro

echo [6/80] Render resilience...
call npm run validate:render-resilience
if errorlevel 1 goto erro

echo [7/80] Data shape audit...
call npm run audit:data-shapes
if errorlevel 1 goto erro

echo [8/80] Frontend Bootstrap...
call npm run frontend:bootstrap
if errorlevel 1 goto erro

echo [9/80] Frontend API errors...
call npm run frontend:api-errors
if errorlevel 1 goto erro

echo [10/80] Frontend SaaS coverage...
call npm run prod:frontend-saas
if errorlevel 1 goto erro

echo [11/80] Browser E2E...
call npm run prod:browser-e2e
if errorlevel 1 goto erro

echo [12/80] SaaS E2E LIVE...
call npm run prod:saas-e2e
if errorlevel 1 goto erro

echo [13/80] Cutover dry run real...
call npm run prod:cutover-dry-run
if errorlevel 1 goto erro

echo [14/80] Rollback ready real...
call npm run prod:rollback-ready
if errorlevel 1 goto erro

echo [15/80] Performance baseline...
call npm run prod:performance
if errorlevel 1 goto erro

echo [16/80] Security live...
call npm run prod:security-live
if errorlevel 1 goto erro

echo [17/80] Bug bash readiness...
call npm run prod:bug-bash
if errorlevel 1 goto erro

echo [18/80] Final release gate...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [79/80] Encerrando ambiente live local...
call npm run local:live:down
if errorlevel 1 goto erro

echo [80/80] Produto SaaS live homologado.
echo PRODUTO SAAS FINAL HOMOLOGAVEL COM GATES LIVE.
pause
exit /b 0

:erro
echo FALHA NA HOMOLOGACAO LIVE DO PRODUTO SAAS.
call npm run local:live:down
pause
exit /b 1
