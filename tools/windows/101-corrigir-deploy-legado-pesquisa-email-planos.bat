@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/35] Segurança sem segredos...
call npm run security:no-secrets
if errorlevel 1 goto erro

echo [2/35] Scripts obrigatórios...
call npm run scripts:required
if errorlevel 1 goto erro

echo [3/35] Runtime Functions Node 22...
call npm run functions:node22-readiness
if errorlevel 1 goto erro

echo [4/35] Instalar Functions...
call npm run functions:install
if errorlevel 1 goto erro

echo [5/35] Lint Functions...
call npm run functions:lint
if errorlevel 1 goto erro

echo [6/35] Submit público legado...
call npm run legacy:public-submit-flow
if errorlevel 1 goto erro

echo [7/35] Functions submit público...
call npm run functions:public-submit
if errorlevel 1 goto erro

echo [8/35] E-mail resultado...
call npm run legacy:result-email-send
if errorlevel 1 goto erro

echo [9/35] Certificado...
call npm run legacy:certificate-complete
if errorlevel 1 goto erro

echo [10/35] Planos...
call npm run legacy:plans-tab
if errorlevel 1 goto erro

echo [11/35] Token gratuito...
call npm run legacy:free-token-never-expires
if errorlevel 1 goto erro

echo [12/35] Menu mobile...
call npm run legacy:admin-mobile-functional
if errorlevel 1 goto erro

echo [13/35] Diagnóstico...
call npm run legacy:diagnostics-complete
if errorlevel 1 goto erro

echo [14/35] Cache busting...
call npm run legacy:cache-busting
if errorlevel 1 goto erro

echo [15/35] Build produção dist...
call npm run build:prod
if errorlevel 1 goto erro

echo [16/35] Validar dist...
call npm run hosting:dist-build
if errorlevel 1 goto erro

echo [17/35] Backend build...
call npm run backend:build
if errorlevel 1 goto erro

echo [18/35] Backend test...
call npm run backend:test
if errorlevel 1 goto erro

echo [19/35] Web ASP.NET...
call npm run web:aspnet-only
if errorlevel 1 goto erro

echo [20/35] Web build...
call npm run web:build
if errorlevel 1 goto erro

echo [21/35] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [22/35] Auditoria...
if not exist SPRINT_67_DEPLOY_AND_LEGACY_FULL_FIX_AUDIT.md goto erro

echo DEPLOY + LEGADO VALIDADO.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO.
pause
exit /b 1
