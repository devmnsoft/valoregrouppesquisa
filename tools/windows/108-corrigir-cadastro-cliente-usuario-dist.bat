@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/25] Segurança sem segredos...
call npm run security:no-secrets
if errorlevel 1 goto erro

echo [2/25] Build produção...
call npm run build:prod
if errorlevel 1 goto erro

echo [3/25] Validar dist...
call npm run hosting:dist-build
if errorlevel 1 goto erro

echo [4/25] Fluxo cliente legado...
call npm run legacy:client-create-flow
if errorlevel 1 goto erro

echo [5/25] Fluxo usuário legado...
call npm run legacy:user-create-flow
if errorlevel 1 goto erro

echo [6/25] Functions cliente/usuário...
call npm run functions:client-user-admin
if errorlevel 1 goto erro

echo [7/25] Rules cliente/usuário...
call npm run rules:client-user
if errorlevel 1 goto erro

echo [8/25] Eventos forms admin...
call npm run legacy:admin-forms-events
if errorlevel 1 goto erro

echo [9/25] Link demo bloqueado...
call npm run legacy:demo-link-blocked
if errorlevel 1 goto erro

echo [10/25] Link oficial gratuito...
call npm run legacy:official-free-link
if errorlevel 1 goto erro

echo [11/25] Provider demo hotfix...
call npm run legacy:provider-demo-hotfix
if errorlevel 1 goto erro

echo [12/25] E-mail resultado...
call npm run legacy:result-email-send
if errorlevel 1 goto erro

echo [13/25] Certificado...
call npm run legacy:certificate-complete
if errorlevel 1 goto erro

echo [14/25] Planos...
call npm run legacy:plans-tab
if errorlevel 1 goto erro

echo [15/25] Menu mobile...
call npm run legacy:admin-mobile-functional
if errorlevel 1 goto erro

echo [16/25] E2E cliente...
call npm run e2e:legacy-client-create
if errorlevel 1 goto erro

echo [17/25] E2E usuário...
call npm run e2e:legacy-user-create
if errorlevel 1 goto erro

echo [18/25] E2E regressão admin...
call npm run e2e:legacy-admin-client-user-regression
if errorlevel 1 goto erro

echo [19/25] Backend build...
call npm run backend:build
if errorlevel 1 goto erro

echo [20/25] Backend test...
call npm run backend:test
if errorlevel 1 goto erro

echo [21/25] Web ASP.NET...
call npm run web:aspnet-only
if errorlevel 1 goto erro

echo [22/25] Web build...
call npm run web:build
if errorlevel 1 goto erro

echo [23/25] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [24/25] Auditoria...
if not exist SPRINT_74_LEGACY_CLIENT_USER_AND_DIST_FIX_AUDIT.md goto erro

echo CADASTRO CLIENTE/USUARIO + DIST CORRIGIDOS.
pause
exit /b 0

:erro
echo FALHA NA CORRECAO CADASTRO CLIENTE/USUARIO + DIST.
pause
exit /b 1
